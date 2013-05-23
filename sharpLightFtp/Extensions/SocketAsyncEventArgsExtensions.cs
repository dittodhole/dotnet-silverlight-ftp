using System.Net.Sockets;

namespace sharpLightFtp.Extensions
{
	public static class SocketAsyncEventArgsExtensions
	{
		public static bool GetSuccess(this SocketAsyncEventArgs socketAsyncEventArgs)
		{
			// TODO check against .ConnectByNameError
			// TODO check against .SocketError
			// TODO check against ((SocketAsyncEventArgsUserToken) .UserToken).TimeoutExpired

			return socketAsyncEventArgs.SocketError == SocketError.Success;
		}
	}
}
