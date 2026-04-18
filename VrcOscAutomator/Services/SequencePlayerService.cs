using System.Diagnostics;
using VrcOscAutomator.Interfaces;
using VrcOscAutomator.Models;

namespace VrcOscAutomator.Services;

public sealed class SequencePlayerService(IOscSender oscSender, IKeyboardSender keyboardSender, IMouseSender mouseSender) : ISequencePlayer
{
    private CancellationTokenSource? _stopCts;

    private volatile CancellationTokenSource? _pauseCts;

    private readonly SemaphoreSlim _resumeSignal = new(0, 1);

    private Task _playTask = Task.CompletedTask;

    private OscSlot? _activeSlot;

    private KeySingleSlot? _pendingKeyRelease;
    private MouseButtonSlot? _pendingMouseRelease;

    private readonly HashSet<int> _pressedKeys = [];
    private readonly HashSet<MouseButton> _pressedMouseButtons = [];
    private readonly Lock _inputLock = new();
    private readonly Dictionary<int, CancellationTokenSource> _repeatTasks = [];
    private KeyRepeatSettings _keyRepeatSettings = new();

    public bool IsPlaying => !_playTask.IsCompleted;

    private volatile bool _isPaused;
    public bool IsPaused { get => _isPaused; private set => _isPaused = value; }

    private volatile int _currentSlotIndex = -1;
    public int CurrentSlotIndex { get => _currentSlotIndex; private set => _currentSlotIndex = value; }

    public async Task PlayAsync(IReadOnlyList<SequenceSlot> slots, bool loop, IProgress<SequenceProgress>? slotProgress, CancellationToken cancellationToken)
    {
        // 前回の残留シグナルをクリア
        while (_resumeSignal.CurrentCount > 0)
            _resumeSignal.Wait(0, CancellationToken.None);
        IsPaused = false;

        _stopCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _playTask = Task.Run(() => ExecuteAsync(slots, loop, slotProgress, _stopCts.Token), CancellationToken.None);
        await _playTask;
    }

    private static IReadOnlyDictionary<int, int> BuildLoopIterations(Stack<(int startIndex, int remaining, int iteration)> stack)
    {
        var dict = new Dictionary<int, int>(stack.Count);
        foreach ((int startIndex, _, int iteration) in stack)
            dict[startIndex] = iteration;
        return dict;
    }

