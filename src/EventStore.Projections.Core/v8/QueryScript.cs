﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using EventStore.Common.Utils;
using EventStore.Projections.Core.Messages;
using Microsoft.Extensions.Logging;

namespace EventStore.Projections.Core.v8
{
    public class QueryScript : IDisposable
    {
        private readonly ILogger Log = TraceLogger.GetLogger<QueryScript>();
        private readonly PreludeScript _prelude;
        private readonly CompiledScript _script;
        private readonly Dictionary<string, IntPtr> _registeredHandlers = new Dictionary<string, IntPtr>(StringComparer.Ordinal);

        private Func<string, string[], string> _getStatePartition;
        private Func<string, string[], string> _transformCatalogEvent;
        private Func<string, string[], Tuple<string, string>> _processEvent;
        private Func<string, string[], string> _processDeletedNotification;
        private Func<string, string[], string> _processCreatedNotification;
        private Func<string> _transformStateToResult;
        private Action<string> _setState;
        private Action<string> _setSharedState;
        private Action _initialize;
        private Action _initialize_shared;
        private Func<string> _getSources;

        // the following two delegates must be kept alive while used by unmanaged code
        private readonly Js1.CommandHandlerRegisteredDelegate _commandHandlerRegisteredCallback; // do not inline
        private readonly Js1.ReverseCommandHandlerDelegate _reverseCommandHandlerDelegate; // do not inline
        private QuerySourcesDefinition _sources;
        private Exception _reverseCommandHandlerException;

        public event Action<string> Emit;

