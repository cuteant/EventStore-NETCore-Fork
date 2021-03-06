﻿using System;
using System.Threading;
using Es.SharedModels;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace ES.Producer
{
    class Program
    {
        static void Main(string[] args)
        {
            var logFactory = new LoggerFactory();
            logFactory.AddNLog();
            TraceLogger.Initialize(logFactory);


            var connStr = "ConnectTo=tcp://admin:changeit@localhost:1113";
            var connSettings = ConnectionSettings.Create().KeepReconnecting().KeepRetrying();
            using (var conn = EventStoreConnection.Create(connStr, connSettings))
            {
                conn.Connect();

                for (var x = 0; x < 100; x++)
                {
                    if (x % 2 == 0)
                    {
                        conn.PublishEventAsync(new Cat { Name = "Cat-" + x, Meow = $"meowing......" });
                        //conn.PublishEventAsync("00", new Cat { Name = "Cat-" + x, Meow = $"meowing......" });
                    }
                    else
                    {
                        conn.PublishEventAsync(new Dog { Name = "Dog-" + x, Bark = $"barking......" });
                        //conn.PublishEventAsync("00", new Dog { Name = "Dog-" + x, Bark = $"barking......" });
                    }
                    Console.WriteLine("event " + x + " written.");
                    Thread.Sleep(1000);
                }
            }

            Console.WriteLine("按任意键退出！");
            Console.ReadKey();
        }
    }
}
