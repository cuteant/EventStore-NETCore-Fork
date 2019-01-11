﻿namespace EventStore.ClientAPI.UserManagement
{
    /// <summary>
    /// Class holding the information needed to create a user.
    /// </summary>
    internal sealed class UserCreationInformation
    {
        /// <summary>
        /// The new users login name.
        /// </summary>
        public readonly string LoginName;

        /// <summary>
        /// The full name of the new user.
        /// </summary>
        public readonly string FullName;

        /// <summary>
        /// The groups the new user should become a member of.
        /// </summary>
        public readonly string[] Groups;

        /// <summary>
        /// The password of the new user.
        /// </summary>
        public readonly string Password;

        /// <summary>
        /// Enstantiates a new <see cref="UserCreationInformation"/> class.
        /// </summary>
        /// <param name="login"></param>
        /// <param name="fullName"></param>
        /// <param name="groups"></param>
        /// <param name="password"></param>
        public UserCreationInformation(string login, string fullName, string[] groups, string password)
        {
            if (string.IsNullOrEmpty(login)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.login); }
            if (string.IsNullOrEmpty(fullName)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.fullName); }
            if (null == groups) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.groups); }
            if (string.IsNullOrEmpty(password)) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.password); }
            LoginName = login;
            FullName = fullName;
            Groups = groups;
            Password = password;
        }
    }
}
