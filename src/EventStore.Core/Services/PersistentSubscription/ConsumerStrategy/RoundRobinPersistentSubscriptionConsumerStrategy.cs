﻿using System;
using System.Collections.Generic;
using System.Linq;
using EventStore.Core.Data;

namespace EventStore.Core.Services.PersistentSubscription.ConsumerStrategy
{
    class RoundRobinPersistentSubscriptionConsumerStrategy : IPersistentSubscriptionConsumerStrategy
    {
        protected readonly Queue<PersistentSubscriptionClient> Clients = new Queue<PersistentSubscriptionClient>();

        public virtual string Name
        {
            get { return SystemConsumerStrategies.RoundRobin; }
        }

        public void ClientAdded(PersistentSubscriptionClient client)
        {
            Clients.Enqueue(client);
        }

        public void ClientRemoved(PersistentSubscriptionClient client)
        {
            if (!Clients.Contains(client))
            {
                ThrowHelper.ThrowInvalidOperationException_OnlyAddedClientsCanBeRemoved();
            }
            var temp = Clients.ToList();
            var indexOf = temp.IndexOf(client);
            temp.RemoveAt(indexOf);
            Clients.Clear();
            foreach (var persistentSubscriptionClient in temp)
            {
                Clients.Enqueue(persistentSubscriptionClient);
            }
        }

        public virtual ConsumerPushResult PushMessageToClient(in ResolvedEvent ev, int retryCount)
        {
            for (int i = 0; i < Clients.Count; i++)
            {
                var c = Clients.Dequeue();
                var pushed = c.Push(ev, retryCount);
                Clients.Enqueue(c);
                if (pushed)
                {
                    return ConsumerPushResult.Sent;
                }
            }
            return ConsumerPushResult.NoMoreCapacity;
        }
    }
}