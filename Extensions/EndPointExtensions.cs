using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;
using sharpLightFtp.EventArgs;

namespace sharpLightFtp.Extensions
{
	internal static class EndPointExtensions
	{
		internal static SocketEventArgs GetSocketEventArgs(this EndPoint endPoint)
		{
			Contract.Requires(endPoint != null);

			var socketEventArgs = new SocketEventArgs
			{
				RemoteEndPoint = endPoint,
				SocketClientAccessPolicyProtocol = SocketClientAccessPolicyProtocol.Http
			};
			socketEventArgs.Completed += (sender, socketAsyncEventArgs) =>
			{
				var syncEventArgs = (SocketEventArgs) socketAsyncEventArgs;
				var autoResetEvent = syncEventArgs.AutoResetEvent;
				autoResetEvent.Set();
			};

			return socketEventArgs;
		}
	}
}
