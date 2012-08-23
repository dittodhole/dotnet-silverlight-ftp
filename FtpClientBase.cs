using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace sharpLightFtp
{
	public abstract class FtpClientBase
	{
		public Encoding Encoding { get; set; }

		protected SocketAsyncEventArgs SendUsername(Socket socket, EndPoint endPoint, string username)
		{
			Contract.Requires(socket != null);
			Contract.Requires(endPoint != null);

			SocketAsyncEventArgs socketAsyncEventArgs;

			if (string.IsNullOrWhiteSpace(username))
			{
				socketAsyncEventArgs = GetSuccessSocketAsyncEventArgs(endPoint);
			}
			else
			{
				var complexCommand = new ComplexFtpCommand
				{
					Command = string.Format("USER {0}", username),
					Encoding = this.Encoding,
					EndPoint = endPoint,
					Socket = socket
				};
				socketAsyncEventArgs = SendCommand(complexCommand);
			}

			return socketAsyncEventArgs;
		}

		protected SocketAsyncEventArgs SendPassword(Socket socket, EndPoint endPoint, string password)
		{
			Contract.Requires(socket != null);
			Contract.Requires(endPoint != null);

			SocketAsyncEventArgs socketAsyncEventArgs;

			if (string.IsNullOrWhiteSpace(password))
			{
				socketAsyncEventArgs = GetSuccessSocketAsyncEventArgs(endPoint);
			}
			else
			{
				var complexFtpCommand = new ComplexFtpCommand
				{
					Command = string.Format("PASS {0}", password),
					Encoding = this.Encoding,
					EndPoint = endPoint,
					Socket = socket
				};
				socketAsyncEventArgs = SendCommand(complexFtpCommand);
			}

			return socketAsyncEventArgs;
		}

		private static SocketAsyncEventArgs SendCommand(ComplexFtpCommand complexFtpCommand)
		{
			Contract.Requires(complexFtpCommand != null);

			complexFtpCommand.Validate();

			var encoding = complexFtpCommand.Encoding;
			var command = complexFtpCommand.Command;
			var endPoint = complexFtpCommand.EndPoint;
			var socket = complexFtpCommand.Socket;

			var commandBuffer = encoding.GetBytes(command);

			var mutex = new AutoResetEvent(false);

			var socketAsyncEventArgs = GetSocketAsyncEventArgs(endPoint);
			socketAsyncEventArgs.SetBuffer(commandBuffer, 0, commandBuffer.Length);
			socketAsyncEventArgs.Completed += (sender, e) => mutex.Set();
			socket.SendAsync(socketAsyncEventArgs);

			mutex.WaitOne();

			return socketAsyncEventArgs;
		}

		private static SocketAsyncEventArgs GetSuccessSocketAsyncEventArgs(EndPoint endPoint)
		{
			Contract.Requires(endPoint != null);

			var socketAsyncEventArgs = GetSocketAsyncEventArgs(endPoint);
			socketAsyncEventArgs.SocketError = SocketError.Success;

			return socketAsyncEventArgs;
		}

		protected static SocketAsyncEventArgs GetSocketAsyncEventArgs(EndPoint endPoint)
		{
			Contract.Requires(endPoint != null);

			var socketAsyncEventArgs = new SocketAsyncEventArgs
			{
				RemoteEndPoint = endPoint,
				SocketClientAccessPolicyProtocol = SocketClientAccessPolicyProtocol.Http
			};

			return socketAsyncEventArgs;
		}
	}
}
