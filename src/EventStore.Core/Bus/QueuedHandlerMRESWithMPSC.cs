﻿using System;
using System.Threading;
using EventStore.Common.Utils;
using EventStore.Core.Messages;
using EventStore.Core.Messaging;
using EventStore.Core.Services.Monitoring.Stats;
using Microsoft.Extensions.Logging;

namespace EventStore.Core.Bus
{
  /// <summary>Lightweight in-memory queue with a separate thread in which it passes messages
  /// to the consumer. It also tracks statistics about the message processing to help
  /// in identifying bottlenecks</summary>
  public class QueuedHandlerMresWithMpsc : IQueuedHandler, IHandle<Message>, IPublisher, IMonitoredQueue, IThreadSafePublisher
  {
    private static readonly ILogger Log = TraceLogger.GetLogger<QueuedHandlerMresWithMpsc>();

    public string Name => _queueStats.Name;

    private readonly IHandle<Message> _consumer;

    private readonly bool _watchSlowMsg;
    private readonly TimeSpan _slowMsgThreshold;

    // assuming 8bytes per object ref its ~1MB.
    private readonly MPSCMessageQueue _queue = new MPSCMessageQueue(128 * 1024);

    private readonly ManualResetEventSlim _msgAddEvent = new ManualResetEventSlim(false);

    private Thread _thread;
    private volatile bool _stop;
    private volatile bool _starving;
    private readonly ManualResetEventSlim _stopped = new ManualResetEventSlim(true);
    private readonly TimeSpan _threadStopWaitTimeout;

    private readonly QueueMonitor _queueMonitor;
    private readonly QueueStatsCollector _queueStats;

    public QueuedHandlerMresWithMpsc(IHandle<Message> consumer,
        string name,
        bool watchSlowMsg = true,
        TimeSpan? slowMsgThreshold = null,
        TimeSpan? threadStopWaitTimeout = null,
        string groupName = null)
    {
      Ensure.NotNull(consumer, nameof(consumer));
      Ensure.NotNull(name, nameof(name));

      _consumer = consumer;

      _watchSlowMsg = watchSlowMsg;
      _slowMsgThreshold = slowMsgThreshold ?? InMemoryBus.DefaultSlowMessageThreshold;
      _threadStopWaitTimeout = threadStopWaitTimeout ?? QueuedHandler.DefaultStopWaitTimeout;

      _queueMonitor = QueueMonitor.Default;
      _queueStats = new QueueStatsCollector(name, groupName);
    }

    public void Start()
    {
      if (_thread != null) { throw new InvalidOperationException("Already a thread running."); }

      _queueMonitor.Register(this);

      _stopped.Reset();

      _thread = new Thread(ReadFromQueue) { IsBackground = true, Name = Name };
      _thread.Start();
    }

    public void Stop()
    {
      _stop = true;
      if (!_stopped.Wait(_threadStopWaitTimeout))
      {
        throw new TimeoutException($"Unable to stop thread '{Name}'.");
      }
    }

    public void RequestStop()
    {
      _stop = true;
    }

    private void ReadFromQueue(object o)
    {
      _queueStats.Start();
      Thread.BeginThreadAffinity(); // ensure we are not switching between OS threads. Required at least for v8.

      var traceEnabled = Log.IsTraceLevelEnabled();
      var batch = new Message[128];
      while (!_stop)
      {
        Message msg = null;
        try
        {
          if (_queue.TryDequeue(batch, out QueueBatchDequeueResult dequeueResult) == false)
          {
            _starving = true;

            _queueStats.EnterIdle();
            _msgAddEvent.Wait(100);
            _msgAddEvent.Reset();

            _starving = false;
          }
          else
          {
            var estimatedQueueCount = dequeueResult.EstimateCurrentQueueCount;

            for (var i = 0; i < dequeueResult.DequeueCount; i++)
            {
              try
              {
                msg = batch[i];


                _queueStats.EnterBusy();
#if DEBUG
                _queueStats.Dequeued(msg);
#endif

                _queueStats.ProcessingStarted(msg.GetType(), estimatedQueueCount);

                if (_watchSlowMsg)
                {
                  var start = DateTime.UtcNow;

                  _consumer.Handle(msg);

                  var elapsed = DateTime.UtcNow - start;
                  if (elapsed > _slowMsgThreshold)
                  {
                    if (Log.IsTraceLevelEnabled())
                    {
                      Log.LogTrace("SLOW QUEUE MSG [{0}]: {1} - {2}ms. Q: {3}/{4}.",
                          Name, _queueStats.InProgressMessage.Name, (int)elapsed.TotalMilliseconds,
                          estimatedQueueCount,
                          _queue.EstimageCurrentQueueCount());
                    }
                    if (elapsed > QueuedHandler.VerySlowMsgThreshold && !(msg is SystemMessage.SystemInit))
                    {
                      Log.LogError("---!!! VERY SLOW QUEUE MSG [{0}]: {1} - {2}ms. Q: {3}/{4}.",
                          Name, _queueStats.InProgressMessage.Name, (int)elapsed.TotalMilliseconds,
                          estimatedQueueCount, _queue.EstimageCurrentQueueCount());
                    }
                  }
                }
                else
                {
                  _consumer.Handle(msg);
                }
              }
              catch (Exception ex)
              {
                Log.LogError(ex, "Error while processing message {0} in queued handler '{1}'.", msg, Name);
              }

              estimatedQueueCount -= 1;
              _queueStats.ProcessingEnded(1);
            }
          }
        }
        catch (Exception ex)
        {
          Log.LogError(ex, "Error while processing message {0} in queued handler '{1}'.", msg, Name);
        }
      }
      _queueStats.Stop();

      _stopped.Set();
      _queueMonitor.Unregister(this);
      Thread.EndThreadAffinity();
    }

    public void Publish(Message message)
    {
      //Ensure.NotNull(message, "message");
#if DEBUG
      _queueStats.Enqueued();
#endif
      _queue.Enqueue(message);
      if (_starving) { _msgAddEvent.Set(); }
    }

    public void Handle(Message message)
    {
      Publish(message);
    }

    public QueueStats GetStatistics()
    {
      return _queueStats.GetStatistics(_queue.EstimageCurrentQueueCount());
    }
  }
}