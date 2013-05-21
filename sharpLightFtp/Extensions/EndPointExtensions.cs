using System;
using System.Net;
using System.Net.Sockets;

namespace sharpLightFtp.Extensions
{
	internal static class EndPointExtensions
	{
		internal static SocketAsyncEventArgs GetSocketAsyncEventArgs(this EndPoint endPoint,
		                                                             SocketClientAccessPolicyProtocol socketClientAccessPolicyProtocol,
		                                                             TimeSpan timeout)
		{
			var asyncEventArgsUserToken = new SocketAsyncEventArgsUserToken(timeout);
			var socketAsyncEventArgs = new SocketAsyncEventArgs
			{
				RemoteEndPoint = endPoint,
				SocketClientAccessPolicyProtocol = socketClientAccessPolicyProtocol,
				UserToken = asyncEventArgsUserToken
			};
			socketAsyncEventArgs.Completed += (sender,
			                                   args) =>
			{
				var userToken = args.UserToken;
				var socketAsyncEventArgsUserToken = (SocketAsyncEventArgsUserToken) userToken;
				socketAsyncEventArgsUserToken.Signal();
			};

			return socketAsyncEventArgs;
		}
	}
}
