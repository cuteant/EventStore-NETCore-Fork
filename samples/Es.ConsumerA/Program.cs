﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CuteAnt.AsyncEx;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common;
using EventStore.ClientAPI.SystemData;
using Microsoft.Extensions.Logging;
using Es.SharedModels;

namespace Es.Consumer
{
  class Program
  {
    const string STREAM = "test-animal";
    const string GROUP = "a_test_group_1";

    static void Main(string[] args)
    {
      var logFactory = new LoggerFactory();
      logFactory.AddNLog();
      TraceLogger.Initialize(logFactory);


      var connStr = "ConnectTo=tcp://admin:changeit@localhost:1113";
      var connSettings = ConnectionSettings.Create().KeepReconnecting().KeepRetrying();
      using (var conn = EventStoreConnection.Create(connStr, connSettings))
      {
        conn.ConnectAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        //UpdateSubscription(conn);

        //UpdateStreamMetadata(conn);

        #region PersistentSubscription

        //var settings = new ConnectToPersistentSubscriptionSettings();
        ////var settings = new ConnectToPersistentSubscriptionSettings { MaxDegreeOfParallelismPerBlock = 5 };
        ////var settings = new ConnectToPersistentSubscriptionSettings { BoundedCapacityPerBlock = 2, NumActionBlocks = 5 };

        //conn.ConnectToPersistentSubscription(STREAM, GROUP, settings, async (_, x) =>
        //{
        //  var data = Encoding.ASCII.GetString(x.Event.Data);
        //  if (x.Event.EventNumber % 3 == 0)
        //  {
        //    //var errorMsg = $"error event number: {x.Event.EventNumber}";
        //    //Console.WriteLine(errorMsg);
        //    //throw new InvalidOperationException(errorMsg);
        //  }
        //  Console.WriteLine("Received: " + x.Event.EventStreamId + ":" + x.Event.EventNumber);
        //  Console.WriteLine(data);
        //  await Task.Delay(500);
        //},
        //(subscription, reason, exc) =>
        //{
        //  Console.WriteLine($"subscriptionDropped: reason-{reason} exc:{exc.Message}");
        //});

        #endregion

        #region VolatileSubscription

        //var settings = new SubscriptionSettings();
        var settings = new SubscriptionSettings { MaxDegreeOfParallelismPerBlock = 5 };
        //var settings = new SubscriptionSettings { BoundedCapacityPerBlock = 2, NumActionBlocks = 5 };

        var sub = conn.VolatileSubscribeAsync(STREAM, settings,
            eventAppearedAsync: async (_, x) =>
            {
              Console.WriteLine("Received: " + x.OriginalEvent.EventStreamId + ":" + x.OriginalEvent.EventNumber);
              var msg = x.OriginalEvent.FullEvent.Value;
              if (msg is Cat cat)
              {
                Console.WriteLine("Cat: " + cat.Name + ":" + cat.Meow);
              }
              if (msg is Dog dog)
              {
                Console.WriteLine("Dog: " + dog.Name + ":" + dog.Bark);
              }
              await Task.Delay(500);
            },
            subscriptionDropped: (subscription, reason, exc) =>
            {
              Console.WriteLine($"subscriptionDropped: reason-{reason} exc:{exc.Message}");
            });

        //var sub = conn.VolatileSubscribeAsync<IAnimal>(settings,
        //    eventAppearedAsync: async (_, x) =>
        //    {
        //      Console.WriteLine("Received: " + x.OriginalEvent.EventStreamId + ":" + x.OriginalEvent.EventNumber);
        //      var msg = x.OriginalEvent.FullEvent.Value;
        //      if (msg is Cat cat)
        //      {
        //        Console.WriteLine("Cat: " + cat.Name + ":" + cat.Meow);
        //      }
        //      if (msg is Dog dog)
        //      {
        //        Console.WriteLine("Dog: " + dog.Name + ":" + dog.Bark);
        //      }
        //      await Task.Delay(500);
        //    },
        //    subscriptionDropped: (subscription, reason, exc) =>
        //    {
        //      Console.WriteLine($"subscriptionDropped: reason-{reason} exc:{exc.Message}");
        //    });

        #endregion

        #region CatchupSubscription

        //Note the subscription is subscribing from the beginning every time. You could also save
        //your checkpoint of the last seen event and subscribe to that checkpoint at the beginning.
        //If stored atomically with the processing of the event this will also provide simulated
        //transactional messaging.

        //var settings = CatchUpSubscriptionSettings.Create(20, true);

        ////settings.MaxDegreeOfParallelismPerBlock = 5;

        ////settings.BoundedCapacityPerBlock = 2;
        ////settings.NumActionBlocks = 5;

        //var sub = conn.SubscribeToStreamFrom(STREAM, null, settings,
        //    eventAppearedAsync: async (_, x) =>
        //    {
        //      await TaskConstants.Completed;
        //      var data = Encoding.ASCII.GetString(x.Event.Data);
        //      if (x.Event.EventNumber % 3 == 0)
        //      {
        //        var errorMsg = $"error event number: {x.Event.EventNumber}";
        //        Console.WriteLine(errorMsg);
        //        throw new InvalidOperationException(errorMsg);
        //      }
        //      Console.WriteLine("Received: " + x.Event.EventStreamId + ":" + x.Event.EventNumber);
        //      Console.WriteLine(data);
        //      await Task.Delay(500);
        //    },
        //    subscriptionDropped: (subscription, reason, exc) =>
        //    {
        //      Console.WriteLine($"subscriptionDropped: reason-{reason} exc:{exc.Message}");
        //    });

        #endregion

        Console.WriteLine("waiting for events. press enter to exit");
        Console.ReadKey();
      }
    }

    //private static void UpdateSubscription(IEventStoreConnection conn)
    //{
    //  PersistentSubscriptionSettings settings = PersistentSubscriptionSettings.Create()
    //      .DoNotResolveLinkTos()
    //      .StartFromBeginning();

    //  conn.UpdatePersistentSubscription(STREAM, GROUP, settings);
    //}

    //private static void UpdateStreamMetadata(IEventStoreConnection conn)
    //{
    //  conn.SetStreamMetadataAsync(STREAM, ExpectedVersion.Any, StreamMetadata.Create(1000, TimeSpan.FromMinutes(30)))
    //      .ConfigureAwait(false).GetAwaiter().GetResult();
    //}
  }
}