    private async Task ExecuteAsync(IReadOnlyList<SequenceSlot> slots, bool loop, IProgress<SequenceProgress>? slotProgress, CancellationToken stopCt)
    {
        // ループスタック: (ループ開始インデックス, 残り繰り返し回数, 現在の繰り返しカウント)
        var loopStack = new Stack<(int startIndex, int remaining, int iteration)>();

        try
        {
            do
            {
                loopStack.Clear();
                int i = 0;

                while (i < slots.Count)
                {
                    stopCt.ThrowIfCancellationRequested();

                    // スロット開始前に一時停止状態なら待機
                    if (IsPaused)
                    {
                        ReleaseAllInputs();
                        await _resumeSignal.WaitAsync(stopCt);
                        RepressAllInputs();
                    }

                    SequenceSlot slot = slots[i];

                    // ── 繰り返し開始マーカー ──────────────────────────
                    if (slot is LoopBeginSlot lb)
                    {
                        loopStack.Push((i, lb.RepeatCount, 1));
                        i++;
                        continue;
                    }

                    // ── 繰り返し終了マーカー ──────────────────────────
                    if (slot is LoopEndSlot)
                    {
                        if (loopStack.Count > 0)
                        {
                            (int startIdx, int remaining, int iteration) = loopStack.Pop();
                            // remaining == 0: 無限ループ、remaining > 1: まだ残りあり
                            if (remaining == 0 || remaining > 1)
                            {
                                loopStack.Push((startIdx, remaining == 0 ? 0 : remaining - 1, iteration + 1));
                                i = startIdx + 1;
                                continue;
                            }
                        }
                        i++;
                        continue;
                    }

                    // ── ブレークポイント ──────────────────────────────
                    if (slot is BreakpointSlot)
                    {
                        CurrentSlotIndex = i;
                        slotProgress?.Report(new SequenceProgress(i, BuildLoopIterations(loopStack)));
                        IsPaused = true;
                        ReleaseAllInputs();
                        await _resumeSignal.WaitAsync(stopCt);
                        RepressAllInputs();
                        i++;
                        continue;
                    }

                    // ── 通常スロット（WaitSlot / OscSlot）──────────────
                    CurrentSlotIndex = i;
                    slotProgress?.Report(new SequenceProgress(i, BuildLoopIterations(loopStack)));

                    OscSlot? activeOsc = slot as OscSlot;
                    bool isTransition = activeOsc is FloatSlot { TransitionMode: not TransitionMode.None }
                                                  or IntSlot { TransitionMode: not TransitionMode.None };

                    if (activeOsc is not null)
                    {
                        _activeSlot = activeOsc;
                        if (!isTransition)
                            SendCommand(activeOsc);
                    }

                    switch (slot)
                    {
                        case KeySingleSlot ks:
                            switch (ks.Action)
                            {
                                case KeyAction.Press:
                                    keyboardSender.SendKey(ks.VirtualKey, KeyAction.Press);
                                    lock (_inputLock) { _pressedKeys.Add(ks.VirtualKey); }
                                    StartKeyRepeat(ks.VirtualKey, stopCt);
                                    break;
                                case KeyAction.Release:
                                    StopKeyRepeat(ks.VirtualKey);
                                    keyboardSender.SendKey(ks.VirtualKey, KeyAction.Release);
                                    lock (_inputLock) { _pressedKeys.Remove(ks.VirtualKey); }
                                    break;
                                case KeyAction.PressAndRelease:
                                    keyboardSender.SendKey(ks.VirtualKey, KeyAction.Press);
                                    lock (_inputLock) { _pressedKeys.Add(ks.VirtualKey); }
                                    _pendingKeyRelease = ks;
                                    break;
                            }
                            break;
                        case KeyTypeStringSlot kts:
                            string text = kts.AppendNewline ? kts.Text + "\n" : kts.Text;
                            keyboardSender.TypeString(text);
                            break;
                        case MouseButtonSlot mb:
                            var mbSendAction = mb.Action == KeyAction.PressAndRelease ? KeyAction.Press : mb.Action;
                            mouseSender.SendMouseButton(mb.Button, mbSendAction);
                            if (mb.Action is KeyAction.Press or KeyAction.PressAndRelease)
                            {
                                lock (_inputLock) { _pressedMouseButtons.Add(mb.Button); }
                                if (mb.Action == KeyAction.PressAndRelease) _pendingMouseRelease = mb;
                            }
                            else
                            {
                                lock (_inputLock) { _pressedMouseButtons.Remove(mb.Button); }
                            }
                            break;
                        case MouseWheelSlot mw:
                            mouseSender.SendMouseWheel(mw.Clicks);
                            break;
                        case MouseMoveSlot mm:
                            mouseSender.SendMouseMove(mm.X, mm.Y, mm.Mode);
                            break;
                    }

                    if (isTransition)
                        await SendTransitionAsync(activeOsc!, stopCt);
                    else
                        await SlotDelayAsync(activeOsc, slot.GetDurationMs(), stopCt);

                    FlushPendingReleases();

                    if (activeOsc is { ResetOnComplete: true }) ResetSlotValue(activeOsc);
                    _activeSlot = null; // 正常完了済み — finally で二重リセットしない

                    i++;
                }
            }
            while (loop && !stopCt.IsCancellationRequested);
        }
        catch (OperationCanceledException)
        {
            // 正常停止パス
        }
        finally
        {
            // 停止時は ResetOnComplete にかかわらず必ずリセット
            if (_activeSlot is not null) ResetSlotValue(_activeSlot);
            _activeSlot = null;

            FlushPendingReleases();
            ClearInputState();

            CurrentSlotIndex = -1;
            slotProgress?.Report(new SequenceProgress(-1, new Dictionary<int, int>()));
        }
    }

