using System;
using System.Net.Sockets;

namespace sharpLightFtp
{
	internal static class SocketHelper
	{
		internal static SocketAsyncEventArgs WrapAsyncCall(Func<SocketAsyncEventArgs, bool> predicate,
		                                                   SocketAsyncEventArgs socketAsyncEventArgs)
		{
			// TODO recheck the signal!
			var async = predicate.Invoke(socketAsyncEventArgs);
			if (async)
			{
				var userToken = socketAsyncEventArgs.UserToken;
				var socketAsyncEventArgsUserToken = (SocketAsyncEventArgsUserToken) userToken;
				socketAsyncEventArgsUserToken.WaitForSignal();
			}

			return socketAsyncEventArgs;
		}
	}
}
