using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace sharpLightFtp.Extensions
{
	internal static class SocketExtensions
	{
		internal static FtpReply Receive(this Socket socket,
		                                 int bufferSize,
		                                 Func<SocketAsyncEventArgs> socketAsyncEventArgsPredicate,
		                                 Encoding encoding)
		{
			string data;
			if (!socket.ReceiveIntoString(bufferSize,
			                              socketAsyncEventArgsPredicate,
			                              encoding,
			                              out data))
			{
				return FtpReply.FailedFtpReply;
			}

			var ftpReply = FtpClientHelper.ParseFtpReply(data);

			return ftpReply;
		}

		internal static bool ReceiveIntoString(this Socket socket,
		                                       int bufferSize,
		                                       Func<SocketAsyncEventArgs> socketAsyncEventArgsPredicate,
		                                       Encoding encoding,
		                                       out string data)
		{
			byte[] bytes;
			using (var memoryStream = new MemoryStream())
			{
				var success = socket.ReceiveIntoStream(bufferSize,
				                                       socketAsyncEventArgsPredicate,
				                                       memoryStream);
				if (!success)
				{
					data = null;
					return false;
				}

				bytes = memoryStream.ToArray();
			}

			data = encoding.GetString(bytes,
			                          0,
			                          bytes.Length);
			return true;
		}

		internal static bool ReceiveIntoStream(this Socket socket,
		                                       int bufferSize,
		                                       Func<SocketAsyncEventArgs> socketAsyncEventArgsPredicate,
		                                       Stream stream,
		                                       Action<long> progressPredicate = null)
		{
			var bytesReceived = 0L;
			int bytesTransferred;

			do
			{
				byte[] buffer;
				int offset;
				// TODO maybe reuse the socketAsyncEventArgs, but not 100% clean ...
				using (var socketAsyncEventArgs = socketAsyncEventArgsPredicate.Invoke())
				{
					buffer = new byte[bufferSize];
					socketAsyncEventArgs.SetBuffer(buffer,
					                               0,
					                               bufferSize);

					SocketHelper.WrapAsyncCall(socket.ReceiveAsync,
					                           socketAsyncEventArgs);
					var success = socketAsyncEventArgs.GetSuccess();
					if (!success)
					{
						return false;
					}

					buffer = socketAsyncEventArgs.Buffer;
					bytesTransferred = socketAsyncEventArgs.BytesTransferred;
					offset = socketAsyncEventArgs.Offset;
				}

				stream.Write(buffer,
				             offset,
				             bytesTransferred);

				if (progressPredicate != null)
				{
					bytesReceived += bytesTransferred;
					progressPredicate.Invoke(bytesReceived);
				}
			} while (bytesTransferred == bufferSize); // TODO I know that there *might* be a chance that this is not valid ...

			return true;
		}

		internal static bool Send(this Socket socket,
		                          int bufferSize,
		                          Func<SocketAsyncEventArgs> socketAsyncEventArgsPredicate,
		                          Stream stream,
		                          Action<long, long> progressPredicate = null)
		{
			var bytesTotal = stream.Length;
			var bytesSent = 0L;

			int bytesRead;
			do
			{
				using (var socketAsyncEventArgs = socketAsyncEventArgsPredicate.Invoke())
				{
					var buffer = new byte[bufferSize];
					bytesRead = stream.Read(buffer,
					                        0,
					                        buffer.Length);
					socketAsyncEventArgs.SetBuffer(buffer,
					                               0,
					                               bytesRead);
					SocketHelper.WrapAsyncCall(socket.SendAsync,
					                           socketAsyncEventArgs);
					var success = socketAsyncEventArgs.GetSuccess();
					if (!success)
					{
						return false;
					}
					if (progressPredicate != null)
					{
						bytesSent += bytesRead;
						progressPredicate.Invoke(bytesSent,
						                         bytesTotal);
					}
				}
			} while (bytesRead == bufferSize); // TODO I know that there *might* be a chance that this is not valid ...

			return true;
		}
	}
}
