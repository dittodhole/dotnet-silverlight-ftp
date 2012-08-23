using System;
using System.Diagnostics.Contracts;
using System.Net.Sockets;
using System.Text;

namespace sharpLightFtp.Extensions
{
	internal static class SocketAsyncEventArgsExtensions
	{
		internal static bool IsSuccess(this SocketAsyncEventArgs socketAsyncEventArgs)
		{
			Contract.Requires(socketAsyncEventArgs != null);

			return socketAsyncEventArgs.SocketError == SocketError.Success;
		}

		internal static Exception GetException(this SocketAsyncEventArgs socketAsyncEventArgs)
		{
			Contract.Requires(socketAsyncEventArgs != null);

			return socketAsyncEventArgs.ConnectByNameError;
		}

		internal static ComplexResult GetComplexResult(this SocketAsyncEventArgs socketAsyncEventArgs, Encoding encoding)
		{
			Contract.Requires(socketAsyncEventArgs != null);
			Contract.Requires(encoding != null);

			var buffer = socketAsyncEventArgs.Buffer;
			var offset = socketAsyncEventArgs.Offset;
			var bytesTransferred = socketAsyncEventArgs.BytesTransferred;
			var data = encoding.GetString(buffer, offset, bytesTransferred);
			var complexResult = new ComplexResult(data);

			return complexResult;
		}
	}
}
