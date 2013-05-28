using System;
using System.Net;
using System.Net.Sockets;
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

			this._socket.ReceiveBufferSize = ftpClient.SocketReceiveBufferSize;
			this._socket.SendBufferSize = ftpClient.SocketSendBufferSize;
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

		internal bool Connect(TimeSpan timeout)
		{
			using (var socketAsyncEventArgs = this.GetSocketAsyncEventArgsWithUserToken(timeout))
			{
				SocketHelper.WrapAsyncCall(this._socket.ConnectAsync,
				                           socketAsyncEventArgs);
				var success = socketAsyncEventArgs.GetSuccess();

				return success;
			}
		}

		internal SocketAsyncEventArgs GetSocketAsyncEventArgs()
		{
			var socketAsyncEventArgs = new SocketAsyncEventArgs
			{
				RemoteEndPoint = this.EndPoint,
				SocketClientAccessPolicyProtocol = this.FtpClient.SocketClientAccessPolicyProtocol
			};

			return socketAsyncEventArgs;
		}

		internal SocketAsyncEventArgs GetSocketAsyncEventArgsWithUserToken(TimeSpan timeout)
		{
			var socketAsyncEventArgs = this.GetSocketAsyncEventArgs();
			socketAsyncEventArgs.UserToken = new SocketAsyncEventArgsUserToken(timeout);
			socketAsyncEventArgs.Completed += (sender,
			                                   args) =>
			{
				var userToken = args.UserToken;
				var socketAsyncEventArgsUserToken = (SocketAsyncEventArgsUserToken) userToken;
				socketAsyncEventArgsUserToken.Signal();
			};

			return socketAsyncEventArgs;
		}
	}
}
