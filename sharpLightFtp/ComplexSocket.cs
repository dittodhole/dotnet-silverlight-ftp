using System;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;
using sharpLightFtp.EventArgs;

namespace sharpLightFtp
{
	public sealed class ComplexSocket : IDisposable
	{
		private readonly EndPoint _endPoint;
		private readonly FtpClient _ftpClient;
		private readonly bool _isControlSocket;
		private readonly Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

		internal ComplexSocket(FtpClient ftpClient, EndPoint endPoint, bool isControlSocket)
		{
			Contract.Requires(ftpClient != null);
			Contract.Requires(endPoint != null);

			this._ftpClient = ftpClient;
			this._endPoint = endPoint;
			this._isControlSocket = isControlSocket;
		}

		public Socket Socket
		{
			get
			{
				return this._socket;
			}
		}

		public EndPoint EndPoint
		{
			get
			{
				return this._endPoint;
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
			this.Socket.Dispose();
		}

		#endregion

		internal byte[] GetReceiveBuffer()
		{
			var socket = this.Socket;
			var receiveBufferSize = socket.ReceiveBufferSize;
			var buffer = new byte[receiveBufferSize];

			return buffer;
		}

		internal byte[] GetSendBuffer()
		{
			var socket = this.Socket;
			var sendBufferSize = socket.SendBufferSize;
			var buffer = new byte[sendBufferSize];

			return buffer;
		}

		internal void RaiseFtpCommandFailedAsync(BaseFtpCommandFailedEventArgs e)
		{
			this.FtpClient.RaiseFtpCommandFailedAsync(e);
		}
	}
}
