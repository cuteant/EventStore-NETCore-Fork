﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventStore.ClientAPI.Serialization;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI.Internal
{
  internal sealed class EventStoreNodeConnectionMultiplexer : IEventStoreConnectionMultiplexer
  {
    private readonly IList<IEventStoreConnection2> _innerConnections;
    private readonly int _connectionCount;

    internal EventStoreNodeConnectionMultiplexer(IList<IEventStoreConnection2> connections)
    {
      _innerConnections = connections;
      _connectionCount = connections.Count;
    }

    public Task ConnectAsync()
    {
      return Task.WhenAll(_innerConnections.Select(_ => _.ConnectAsync()).ToArray());
    }

    public void Close()
    {
      foreach (var conn in _innerConnections)
      {
        conn.Close();
      }
    }

    public Task<DeleteResult> DeleteStreamAsync(string stream, long expectedVersion, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].DeleteStreamAsync(stream, expectedVersion, userCredentials);
    }

    public Task<DeleteResult> DeleteStreamAsync(string stream, long expectedVersion, bool hardDelete, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].DeleteStreamAsync(stream, expectedVersion, hardDelete, userCredentials);
    }

    public Task<WriteResult> AppendToStreamAsync(string stream, long expectedVersion, params EventData[] events)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].AppendToStreamAsync(stream, expectedVersion, events);
    }

    public Task<WriteResult> AppendToStreamAsync(string stream, long expectedVersion, UserCredentials userCredentials, params EventData[] events)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].AppendToStreamAsync(stream, expectedVersion, userCredentials, events);
    }

    public Task<WriteResult> AppendToStreamAsync(string stream, long expectedVersion, IEnumerable<EventData> events, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].AppendToStreamAsync(stream, expectedVersion, events, userCredentials);
    }

    public Task<ConditionalWriteResult> ConditionalAppendToStreamAsync(string stream, long expectedVersion, IEnumerable<EventData> events, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].ConditionalAppendToStreamAsync(stream, expectedVersion, events, userCredentials);
    }

    public Task<EventStoreTransaction> StartTransactionAsync(string stream, long expectedVersion, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].StartTransactionAsync(stream, expectedVersion, userCredentials);
    }

    public EventStoreTransaction ContinueTransaction(string stream, long transactionId, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].ContinueTransaction(transactionId, userCredentials);
    }

    public Task<EventReadResult> ReadEventAsync(string stream, long eventNumber, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].ReadEventAsync(stream, eventNumber, resolveLinkTos, userCredentials);
    }

    public Task<StreamEventsSlice> ReadStreamEventsForwardAsync(string stream, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].ReadStreamEventsForwardAsync(stream, start, count, resolveLinkTos, userCredentials);
    }

    public Task<StreamEventsSlice> ReadStreamEventsBackwardAsync(string stream, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].ReadStreamEventsBackwardAsync(stream, start, count, resolveLinkTos, userCredentials);
    }

    public Task<EventReadResult<object>> GetEventAsync(string stream, long eventNumber, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].GetEventAsync(stream, eventNumber, resolveLinkTos, userCredentials);
    }

    public Task<StreamEventsSlice<object>> GetStreamEventsForwardAsync(string stream, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].GetStreamEventsForwardAsync(stream, start, count, resolveLinkTos, userCredentials);
    }

    public Task<StreamEventsSlice<object>> GetStreamEventsBackwardAsync(string stream, long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].GetStreamEventsBackwardAsync(stream, start, count, resolveLinkTos, userCredentials);
    }

    public Task<EventReadResult<TEvent>> GetEventAsync<TEvent>(long eventNumber, bool resolveLinkTos, UserCredentials userCredentials = null) where TEvent : class
    {
      var stream = SerializationManager.GetStreamId(typeof(TEvent));
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].GetEventAsync<TEvent>(eventNumber, resolveLinkTos, userCredentials);
    }

    public Task<StreamEventsSlice<TEvent>> GetStreamEventsForwardAsync<TEvent>(long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null) where TEvent : class
    {
      var stream = SerializationManager.GetStreamId(typeof(TEvent));
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].GetStreamEventsForwardAsync<TEvent>(start, count, resolveLinkTos, userCredentials);
    }

    public Task<StreamEventsSlice<TEvent>> GetStreamEventsBackwardAsync<TEvent>(long start, int count, bool resolveLinkTos, UserCredentials userCredentials = null) where TEvent : class
    {
      var stream = SerializationManager.GetStreamId(typeof(TEvent));
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].GetStreamEventsBackwardAsync<TEvent>(start, count, resolveLinkTos, userCredentials);
    }


    public Task<EventStoreSubscription> VolatileSubscribeAsync(string stream, SubscriptionSettings settings,
      Action<EventStoreSubscription, ResolvedEvent<object>> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].VolatileSubscribeAsync(stream, settings, eventAppeared, subscriptionDropped, userCredentials);
    }
    public Task<EventStoreSubscription> VolatileSubscribeAsync(string stream, SubscriptionSettings settings,
      Func<EventStoreSubscription, ResolvedEvent<object>, Task> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].VolatileSubscribeAsync(stream, settings, eventAppeared, subscriptionDropped, userCredentials);
    }
    public Task<EventStoreSubscription> VolatileSubscribeAsync<TEvent>(SubscriptionSettings settings,
      Action<EventStoreSubscription, ResolvedEvent<TEvent>> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null) where TEvent : class
    {
      var stream = SerializationManager.GetStreamId(typeof(TEvent));
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].VolatileSubscribeAsync<TEvent>(settings, eventAppeared, subscriptionDropped, userCredentials);
    }
    public Task<EventStoreSubscription> VolatileSubscribeAsync<TEvent>(SubscriptionSettings settings,
      Func<EventStoreSubscription, ResolvedEvent<TEvent>, Task> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null) where TEvent : class
    {
      var stream = SerializationManager.GetStreamId(typeof(TEvent));
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].VolatileSubscribeAsync<TEvent>(settings, eventAppeared, subscriptionDropped, userCredentials);
    }
    public Task<EventStoreSubscription> VolatileSubscribeAsync<TEvent>(string topic, SubscriptionSettings settings,
      Action<EventStoreSubscription, ResolvedEvent<TEvent>> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null) where TEvent : class
    {
      var stream = SerializationManager.GetStreamId(typeof(TEvent));
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].VolatileSubscribeAsync<TEvent>(topic, settings, eventAppeared, subscriptionDropped, userCredentials);
    }
    public Task<EventStoreSubscription> VolatileSubscribeAsync<TEvent>(string topic, SubscriptionSettings settings,
      Func<EventStoreSubscription, ResolvedEvent<TEvent>, Task> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null) where TEvent : class
    {
      var stream = SerializationManager.GetStreamId(typeof(TEvent));
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].VolatileSubscribeAsync<TEvent>(topic, settings, eventAppeared, subscriptionDropped, userCredentials);
    }

    public Task<EventStoreSubscription> SubscribeToStreamAsync(string stream, SubscriptionSettings settings,
      Action<EventStoreSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].SubscribeToStreamAsync(stream, settings, eventAppeared, subscriptionDropped, userCredentials);
    }
    public Task<EventStoreSubscription> SubscribeToStreamAsync(string stream, SubscriptionSettings settings,
      Func<EventStoreSubscription, ResolvedEvent, Task> eventAppeared,
      Action<EventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].SubscribeToStreamAsync(stream, settings, eventAppeared, subscriptionDropped, userCredentials);
    }

    public EventStoreStreamCatchUpSubscription SubscribeToStreamFrom(
      string stream, long? lastCheckpoint, CatchUpSubscriptionSettings settings,
      Action<EventStoreCatchUpSubscription, ResolvedEvent> eventAppeared,
      Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].SubscribeToStreamFrom(stream, lastCheckpoint, settings, eventAppeared, liveProcessingStarted, subscriptionDropped, userCredentials);
    }

    public EventStoreStreamCatchUpSubscription SubscribeToStreamFrom(
      string stream, long? lastCheckpoint, CatchUpSubscriptionSettings settings,
      Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStoreCatchUpSubscription> liveProcessingStarted = null,
      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].SubscribeToStreamFrom(stream, lastCheckpoint, settings, eventAppearedAsync, liveProcessingStarted, subscriptionDropped, userCredentials);
    }

    public Task<EventStorePersistentSubscriptionBase> ConnectToPersistentSubscriptionAsync(string stream, string groupName,
      ConnectToPersistentSubscriptionSettings settings,
      Action<EventStorePersistentSubscriptionBase, ResolvedEvent> eventAppeared,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].ConnectToPersistentSubscriptionAsync(stream, groupName, settings, eventAppeared, subscriptionDropped, userCredentials);
    }

    public Task<EventStorePersistentSubscriptionBase> ConnectToPersistentSubscriptionAsync(string stream, string groupName,
      ConnectToPersistentSubscriptionSettings settings,
      Func<EventStorePersistentSubscriptionBase, ResolvedEvent, Task> eventAppearedAsync,
      Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = null,
      UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].ConnectToPersistentSubscriptionAsync(stream, groupName, settings, eventAppearedAsync, subscriptionDropped, userCredentials);
    }

    public Task UpdatePersistentSubscriptionAsync(string stream, string groupName, PersistentSubscriptionSettings settings, UserCredentials credentials)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].UpdatePersistentSubscriptionAsync(stream, groupName, settings, credentials);
    }


    public Task CreatePersistentSubscriptionAsync(string stream, string groupName, PersistentSubscriptionSettings settings, UserCredentials credentials)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].CreatePersistentSubscriptionAsync(stream, groupName, settings, credentials);
    }

    public Task DeletePersistentSubscriptionAsync(string stream, string groupName, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].DeletePersistentSubscriptionAsync(stream, groupName, userCredentials);
    }

    public Task<WriteResult> SetStreamMetadataAsync(string stream, long expectedMetastreamVersion, StreamMetadata metadata, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].SetStreamMetadataAsync(stream, expectedMetastreamVersion, metadata, userCredentials);
    }

    public Task<WriteResult> SetStreamMetadataAsync(string stream, long expectedMetastreamVersion, byte[] metadata, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].SetStreamMetadataAsync(stream, expectedMetastreamVersion, metadata, userCredentials);
    }

    public Task<StreamMetadataResult> GetStreamMetadataAsync(string stream, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].GetStreamMetadataAsync(stream, userCredentials);
    }

    public Task<RawStreamMetadataResult> GetStreamMetadataAsRawBytesAsync(string stream, UserCredentials userCredentials = null)
    {
      var index = CalculateConnectionIndex(stream, _connectionCount);
      return _innerConnections[index].GetStreamMetadataAsRawBytesAsync(stream, userCredentials);
    }

    public Task SetSystemSettingsAsync(SystemSettings settings, UserCredentials userCredentials = null)
    {
      return _innerConnections[0].SetSystemSettingsAsync(settings, userCredentials);
    }

    public ConnectionSettings Settings => _innerConnections[0].Settings;
    void IDisposable.Dispose()
    {
      Close();
    }

    private static int CalculateConnectionIndex(string streamId, int count)
    {
      if (string.IsNullOrEmpty(streamId)) { throw new ArgumentNullException(nameof(streamId)); }

      return Math.Abs(streamId.GetHashCode() % count);
    }
  }
}
