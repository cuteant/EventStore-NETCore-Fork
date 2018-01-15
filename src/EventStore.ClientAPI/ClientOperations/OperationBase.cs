﻿using System;
using System.Threading;
using System.Threading.Tasks;
using CuteAnt.Buffers;
using EventStore.ClientAPI.Common.Utils;
using EventStore.ClientAPI.Exceptions;
using EventStore.Core.Messages;
using EventStore.ClientAPI.SystemData;
using EventStore.ClientAPI.Transport.Tcp;
using Microsoft.Extensions.Logging;

namespace EventStore.ClientAPI.ClientOperations
{
  internal abstract class OperationBase<TResult, TResponse> : IClientOperation
      where TResponse : class
  {
    private readonly TcpCommand _requestCommand;
    private readonly TcpCommand _responseCommand;
    protected readonly UserCredentials UserCredentials;

    protected readonly ILogger Log;
    private readonly TaskCompletionSource<TResult> _source;
    private TResponse _response;
    private int _completed;

    protected abstract object CreateRequestDto();
    protected abstract InspectionResult InspectResponse(TResponse response);
    protected abstract TResult TransformResponse(TResponse response);

    protected OperationBase(TaskCompletionSource<TResult> source,
      TcpCommand requestCommand, TcpCommand responseCommand, UserCredentials userCredentials)
    {
      Ensure.NotNull(source, nameof(source));

      Log = TraceLogger.GetLogger(this.GetType());
      _source = source;
      _requestCommand = requestCommand;
      _responseCommand = responseCommand;
      UserCredentials = userCredentials;
    }

    public TcpPackage CreateNetworkPackage(Guid correlationId)
    {
      return new TcpPackage(_requestCommand,
                            UserCredentials != null ? TcpFlags.Authenticated : TcpFlags.None,
                            correlationId,
                            UserCredentials?.Username,
                            UserCredentials?.Password,
                            CreateRequestDto().Serialize());
    }

    public virtual InspectionResult InspectPackage(TcpPackage package)
    {
      try
      {
        if (package.Command == _responseCommand)
        {
          _response = package.Data.Deserialize<TResponse>();
          return InspectResponse(_response);
        }
        switch (package.Command)
        {
          case TcpCommand.NotAuthenticated: return InspectNotAuthenticated(package);
          case TcpCommand.BadRequest: return InspectBadRequest(package);
          case TcpCommand.NotHandled: return InspectNotHandled(package);
          default: return InspectUnexpectedCommand(package, _responseCommand);
        }
      }
      catch (Exception e)
      {
        Fail(e);
        return new InspectionResult(InspectionDecision.EndOperation, $"Exception - {e.Message}");
      }
    }

    protected void Succeed()
    {
      if (Interlocked.CompareExchange(ref _completed, 1, 0) == 0)
      {
        if (_response != null)
          _source.SetResult(TransformResponse(_response));
        else
          _source.SetException(new NoResultException());
      }
    }

    public void Fail(Exception exception)
    {
      if (Interlocked.CompareExchange(ref _completed, 1, 0) == 0)
      {
        _source.SetException(exception);
      }
    }

    public InspectionResult InspectNotAuthenticated(TcpPackage package)
    {
      string message = Helper.EatException(() => Helper.UTF8NoBom.GetString(package.Data.Array, package.Data.Offset, package.Data.Count));
      Fail(new NotAuthenticatedException(string.IsNullOrEmpty(message) ? "Authentication error" : message));
      return new InspectionResult(InspectionDecision.EndOperation, "NotAuthenticated");
    }

    public InspectionResult InspectBadRequest(TcpPackage package)
    {
      string message = Helper.EatException(() => Helper.UTF8NoBom.GetString(package.Data.Array, package.Data.Offset, package.Data.Count));
      Fail(new ServerErrorException(string.IsNullOrEmpty(message) ? "<no message>" : message));
      return new InspectionResult(InspectionDecision.EndOperation, $"BadRequest - {message}");
    }

    public InspectionResult InspectNotHandled(TcpPackage package)
    {
      var message = package.Data.Deserialize<TcpClientMessageDto.NotHandled>();
      switch (message.Reason)
      {
        case TcpClientMessageDto.NotHandled.NotHandledReason.NotReady:
          return new InspectionResult(InspectionDecision.Retry, "NotHandled - NotReady");

        case TcpClientMessageDto.NotHandled.NotHandledReason.TooBusy:
          return new InspectionResult(InspectionDecision.Retry, "NotHandled - TooBusy");

        case TcpClientMessageDto.NotHandled.NotHandledReason.NotMaster:
          var masterInfo = message.AdditionalInfo.Deserialize<TcpClientMessageDto.NotHandled.MasterInfo>();
          return new InspectionResult(InspectionDecision.Reconnect, "NotHandled - NotMaster",
                                      masterInfo.ExternalTcpEndPoint, masterInfo.ExternalSecureTcpEndPoint);

        default:
          Log.LogError("Unknown NotHandledReason: {0}.", message.Reason);
          return new InspectionResult(InspectionDecision.Retry, "NotHandled - <unknown>");
      }
    }

    public InspectionResult InspectUnexpectedCommand(TcpPackage package, TcpCommand expectedCommand)
    {
      if (package.Command == expectedCommand)
        throw new ArgumentException($"Command should not be {package.Command}.");

      Log.LogError("Unexpected TcpCommand received.");
      Log.LogError("Expected: {0}, Actual: {1}, Flags: {2}, CorrelationId: {3}", expectedCommand, package.Command, package.Flags, package.CorrelationId);
      Log.LogError("Operation ({0}): {1}", GetType().Name, this);
      Log.LogError("TcpPackage Data Dump:");
      Log.LogError(Helper.FormatBinaryDump(package.Data));

      Fail(new CommandNotExpectedException(expectedCommand.ToString(), package.Command.ToString()));
      return new InspectionResult(InspectionDecision.EndOperation, $"Unexpected command - {package.Command.ToString()}");
    }
  }
}
