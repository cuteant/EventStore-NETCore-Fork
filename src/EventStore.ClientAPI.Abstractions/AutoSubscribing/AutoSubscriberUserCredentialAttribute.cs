﻿using System;

namespace EventStore.ClientAPI.AutoSubscribing
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false)]
    public class AutoSubscriberUserCredentialAttribute : Attribute
    {
        /// <summary>The username</summary>
        public readonly string Username;

        /// <summary>The password</summary>
        public readonly string Password;

        /// <summary>Constructs a new <see cref="AutoSubscriberUserCredentialAttribute"/>.</summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public AutoSubscriberUserCredentialAttribute(string username, string password)
        {
            if (username is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.username); }
            if (password is null) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.password); }
            Username = username;
            Password = password;
        }
    }
}
