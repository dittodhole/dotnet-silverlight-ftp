using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using sharpLightFtp.EventArgs;
using sharpLightFtp.Extensions;

namespace sharpLightFtp
{
	public sealed class ComplexSocket : IDisposable
	{
		private readonly EndPoint _endPoint;
		private readonly FtpClient _ftpClient;
		private readonly bool _isControlSocket;

		private readonly Socket _socket = new Socket(AddressFamily.InterNetwork,
		                                             SocketType.Stream,
		                                             ProtocolType.Tcp);

		internal ComplexSocket(FtpClient ftpClient,
		                       EndPoint endPoint,
		                       bool isControlSocket)
		{
			this._ftpClient = ftpClient;
			this._endPoint = endPoint;
			this._isControlSocket = isControlSocket;
		}

		internal EndPoint EndPoint
		{
			get
			{
				return this._endPoint;
			}
		}

		internal Socket Socket
		{
			get
			{
				return this._socket;
			}
		}

		public bool IsControlSocket
		{
			get
			{
				return this._isControlSocket;
			}
		}

		public bool Connected
		{
			get
			{
				return this._socket.Connected;
			}
		}

		public FtpClient FtpClient
		{
			get
			{
				return this._ftpClient;
			}
		}

		#region IDisposable Members

		public void Dispose()
		{
			this._socket.Dispose();
		}

		#endregion

		private void RaiseFtpCommandFailedAsync(SocketAsyncEventArgs socketAsyncEventArgs,
		                                        InternalCommandResult internalCommandResult)
		{
			switch (internalCommandResult)
			{
				case InternalCommandResult.ExceptionOccured:
					var ftpCommandFailedEventArgs = new FtpCommandFailedEventArgs(socketAsyncEventArgs);
					this.FtpClient.RaiseFtpCommandFailedAsync(ftpCommandFailedEventArgs);
					break;
				case InternalCommandResult.NoReceivedWithinTime:
					var ftpCommandTimedOutEventArgs = new FtpCommandTimedOutEventArgs(socketAsyncEventArgs);
					this.FtpClient.RaiseFtpCommandFailedAsync(ftpCommandTimedOutEventArgs);
					break;
			}
		}

		internal bool Connect(TimeSpan timeout)
		{
			using (var socketAsyncEventArgs = this.GetSocketAsyncEventArgs(timeout))
			{
				var internalCommandResult = SocketHelper.WrapAsyncCall(this._socket.ConnectAsync,
				                                                       socketAsyncEventArgs);
				if (internalCommandResult != InternalCommandResult.Success)
				{
					this.RaiseFtpCommandFailedAsync(socketAsyncEventArgs,
					                                internalCommandResult);
					return false;
				}
			}

			return true;
		}

		internal bool Send(Stream stream,
		                   TimeSpan timeout)
		{
			var buffer = this._socket.GetSendBuffer();
			int read;
			while ((read = stream.Read(buffer,
			                           0,
			                           buffer.Length)) > 0)
			{
				using (var socketAsyncEventArgs = this.GetSocketAsyncEventArgs(timeout))
				{
					socketAsyncEventArgs.SetBuffer(buffer,
					                               0,
					                               read);
					var internalCommandResult = SocketHelper.WrapAsyncCall(this._socket.SendAsync,
					                                                       socketAsyncEventArgs);
					if (internalCommandResult != InternalCommandResult.Success)
					{
						this.RaiseFtpCommandFailedAsync(socketAsyncEventArgs,
						                                internalCommandResult);
						return false;
					}
				}
			}

			return true;
		}

		internal SocketAsyncEventArgs GetSocketAsyncEventArgs(TimeSpan timeout)
		{
			var endPoint = this.EndPoint;
			var ftpClient = this.FtpClient;
			var socketClientAccessPolicyProtocol = ftpClient.SocketClientAccessPolicyProtocol;
			var socketAsyncEventArgs = endPoint.GetSocketAsyncEventArgs(socketClientAccessPolicyProtocol,
			                                                            timeout);

			return socketAsyncEventArgs;
		}

		internal FtpReply GetFinalFtpReply(Encoding encoding,
		                                   TimeSpan timeout)
		{
			FtpReply ftpReply;
			do
			{
				using (var socketAsyncEventArgs = this.GetSocketAsyncEventArgs(timeout))
				{
					ftpReply = this.Socket.Receive(socketAsyncEventArgs,
					                               encoding);
				}
			} while (ftpReply.FtpResponseType == FtpResponseType.None);

			return ftpReply;
		}
	}
}
