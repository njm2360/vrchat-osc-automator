using VrcOscAutomator.Interfaces;
using VrcOscAutomator.Models;

namespace VrcOscAutomator.Services;

public sealed class SequencePlayerService(IOscSender oscSender) : ISequencePlayer
{
    private CancellationTokenSource? _stopCts;

    private CancellationTokenSource? _pauseCts;

    private readonly SemaphoreSlim _resumeSignal = new(0, 1);

    private Task _playTask = Task.CompletedTask;

    private SequenceSlot? _activeSlot;

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
                    if (slot.SlotType == SlotType.LoopBegin)
                    {
                        loopStack.Push((i, slot.RepeatCount));
                        i++;
                        continue;
                    }

                    // ── 繰り返し終了マーカー ──────────────────────────
                    if (slot.SlotType == SlotType.LoopEnd)
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

                    // ── 通常スロット ──────────────────────────────────
                    CurrentSlotIndex = i;
                    slotProgress?.Report(i);

                    _activeSlot = slot;
                    SendCommand(slot);

                    await SlotDelayAsync(slot, slot.DurationMs, stopCt);

                    if (slot.ResetOnComplete) ResetSlotValue(slot);
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
            if (_activeSlot is not null && _activeSlot.ResetOnComplete) ResetSlotValue(_activeSlot);
            _activeSlot = null;
            CurrentSlotIndex = -1;
            slotProgress?.Report(-1);
        }
    }

    private async Task SlotDelayAsync(SequenceSlot slot, int totalMs, CancellationToken stopCt)
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
            if (slot.ResetOnComplete)
            {
                ResetSlotValue(slot);
                _activeSlot = null;
            }

            // 再開待ち
            await _resumeSignal.WaitAsync(stopCt);

            // 再開
            _activeSlot = slot;
            SendCommand(slot);
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

    private void SendCommand(SequenceSlot slot)
    {
        if (slot.Address is not { Length: > 0 }) return;
        switch (slot.ValueType)
        {
            case OscValueType.Int: oscSender.SendInt(slot.Address, (int)slot.Value); break;
            case OscValueType.Bool: oscSender.SendBool(slot.Address, slot.Value != 0f); break;
            case OscValueType.String: oscSender.SendString(slot.Address, slot.StringValue); break;
            default: oscSender.SendFloat(slot.Address, slot.Value); break;
        }
    }

    private void ResetSlotValue(SequenceSlot slot)
    {
        if (slot.Address is not { Length: > 0 }) return;
        switch (slot.ValueType)
        {
            case OscValueType.Int: oscSender.SendInt(slot.Address, 0); break;
            case OscValueType.Bool: oscSender.SendBool(slot.Address, false); break;
            case OscValueType.String: oscSender.SendString(slot.Address, string.Empty); break;
            default: oscSender.SendFloat(slot.Address, 0f); break;
        }
    }

    public void Dispose()
    {
        _stopCts?.Dispose();
        _pauseCts?.Dispose();
        _resumeSignal.Dispose();
    }
}
