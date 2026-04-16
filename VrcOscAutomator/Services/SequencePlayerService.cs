using VrcOscAutomator.Interfaces;
using VrcOscAutomator.Models;

namespace VrcOscAutomator.Services;

public sealed class SequencePlayerService(IOscSender oscSender) : ISequencePlayer
{
    private CancellationTokenSource? _stopCts;

    private CancellationTokenSource? _pauseCts;

    private readonly SemaphoreSlim _resumeSignal = new(0, 1);

    private Task _playTask = Task.CompletedTask;

    private OscSlot? _activeSlot;

    public bool IsPlaying => !_playTask.IsCompleted;
    public bool IsPaused { get; private set; }
    public int CurrentSlotIndex { get; private set; } = -1;

    public async Task PlayAsync(IReadOnlyList<SequenceSlot> slots, bool loop, IProgress<int>? slotProgress, CancellationToken cancellationToken)
    {
        // 前回の残留シグナルをクリア
        while (_resumeSignal.CurrentCount > 0)
            _resumeSignal.Wait(0, CancellationToken.None);
        IsPaused = false;

        _stopCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _playTask = ExecuteAsync(slots, loop, slotProgress, _stopCts.Token);
        await _playTask;
    }

    private async Task ExecuteAsync(IReadOnlyList<SequenceSlot> slots, bool loop, IProgress<int>? slotProgress, CancellationToken stopCt)
    {
        // ループスタック: (ループ開始インデックス, 残り繰り返し回数)
        var loopStack = new Stack<(int startIndex, int remaining)>();

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
                        await _resumeSignal.WaitAsync(stopCt);

                    SequenceSlot slot = slots[i];

                    // ── 繰り返し開始マーカー ──────────────────────────
                    if (slot is LoopBeginSlot lb)
                    {
                        loopStack.Push((i, lb.RepeatCount));
                        i++;
                        continue;
                    }

                    // ── 繰り返し終了マーカー ──────────────────────────
                    if (slot is LoopEndSlot)
                    {
                        if (loopStack.Count > 0)
                        {
                            (int startIdx, int remaining) = loopStack.Pop();
                            // remaining == 0: 無限ループ、remaining > 1: まだ残りあり
                            if (remaining == 0 || remaining > 1)
                            {
                                loopStack.Push((startIdx, remaining == 0 ? 0 : remaining - 1));
                                i = startIdx + 1;
                                continue;
                            }
                        }
                        i++;
                        continue;
                    }

                    // ── 通常スロット（WaitSlot / OscSlot）──────────────
                    CurrentSlotIndex = i;
                    slotProgress?.Report(i);

                    OscSlot? activeOsc = slot as OscSlot;
                    int durationMs = slot switch
                    {
                        OscSlot osc => osc.DurationMs,
                        WaitSlot w => w.DurationMs,
                        _ => 0,
                    };

                    if (activeOsc is not null)
                    {
                        _activeSlot = activeOsc;
                        SendCommand(activeOsc);
                    }

                    await SlotDelayAsync(activeOsc, durationMs, stopCt);

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
            if (_activeSlot is { ResetOnComplete: true }) ResetSlotValue(_activeSlot);
            _activeSlot = null;
            CurrentSlotIndex = -1;
            slotProgress?.Report(-1);
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

            // 一時停止中
            if (slot is { ResetOnComplete: true })
            {
                ResetSlotValue(slot);
                _activeSlot = null;
            }

            // 再開待ち
            await _resumeSignal.WaitAsync(stopCt);

            // 再開
            if (slot is not null)
            {
                _activeSlot = slot;
                SendCommand(slot);
            }
        }
    }

    public Task PauseAsync()
    {
        if (IsPaused || !IsPlaying) return Task.CompletedTask;
        IsPaused = true;
        _pauseCts?.Cancel();
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

    public void Dispose()
    {
        _stopCts?.Dispose();
        _pauseCts?.Dispose();
        _resumeSignal.Dispose();
    }
}
