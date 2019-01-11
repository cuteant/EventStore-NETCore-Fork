﻿using System;
using System.Runtime.CompilerServices;

namespace EventStore.ClientAPI
{
    internal static class ConsumerThrowHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException(SubscriptionDropReason dropReason)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException("DropReason", dropReason, null);
            }
        }
    }
}