    private async Task SlotDelayAsync(OscSlot? slot, int totalMs, CancellationToken stopCt)
    {
        int remaining = totalMs;

        while (remaining > 0)
        {
            stopCt.ThrowIfCancellationRequested();

            // 一時停止割り込み用CTS
            CancellationTokenSource pauseCts = CancellationTokenSource.CreateLinkedTokenSource(stopCt);
            _pauseCts = pauseCts;

            // CTSセット直前にPauseが来た場合の競合を解消
            if (IsPaused)
                pauseCts.Cancel();

            long startTick = Environment.TickCount64;
            try
            {
                await Task.Delay(remaining, pauseCts.Token);
                _pauseCts = null;
                pauseCts.Dispose();
                return; // 正常完了
            }
            catch (OperationCanceledException) when (!stopCt.IsCancellationRequested)
            {
                // 一時停止
                int elapsed = (int)Math.Min(Environment.TickCount64 - startTick, remaining);
                remaining -= elapsed;
                _pauseCts = null;
                pauseCts.Dispose();
            }

            if (remaining <= 0) return;

            // 一時停止中: OSC リセット・キー/ボタン解放（ResetOnComplete にかかわらずリセット）
            if (slot is not null)
            {
                ResetSlotValue(slot);
                _activeSlot = null;
            }
            ReleaseAllInputs();

            // 再開待ち
            await _resumeSignal.WaitAsync(stopCt);

            // 再開: OSC 再送・キー/ボタン再押下
            if (slot is not null)
            {
                _activeSlot = slot;
                SendCommand(slot);
            }
            RepressAllInputs();
        }
    }

    public Task PauseAsync()
    {
        if (IsPaused || !IsPlaying) return Task.CompletedTask;
        IsPaused = true;
        try { _pauseCts?.Cancel(); } catch (ObjectDisposedException) { }
        return Task.CompletedTask;
    }

    public Task ResumeAsync()
    {
        if (!IsPaused) return Task.CompletedTask;
        IsPaused = false;
        _resumeSignal.Release();
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        IsPaused = false;
        _stopCts?.Cancel();
        try { await _playTask; } catch { }
    }

    private void SendCommand(OscSlot slot)
    {
        switch (slot)
        {
            case FloatSlot f: oscSender.SendFloat(f.Address, f.Value); break;
            case IntSlot n: oscSender.SendInt(n.Address, n.Value); break;
            case BoolSlot b: oscSender.SendBool(b.Address, b.Value); break;
            case StringSlot s: oscSender.SendString(s.Address, s.Value); break;
        }
    }

    private async Task SendTransitionAsync(OscSlot slot, CancellationToken stopCt)
    {
        const int StepMs = 50;
        int totalMs = slot.GetDurationMs();
        TransitionMode mode = slot switch
        {
            FloatSlot f => f.TransitionMode,
            IntSlot n => n.TransitionMode,
            _ => throw new UnreachableException(),
        };

        SendTransitionValue(slot, 0f, mode);

        int elapsed = 0;
        while (elapsed < totalMs)
        {
            stopCt.ThrowIfCancellationRequested();

            int stepMs = Math.Min(StepMs, totalMs - elapsed);
            CancellationTokenSource pauseCts = CancellationTokenSource.CreateLinkedTokenSource(stopCt);
            _pauseCts = pauseCts;
            if (IsPaused) pauseCts.Cancel();

            long startTick = Environment.TickCount64;
            try
            {
                await Task.Delay(stepMs, pauseCts.Token);
                _pauseCts = null;
                pauseCts.Dispose();
                elapsed += stepMs;
            }
            catch (OperationCanceledException) when (!stopCt.IsCancellationRequested)
            {
                elapsed += (int)Math.Min(Environment.TickCount64 - startTick, (long)stepMs);
                _pauseCts = null;
                pauseCts.Dispose();

                if (elapsed < totalMs)
                {
                    ResetSlotValue(slot);
                    _activeSlot = null;
                    ReleaseAllInputs();
                    await _resumeSignal.WaitAsync(stopCt);
                    _activeSlot = slot;
                    RepressAllInputs();
                    SendTransitionValue(slot, (float)elapsed / totalMs, mode);
                }
                continue;
            }

            float t = elapsed >= totalMs ? 1f : (float)elapsed / totalMs;
            SendTransitionValue(slot, t, mode);
        }
    }

