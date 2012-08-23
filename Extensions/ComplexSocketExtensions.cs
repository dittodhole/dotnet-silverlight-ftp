using System.Diagnostics.Contracts;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace sharpLightFtp.Extensions
{
	internal static class ComplexSocketExtensions
	{
		internal static SocketAsyncEventArgs Connect(this ComplexSocket complexSocket, Encoding encoding)
		{
			Contract.Requires(complexSocket != null);

			var socket = complexSocket.Socket;
			var endPoint = complexSocket.EndPoint;

			var mutex = new AutoResetEvent(false);

			var asyncEventArgs = endPoint.GetSocketAsyncEventArgs();
			asyncEventArgs.Completed += (sender, args) => mutex.Set();
			socket.ConnectAsync(asyncEventArgs);

			mutex.WaitOne();

			asyncEventArgs = complexSocket.Receive();

			var complexResult = asyncEventArgs.GetComplexResult(encoding);

			return asyncEventArgs;
		}

		internal static SocketAsyncEventArgs Authenticate(this ComplexSocket complexSocket, string username, string password, Encoding encoding)
		{
			Contract.Requires(complexSocket != null);
			Contract.Requires(encoding != null);

			var endPoint = complexSocket.EndPoint;

			if (string.IsNullOrWhiteSpace(username))
			{
				return endPoint.GetSuccessSocketAsyncEventArgs();
			}

			var complexFtpCommand = complexSocket.GetAuthenticateCommand(username, password, encoding);
			var socketAsyncEventArgs = complexFtpCommand.SendCommand();

			return socketAsyncEventArgs;
		}

		internal static ComplexFtpCommand GetAuthenticateCommand(this ComplexSocket complexSocket, string username, string password, Encoding encoding)
		{
			Contract.Requires(complexSocket != null);
			Contract.Requires(!string.IsNullOrWhiteSpace(username));
			Contract.Requires(encoding != null);

			var complexFtpCommand = new ComplexFtpCommand(complexSocket, encoding);

			var userCommand = string.Format("USER {0}", username);

			if (string.IsNullOrWhiteSpace(password))
			{
				complexFtpCommand.Command = userCommand;
			}
			else
			{
				complexFtpCommand.Commands = new[]
				{
					userCommand, string.Format("PASS {0}", password)
				};
			}

			return complexFtpCommand;
		}

		internal static SocketAsyncEventArgs SendFeatures(this ComplexSocket complexSocket, Encoding encoding)
		{
			Contract.Requires(complexSocket != null);

			var complexFtpCommand = new ComplexFtpCommand(complexSocket, encoding)
			{
				Command = "FEAT"
			};
			var socketAsyncEventArgs = complexFtpCommand.SendCommand();

			return socketAsyncEventArgs;
		}

		internal static SocketAsyncEventArgs Receive(this ComplexSocket complexSocket)
		{
			Contract.Requires(complexSocket != null);

			var socket = complexSocket.Socket;
			var endPoint = complexSocket.EndPoint;

			var mutex = new AutoResetEvent(false);

			var responseBuffer = new byte[1024];
			var socketAsyncEventArgs = endPoint.GetSocketAsyncEventArgs();
			socketAsyncEventArgs.SetBuffer(responseBuffer, 0, responseBuffer.Length);
			socketAsyncEventArgs.Completed += (sender, receiveSocketAsyncEventArgs) => mutex.Set();

			socket.ReceiveAsync(socketAsyncEventArgs);

			mutex.WaitOne();

			return socketAsyncEventArgs;
		}

	}
}
