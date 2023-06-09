﻿using DinkToPdf;
using DinkToPdf.Contracts;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace RazorDinkToPdf.Internal;

internal sealed class StaSynchronizedConverter : BasicConverter
{
    private static readonly IConverter instance = new StaSynchronizedConverter(new PdfTools());

    private readonly BlockingCollection<Task> conversions = new BlockingCollection<Task>();
    private bool kill = false;
    private readonly object startLock = new object();
    private Thread? conversionThread;

    static StaSynchronizedConverter() { }

    private StaSynchronizedConverter(ITools tools)
        : base(tools)
    {

    }

    public static IConverter Instance => instance;

    public override byte[] Convert(IDocument document)
    {
        return Invoke(() => base.Convert(document));
    }

    private TResult Invoke<TResult>(Func<TResult> @delegate)
    {
        StartThread();

        Task<TResult> task = new Task<TResult>(@delegate);

        lock (task)
        {
            // add task to blocking collection
            conversions.Add(task);

            // wait for task to be processed by conversion thread 
            Monitor.Wait(task);
        }

        // throw exception that happened during conversion
        if (task.Exception != null)
        {
            throw task.Exception;
        }

        return task.Result;
    }

    private void StartThread()
    {
        lock (startLock)
        {
            if (conversionThread == null)
            {
                conversionThread = new Thread(Run)
                {
                    IsBackground = true,
                    Name = "wkhtmltopdf worker thread"
                };

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    conversionThread.SetApartmentState(ApartmentState.STA);
                }

                kill = false;

                conversionThread.Start();
            }
        }
    }

    private void StopThread()
    {
        lock (startLock)
        {
            if (conversionThread != null)
            {
                kill = true;

                while (conversionThread.ThreadState == ThreadState.Stopped)
                { }

                conversionThread = null;
            }
        }
    }

    private void Run()
    {
        while (!kill)
        {
            // get next conversion task from blocking collection
            Task task = conversions.Take();

            lock (task)
            {
                // run task on thread that called RunSynchronously method
                task.RunSynchronously();

                // notify caller thread that task is completed
                Monitor.Pulse(task);
            }
        }
    }

    ~StaSynchronizedConverter()
    {
        try
        {
            StopThread();
        }
        catch
        {
            // nothing to do since we're aborting anyway
        }
    }
}