﻿using System;

namespace EventStore.ClientAPI
{
    internal static class Consts
    {
        public const int DefaultMaxQueueSize = 5000;
        public const int DefaultMaxConcurrentItems = 5000;
        public const int DefaultMaxOperationRetries = 10;
        public const int DefaultMaxReconnections = 10;

        public const bool DefaultRequireMaster = true;

        public static readonly TimeSpan DefaultReconnectionDelay = TimeSpan.FromMilliseconds(100);
        public static readonly TimeSpan DefaultQueueTimeout = TimeSpan.Zero; // Unlimited
        public static readonly TimeSpan DefaultOperationTimeout = TimeSpan.FromSeconds(7);
        public static readonly TimeSpan DefaultOperationTimeoutCheckPeriod = TimeSpan.FromSeconds(1);

        public static readonly TimeSpan TimerPeriod = TimeSpan.FromMilliseconds(200);
        public const int MaxReadSize = 4096;
        public const int DefaultMaxClusterDiscoverAttempts = 10;
        public const int DefaultClusterManagerExternalHttpPort = 30778;

        public const int CatchUpDefaultReadBatchSize = 500;
        public const int CatchUpDefaultMaxPushQueueSize = 10000;

        public const string PersistentSubscriptionAlreadyExists = "Subscription group {0} on stream {1} already exists";
        public const string PersistentSubscriptionDoesNotExist = "Subscription group {0} on stream {1} does not exist";

        public const int True = 1;
        public const int False = 0;

        public const uint TooBigOrNegative = int.MaxValue;
        public const ulong TooBigOrNegativeUL = long.MaxValue;
    }
}