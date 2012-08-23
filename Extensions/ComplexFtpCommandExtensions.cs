using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace sharpLightFtp.Extensions
{
	internal static class ComplexFtpCommandExtensions
	{
		internal static SocketAsyncEventArgs SendCommand(this ComplexFtpCommand complexFtpCommand)
		{
			Contract.Requires(complexFtpCommand != null);
			Contract.Requires(complexFtpCommand.Commands.Any());

			var encoding = complexFtpCommand.Encoding;
			var commands = complexFtpCommand.Commands;
			var complexSocket = complexFtpCommand.ComplexSocket;
			var endPoint = complexSocket.EndPoint;
			var socket = complexSocket.Socket;

			foreach (var command in commands)
			{
				var mutex = new AutoResetEvent(false);

				var sendBuffer = encoding.GetBytes(command);
				var sendAsyncEventArgs = endPoint.GetSocketAsyncEventArgs();
				sendAsyncEventArgs.SetBuffer(sendBuffer, 0, sendBuffer.Length);
				sendAsyncEventArgs.Completed += (sender, sendSocketAsyncEventArgs) => mutex.Set();

				socket.SendAsync(sendAsyncEventArgs);

				mutex.WaitOne();
			}

			var receiveSocketAsyncEventArgs = complexSocket.Receive();

			var complexResult = receiveSocketAsyncEventArgs.GetComplexResult(encoding);

			return receiveSocketAsyncEventArgs;
		}
	}
}
