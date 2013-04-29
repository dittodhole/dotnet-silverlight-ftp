using System.Diagnostics.Contracts;
using System.Net.Sockets;
using System.Text;

namespace sharpLightFtp.Extensions
{
	public static class SocketAsyncEventArgsExtensions
	{
		public static string GetData(this SocketAsyncEventArgs socketAsyncEventArgs,
		                             Encoding encoding)
		{
			Contract.Requires(encoding != null);

			var buffer = socketAsyncEventArgs.Buffer;
			var offset = socketAsyncEventArgs.Offset;
			var bytesTransferred = socketAsyncEventArgs.BytesTransferred;
			var data = encoding.GetString(buffer,
			                              offset,
			                              bytesTransferred);

			return data;
		}
	}
}