    private void SendTransitionValue(OscSlot slot, float t, TransitionMode mode)
    {
        float eased = mode switch
        {
            TransitionMode.EaseIn => t * t,
            TransitionMode.EaseOut => 1f - (1f - t) * (1f - t),
            TransitionMode.EaseInOut => t < 0.5f ? 2f * t * t : 1f - MathF.Pow(-2f * t + 2f, 2f) / 2f,
            _ => t,
        };
        switch (slot)
        {
            case FloatSlot f:
                oscSender.SendFloat(f.Address, f.TransitionFromValue + (f.TransitionToValue - f.TransitionFromValue) * eased);
                break;
            case IntSlot n:
                oscSender.SendInt(n.Address, (int)Math.Round(n.TransitionFromValue + (double)(n.TransitionToValue - n.TransitionFromValue) * eased));
                break;
        }
    }

    private void ResetSlotValue(OscSlot slot)
    {
        switch (slot)
        {
            case FloatSlot f: oscSender.SendFloat(f.Address, 0f); break;
            case IntSlot n: oscSender.SendInt(n.Address, 0); break;
            case BoolSlot b: oscSender.SendBool(b.Address, false); break;
            case StringSlot s: oscSender.SendString(s.Address, string.Empty); break;
        }
    }

    private void StartKeyRepeat(int virtualKey, CancellationToken stopCt)
    {
        StopKeyRepeat(virtualKey); // 二重登録防止
        var cts = CancellationTokenSource.CreateLinkedTokenSource(stopCt);
        _repeatTasks[virtualKey] = cts;
        _ = RunRepeatLoopAsync(virtualKey, cts.Token);
    }

    public void SetKeyRepeatSettings(KeyRepeatSettings settings) => _keyRepeatSettings = settings;

    private async Task RunRepeatLoopAsync(int virtualKey, CancellationToken ct)
    {
        try
        {
            if (!_keyRepeatSettings.IsEnabled) return;

            if (_keyRepeatSettings.InitialDelayMs > 0)
                await Task.Delay(_keyRepeatSettings.InitialDelayMs, ct);

            int intervalMs = Math.Max(1, _keyRepeatSettings.IntervalMs);
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(intervalMs, ct);
                lock (_inputLock)
                {
                    if (!IsPaused && _pressedKeys.Contains(virtualKey))
                        keyboardSender.SendKey(virtualKey, KeyAction.Press);
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private void StopKeyRepeat(int virtualKey)
    {
        if (_repeatTasks.Remove(virtualKey, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    private void StopAllKeyRepeats()
    {
        foreach (var cts in _repeatTasks.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }
        _repeatTasks.Clear();
    }

    private void FlushPendingReleases()
    {
        if (_pendingKeyRelease != null)
        {
            keyboardSender.SendKey(_pendingKeyRelease.VirtualKey, KeyAction.Release);
            lock (_inputLock) { _pressedKeys.Remove(_pendingKeyRelease.VirtualKey); }
            _pendingKeyRelease = null;
        }
        if (_pendingMouseRelease != null)
        {
            mouseSender.SendMouseButton(_pendingMouseRelease.Button, KeyAction.Release);
            lock (_inputLock) { _pressedMouseButtons.Remove(_pendingMouseRelease.Button); }
            _pendingMouseRelease = null;
        }
    }

    private void ReleaseAllInputs()
    {
        lock (_inputLock)
        {
            foreach (int vk in _pressedKeys)
                keyboardSender.SendKey(vk, KeyAction.Release);
            foreach (MouseButton btn in _pressedMouseButtons)
                mouseSender.SendMouseButton(btn, KeyAction.Release);
        }
    }

    private void RepressAllInputs()
    {
        lock (_inputLock)
        {
            foreach (int vk in _pressedKeys)
                keyboardSender.SendKey(vk, KeyAction.Press);
            foreach (MouseButton btn in _pressedMouseButtons)
                mouseSender.SendMouseButton(btn, KeyAction.Press);
        }
    }

    private void ClearInputState()
    {
        StopAllKeyRepeats();
        ReleaseAllInputs();
        lock (_inputLock)
        {
            _pressedKeys.Clear();
            _pressedMouseButtons.Clear();
        }
    }

    public void Dispose()
    {
        _stopCts?.Dispose();
        _pauseCts?.Dispose();
        _resumeSignal.Dispose();
    }
}
