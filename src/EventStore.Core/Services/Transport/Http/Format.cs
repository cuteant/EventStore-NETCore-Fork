﻿using System;
using EventStore.Core.Data;
using EventStore.Core.Messages;
using EventStore.Core.Messaging;
using EventStore.Core.Services.Transport.Http.Controllers;
using EventStore.Transport.Http;
using ReadStreamResult = EventStore.Core.Data.ReadStreamResult;
using EventStore.Common.Utils;

namespace EventStore.Core.Services.Transport.Http
{
    public static class Format
    {
        public static string TextMessage(HttpResponseFormatterArgs entity, Message message)
        {
            var textMessage = message as HttpMessage.TextMessage;
            return textMessage is object ? entity.ResponseCodec.To(textMessage) : String.Empty;
        }

        public static object EventEntry(HttpResponseFormatterArgs entity, Message message, EmbedLevel embed)
        {
            var msg = message as ClientMessage.ReadEventCompleted;
            if (msg is null || msg.Result != ReadEventResult.Success || msg.Record.Event is null)
                return entity.ResponseCodec.To(Empty.Result);

            switch (entity.ResponseCodec.ContentType)
            {
                case ContentType.Atom:
                case ContentType.AtomJson:
                case ContentType.Html:
                    return entity.ResponseCodec.To(Convert.ToEntry(msg.Record, entity.ResponseUrl, embed, singleEntry: true));
                default:
                    return AutoEventConverter.SmartFormat(msg.Record, entity.ResponseCodec);
            }
        }

        public static string GetStreamEventsBackward(HttpResponseFormatterArgs entity, Message message, EmbedLevel embed, bool headOfStream)
        {
            var msg = message as ClientMessage.ReadStreamEventsBackwardCompleted;
            if (msg is null || msg.Result != ReadStreamResult.Success)
                return String.Empty;

            return entity.ResponseCodec.To(Convert.ToStreamEventBackwardFeed(msg, entity.ResponseUrl, embed, headOfStream));
        }

        public static string GetStreamEventsForward(HttpResponseFormatterArgs entity, Message message, EmbedLevel embed)
        {
            var msg = message as ClientMessage.ReadStreamEventsForwardCompleted;
            if (msg is null || msg.Result != ReadStreamResult.Success)
                return String.Empty;
                
            return entity.ResponseCodec.To(Convert.ToStreamEventForwardFeed(msg, entity.ResponseUrl, embed));
        }

        public static string ReadAllEventsBackwardCompleted(HttpResponseFormatterArgs entity, Message message, EmbedLevel embed)
        {
            var msg = message as ClientMessage.ReadAllEventsBackwardCompleted;
            if (msg is null || msg.Result != ReadAllResult.Success)
                return String.Empty;

            return entity.ResponseCodec.To(Convert.ToAllEventsBackwardFeed(msg, entity.ResponseUrl, embed));
        }

        public static string ReadAllEventsForwardCompleted(HttpResponseFormatterArgs entity, Message message, EmbedLevel embed)
        {
            var msg = message as ClientMessage.ReadAllEventsForwardCompleted;
            if (msg is null || msg.Result != ReadAllResult.Success)
                return String.Empty;

            return entity.ResponseCodec.To(Convert.ToAllEventsForwardFeed(msg, entity.ResponseUrl, embed)); 
        }

        public static string WriteEventsCompleted(HttpResponseFormatterArgs entity, Message message)
        {
            return String.Empty;
        }

        public static string DeleteStreamCompleted(HttpResponseFormatterArgs entity, Message message)
        {
            return String.Empty;
        }

        public static string GetFreshStatsCompleted(HttpResponseFormatterArgs entity, Message message)
        {
            var completed = message as MonitoringMessage.GetFreshStatsCompleted;
            if (completed is null || !completed.Success)
                return String.Empty;

            return entity.ResponseCodec.To(completed.Stats);
        }

        public static string GetReplicationStatsCompleted(HttpResponseFormatterArgs entity, Message message)
        {
            if (message.GetType() != typeof(ReplicationMessage.GetReplicationStatsCompleted))
                throw new Exception(string.Format("Unexpected type of Response message: {0}, expected: {1}",
                                                    message.GetType().Name,
                                                    typeof(ReplicationMessage.GetReplicationStatsCompleted).Name));
            var completed = message as ReplicationMessage.GetReplicationStatsCompleted;
            return entity.ResponseCodec.To(completed.ReplicationStats);
        }

        public static string GetFreshTcpConnectionStatsCompleted(HttpResponseFormatterArgs entity, Message message)
        {
            var completed = message as MonitoringMessage.GetFreshTcpConnectionStatsCompleted;
            if (completed is null)
                return String.Empty;

            return entity.ResponseCodec.To(completed.ConnectionStats);
        }

        public static string SendGossip(HttpResponseFormatterArgs entity, Message message)
        {
            if (message.GetType() != typeof(GossipMessage.SendGossip))
                throw new Exception(string.Format("Unexpected type of response message: {0}, expected: {1}",
                                                  message.GetType().Name,
                                                  typeof(GossipMessage.SendGossip).Name));

            var sendGossip = message as GossipMessage.SendGossip;
            return sendGossip is object
                       ? entity.ResponseCodec.To(new ClusterInfoDto(sendGossip.ClusterInfo, sendGossip.ServerEndPoint))
                       : string.Empty;
        }

        public static string ReadNextNPersistentMessagesCompleted(HttpResponseFormatterArgs entity, Message message, string streamId, string groupName, int count, EmbedLevel embed)
        {
            var msg = message as ClientMessage.ReadNextNPersistentMessagesCompleted;
            if (msg is null || msg.Result != ClientMessage.ReadNextNPersistentMessagesCompleted.ReadNextNPersistentMessagesResult.Success)
                return String.Empty;

            return entity.ResponseCodec.To(Convert.ToNextNPersistentMessagesFeed(msg, entity.ResponseUrl, streamId, groupName, count, embed));
        }

        public static string GetDescriptionDocument(HttpResponseFormatterArgs entity, string streamId, string[] persistentSubscriptionStats)
        {
            return entity.ResponseCodec.To(Convert.ToDescriptionDocument(entity.RequestedUrl, streamId, persistentSubscriptionStats));
        }
    }
}
