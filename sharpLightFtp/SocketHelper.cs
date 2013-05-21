using System;
using System.Net.Sockets;

namespace sharpLightFtp
{
	internal static class SocketHelper
	{
		internal static InternalCommandResult WrapAsyncCall(Func<SocketAsyncEventArgs, bool> predicate,
		                                                    SocketAsyncEventArgs socketAsyncEventArgs)
		{
			var async = predicate.Invoke(socketAsyncEventArgs);
			if (async)
			{
				var userToken = socketAsyncEventArgs.UserToken;
				var socketAsyncEventArgsUserToken = (SocketAsyncEventArgsUserToken) userToken;
				var receivedSignalWithinTime = socketAsyncEventArgsUserToken.WaitForSignal();
				if (!receivedSignalWithinTime)
				{
					return InternalCommandResult.NoReceivedWithinTime;
				}
			}

			var exception = socketAsyncEventArgs.ConnectByNameError;
			if (exception != null)
			{
				return InternalCommandResult.ExceptionOccured;
			}

			// TODO maybe a check against socketAsyncEventArgs.SocketError == SocketError.Success would be more correct

			return InternalCommandResult.Success;
		}
	}
}
