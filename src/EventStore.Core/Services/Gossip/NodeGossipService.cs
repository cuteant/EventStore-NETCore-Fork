﻿using System;
using EventStore.Core.Bus;
using EventStore.Core.Cluster;
using EventStore.Core.Data;
using EventStore.Core.Services.Storage.EpochManager;
using EventStore.Core.TransactionLog.Checkpoint;

namespace EventStore.Core.Services.Gossip
{
    public class NodeGossipService: GossipServiceBase
    {
        private readonly ICheckpoint _writerCheckpoint;
        private readonly ICheckpoint _chaserCheckpoint;
        private readonly IEpochManager _epochManager;
        private readonly Func<long> _getLastCommitPosition;
        private readonly int _nodePriority;

        public NodeGossipService(IPublisher bus,
                                 IGossipSeedSource gossipSeedSource,
                                 VNodeInfo nodeInfo,
                                 ICheckpoint writerCheckpoint,
                                 ICheckpoint chaserCheckpoint,
                                 IEpochManager epochManager,
                                 Func<long> getLastCommitPosition,
                                 int nodePriority,
                                 TimeSpan interval,
                                 TimeSpan allowedTimeDifference)
            : base(bus, gossipSeedSource, nodeInfo, interval, allowedTimeDifference)
        {
            if (writerCheckpoint is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.writerCheckpoint); }
            if (chaserCheckpoint is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.chaserCheckpoint); }
            if (epochManager is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.epochManager); }
            if (getLastCommitPosition is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.getLastCommitPosition); }

            _writerCheckpoint = writerCheckpoint;
            _chaserCheckpoint = chaserCheckpoint;
            _epochManager = epochManager;
            _getLastCommitPosition = getLastCommitPosition;
            _nodePriority = nodePriority;
        }

        protected override MemberInfo GetInitialMe()
        {
            var lastEpoch = _epochManager.GetLastEpoch();
            return MemberInfo.ForVNode(NodeInfo.InstanceId,
                                       DateTime.UtcNow,
                                       VNodeState.Unknown,
                                       true,
                                       NodeInfo.InternalTcp,
                                       NodeInfo.InternalSecureTcp,
                                       NodeInfo.ExternalTcp,
                                       NodeInfo.ExternalSecureTcp,
                                       NodeInfo.InternalHttp,
                                       NodeInfo.ExternalHttp,
                                       _getLastCommitPosition(),
                                       _writerCheckpoint.Read(),
                                       _chaserCheckpoint.Read(),
                                       lastEpoch is null ? -1 : lastEpoch.EpochPosition,
                                       lastEpoch is null ? -1 : lastEpoch.EpochNumber,
                                       lastEpoch is null ? Guid.Empty : lastEpoch.EpochId,
                                       _nodePriority);
        }

        protected override MemberInfo GetUpdatedMe(MemberInfo me)
        {
            return me.Updated(isAlive: true,
                              state: CurrentRole,
                              lastCommitPosition: _getLastCommitPosition(),
                              writerCheckpoint: _writerCheckpoint.ReadNonFlushed(),
                              chaserCheckpoint: _chaserCheckpoint.ReadNonFlushed(),
                              epoch: _epochManager.GetLastEpoch());
        }
    }
}