        public QueryScript(PreludeScript prelude, string script, string fileName)
        {
            _prelude = prelude;
            _commandHandlerRegisteredCallback = CommandHandlerRegisteredCallback;
            _reverseCommandHandlerDelegate = ReverseCommandHandler;

            _script = CompileScript(prelude, script, fileName);

            try
            {
                GetSources();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private CompiledScript CompileScript(PreludeScript prelude, string script, string fileName)
        {
            prelude.ScheduleTerminateExecution();
            IntPtr query = Js1.CompileQuery(
                prelude.GetHandle(), script, fileName, _commandHandlerRegisteredCallback, _reverseCommandHandlerDelegate);
            var terminated = prelude.CancelTerminateExecution();
            CompiledScript.CheckResult(query, terminated, disposeScriptOnException: true);
            return new CompiledScript(query);
        }

        private void ReverseCommandHandler(string commandName, string commandBody)
        {
            try
            {
                switch (commandName)
                {
                    case "emit":
                        DoEmit(commandBody);
                        break;
                    default:
                        if (Log.IsDebugLevelEnabled()) Log.IgnoringUnknownReverseCommand(commandName);
                        break;
                }
            }
            catch (Exception ex)
            {
                // report only the first exception occured in reverse command handler
                if (_reverseCommandHandlerException is null)
                    _reverseCommandHandlerException = ex;
            }
        }

        private void CommandHandlerRegisteredCallback(string commandName, IntPtr handlerHandle)
        {
            _registeredHandlers.Add(commandName, handlerHandle);
            //TODO: change to dictionary
            switch (commandName)
            {
                case "initialize":
                    _initialize = () => ExecuteHandler(handlerHandle, "");
                    break;
                case "initialize_shared":
                    _initialize_shared = () => ExecuteHandler(handlerHandle, "");
                    break;
                case "get_state_partition":
                    _getStatePartition = (json, other) => ExecuteHandler(handlerHandle, json, other);
                    break;
                case "process_event":
                    string newSharedState;
                    _processEvent =
                        (json, other) =>
                            Tuple.Create(ExecuteHandler(handlerHandle, json, other, out newSharedState), newSharedState);
                    break;
                case "process_deleted_notification":
                    _processDeletedNotification = (json, other) => ExecuteHandler(handlerHandle, json, other);
                    break;
                case "process_created_notification":
                    _processCreatedNotification = (json, other) => ExecuteHandler(handlerHandle, json, other);
                    break;
                case "transform_catalog_event":
                    _transformCatalogEvent = (json, other) => ExecuteHandler(handlerHandle, json, other);
                    break;
                case "transform_state_to_result":
                    _transformStateToResult = () => ExecuteHandler(handlerHandle, "");
                    break;
                case "test_array":
                    break;
                case "set_state":
                    _setState = json => ExecuteHandler(handlerHandle, json);
                    break;
                case "set_shared_state":
                    _setSharedState = json => ExecuteHandler(handlerHandle, json);
                    break;
                case "get_sources":
                    _getSources = () => ExecuteHandler(handlerHandle, "");
                    break;
                case "set_debugging":
                case "debugging_get_state":
                    // ignore - browser based debugging only
                    break;
                default:
                    if (Log.IsDebugLevelEnabled())
                    {
                        Log.UnknownCommandHandlerRegistered(commandName);
                    }

                    break;
            }
        }

        private void DoEmit(string commandBody)
        {
            OnEmit(commandBody);
        }

        private void GetSources()
        {
            if (_getSources is null)
            {
                throw new InvalidOperationException("'get_sources' command handler has not been registered");
            }

            var sourcesJson = _getSources();


            _sources = sourcesJson.ParseJson<QuerySourcesDefinition>();
        }

        private string ExecuteHandler(IntPtr commandHandlerHandle, string json, string[] other = null)
        {
            return ExecuteHandler(commandHandlerHandle, json, other, out string newSharedState);
        }

        private string ExecuteHandler(
            IntPtr commandHandlerHandle, string json, string[] other, out string newSharedState)
        {
            _reverseCommandHandlerException = null;

            _prelude.ScheduleTerminateExecution();

            var success = Js1.ExecuteCommandHandler(
                _script.GetHandle(), commandHandlerHandle, json, other, other is object ? other.Length : 0,
                out IntPtr resultJsonPtr, out IntPtr result2JsonPtr, out IntPtr memoryHandle);

            var terminated = _prelude.CancelTerminateExecution();
            if (!success)
            {
                CompiledScript.CheckResult(_script.GetHandle(), terminated, disposeScriptOnException: false);
            }

            string resultJson = Marshal.PtrToStringUni(resultJsonPtr);
            string result2Json = Marshal.PtrToStringUni(result2JsonPtr);
            Js1.FreeResult(memoryHandle);
            if (_reverseCommandHandlerException is object)
            {
                throw new ApplicationException(
                    "An exception occurred while executing a reverse command handler. "
                    + _reverseCommandHandlerException.Message, _reverseCommandHandlerException);
            }
            newSharedState = result2Json;
            return resultJson;
        }

        private void OnEmit(string obj)
        {
            var handler = Emit;
            handler?.Invoke(obj);
        }

        public void Dispose()
        {
            _script.Dispose();
        }

        public void Initialize()
        {
            InitializeScript();
        }

        public void InitializeShared()
        {
            InitializeScriptShared();
        }

        private void InitializeScript()
        {
            _initialize?.Invoke();
        }

        private void InitializeScriptShared()
        {
            _initialize_shared?.Invoke();
        }

        public string GetPartition(string json, string[] other)
        {
            if (_getStatePartition is null)
                throw new InvalidOperationException("'get_state_partition' command handler has not been registered");

            return _getStatePartition(json, other);
        }

        public string TransformCatalogEvent(string json, string[] other)
        {
            if (_transformCatalogEvent is null)
                throw new InvalidOperationException("'transform_catalog_event' command handler has not been registered");

            return _transformCatalogEvent(json, other);
        }

        public Tuple<string, string> Push(string json, string[] other)
        {
            if (_processEvent is null)
                throw new InvalidOperationException("'process_event' command handler has not been registered");

            return _processEvent(json, other);
        }

        public string NotifyDeleted(string json, string[] other)
        {
            if (_processDeletedNotification is null)
                throw new InvalidOperationException("'process_deleted_notification' command handler has not been registered");

            return _processDeletedNotification(json, other);
        }

        public string NotifyCreated(string json, string[] other)
        {
            if (_processCreatedNotification is null)
                throw new InvalidOperationException("'process_created_notification' command handler has not been registered");

            return _processCreatedNotification(json, other);
        }

        public string TransformStateToResult()
        {
            if (_transformStateToResult is null)
                throw new InvalidOperationException("'transform_state_to_result' command handler has not been registered");

            return _transformStateToResult();
        }

        public void SetState(string state)
        {
            if (_setState is null)
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.Set_StateCommandHandlerHasNotBeenRegistered);
            _setState(state);
        }

        public void SetSharedState(string state)
        {
            if (_setSharedState is null)
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.Set_Shared_StateCommandHandlerHasNotBeenRegistered);
            _setSharedState(state);
        }

        public QuerySourcesDefinition GetSourcesDefintion()
        {
            return _sources;
        }

    }
}
