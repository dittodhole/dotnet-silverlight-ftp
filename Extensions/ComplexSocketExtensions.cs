using System.Diagnostics.Contracts;
using System.Text;
using sharpLightFtp.EventArgs;

namespace sharpLightFtp.Extensions
{
	internal static class ComplexSocketExtensions
	{
		internal static ComplexResult Connect(this ComplexSocket complexSocket, Encoding encoding)
		{
			Contract.Requires(complexSocket != null);

			var socket = complexSocket.Socket;
			var endPoint = complexSocket.EndPoint;

			var socketEventArgs = endPoint.GetSocketEventArgs();
			var async = socket.ConnectAsync(socketEventArgs);
			if (async)
			{
				socketEventArgs.AutoResetEvent.WaitOne();
			}

			socketEventArgs = complexSocket.Receive(socketEventArgs);

			var complexResult = socketEventArgs.GetComplexResult(encoding);
			complexResult.SocketAsyncEventArgs = socketEventArgs;

			return complexResult;
		}

		internal static ComplexResult Authenticate(this ComplexSocket complexSocket, string username, string password, Encoding encoding)
		{
			Contract.Requires(complexSocket != null);
			Contract.Requires(encoding != null);

			var complexFtpCommand = new ComplexFtpCommand(complexSocket, encoding)
			{
				Command = string.Format("USER {0}", username)
			};
			var complexResult = complexFtpCommand.SendCommand();
			if (!complexResult.Success)
			{
				throw new FtpException(complexResult.ResponseMessage);
			}
			if (complexResult.FtpResponseType == FtpResponseType.PositiveIntermediate)
			{
				complexFtpCommand = new ComplexFtpCommand(complexSocket, encoding)
				{
					Command = string.Format("PASS {0}", password)
				};
				complexResult = complexFtpCommand.SendCommand();
				if (!complexResult.Success)
				{
					throw new FtpException(complexResult.ResponseMessage);
				}
			}

			return complexResult;
		}

		internal static ComplexResult SendFeatures(this ComplexSocket complexSocket, Encoding encoding)
		{
			Contract.Requires(complexSocket != null);

			var complexFtpCommand = new ComplexFtpCommand(complexSocket, encoding)
			{
				Command = "FEAT"
			};
			var complexResult = complexFtpCommand.SendCommand();

			return complexResult;
		}

		internal static SocketEventArgs Receive(this ComplexSocket complexSocket, SocketEventArgs sendSocketEventArgs)
		{
			Contract.Requires(complexSocket != null);

			var socket = complexSocket.Socket;
			var endPoint = complexSocket.EndPoint;

			var receiveSocketEventArgs = endPoint.GetSocketEventArgs();
			{
				var responseBuffer = new byte[1024];
				receiveSocketEventArgs.SetBuffer(responseBuffer, 0, responseBuffer.Length);
			}

			var async = socket.ReceiveAsync(receiveSocketEventArgs);
			if (async)
			{
				receiveSocketEventArgs.AutoResetEvent.WaitOne();
			}

			return receiveSocketEventArgs;
		}

	}
}
