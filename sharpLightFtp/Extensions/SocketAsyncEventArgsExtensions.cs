using System;
using System.Diagnostics.Contracts;
using System.Net.Sockets;
using System.Text;
using sharpLightFtp.EventArgs;

namespace sharpLightFtp.Extensions
{
	internal static class SocketAsyncEventArgsExtensions
	{
		internal static bool IsSuccess(this SocketEventArgs socketEventArgs)
		{
			Contract.Requires(socketEventArgs != null);

			var success = socketEventArgs.SocketError == SocketError.Success;

			return success;
		}

		internal static Exception GetException(this SocketEventArgs socketEventArgs)
		{
			Contract.Requires(socketEventArgs != null);

			var exception = socketEventArgs.ConnectByNameError;

			return exception;
		}

		internal static string GetData(this SocketEventArgs socketEventArgs, Encoding encoding)
		{
			Contract.Requires(socketEventArgs != null);
			Contract.Requires(encoding != null);

			var buffer = socketEventArgs.Buffer;
			var offset = socketEventArgs.Offset;
			var bytesTransferred = socketEventArgs.BytesTransferred;
			var data = encoding.GetString(buffer, offset, bytesTransferred);

			return data;
		}
	}
}
