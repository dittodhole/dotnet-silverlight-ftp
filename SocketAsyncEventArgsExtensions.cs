using System;
using System.Net.Sockets;

namespace sharpLightFtp
{
	internal static class SocketAsyncEventArgsExtensions
	{
		internal static bool IsSuccess(this SocketAsyncEventArgs socketAsyncEventArgs)
		{
			return socketAsyncEventArgs.SocketError == SocketError.Success;
		}

		internal static Exception GetException(this SocketAsyncEventArgs socketAsyncEventArgs)
		{
			return socketAsyncEventArgs.ConnectByNameError;
		}
	}
}
