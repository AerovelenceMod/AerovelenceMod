using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace AerovelenceMod.Common.Systems.Gas;

internal sealed class GasSimulationWorker : IDisposable
{
    private const int Idle = 0, Pending = 1, Claimed = 2;
    private readonly AutoResetEvent ready = new(false);
    private readonly ManualResetEventSlim completed = new(true, 0);
    private readonly Thread thread;
    private Action<int, int, float> pass;
    private ExceptionDispatchInfo error;
    private int chunks, nextChunk, state;
    private float timeStep;
    private volatile bool stopped;

    public GasSimulationWorker()
    {
        thread = new Thread(Work) { IsBackground = true, Name = "Aerovelence gas" };
        thread.Start();
    }

    public void Run(Action<int, int, float> update, int count, float step)
    {
        pass = update;
        chunks = count;
        timeStep = step;
        nextChunk = -1;
        error = null;
        completed.Reset();
        Volatile.Write(ref state, Pending);
        ready.Set();
        try
        {
            Drain();
        }
        finally
        {
            if (Interlocked.CompareExchange(ref state, Idle, Pending) != Pending)
                completed.Wait();
            pass = null;
        }
        error?.Throw();
    }

    private void Drain()
    {
        int cells = GasFluid.ChunkSize * GasFluid.ChunkSize;
        int chunk;
        while ((chunk = Interlocked.Increment(ref nextChunk)) < chunks)
            pass(chunk * cells, (chunk + 1) * cells, timeStep);
    }

    private void Work()
    {
        while (true)
        {
            ready.WaitOne();
            if (stopped) return;
            if (Interlocked.CompareExchange(ref state, Claimed, Pending) != Pending) continue;
            try
            {
                Drain();
            }
            catch (Exception exception)
            {
                error = ExceptionDispatchInfo.Capture(exception);
            }
            finally
            {
                completed.Set();
            }
        }
    }

    public void Dispose()
    {
        if (stopped) return;
        stopped = true;
        ready.Set();
        thread.Join();
        ready.Dispose();
        completed.Dispose();
    }
}
