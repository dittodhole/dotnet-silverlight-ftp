using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;

namespace sharpLightFtp.Extensions
{
	internal static class EndPointExtensions
	{
		internal static SocketAsyncEventArgs GetSuccessSocketAsyncEventArgs(this EndPoint endPoint)
		{
			Contract.Requires(endPoint != null);

			var socketAsyncEventArgs = endPoint.GetSocketAsyncEventArgs();
			socketAsyncEventArgs.SocketError = SocketError.Success;

			return socketAsyncEventArgs;
		}

		internal static SocketAsyncEventArgs GetSocketAsyncEventArgs(this EndPoint endPoint)
		{
			Contract.Requires(endPoint != null);

			var socketAsyncEventArgs = new SocketAsyncEventArgs
			{
				RemoteEndPoint = endPoint,
				SocketClientAccessPolicyProtocol = SocketClientAccessPolicyProtocol.Http
			};

			return socketAsyncEventArgs;
		}
	}
}
