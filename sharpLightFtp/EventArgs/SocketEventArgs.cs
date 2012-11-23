using System;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace sharpLightFtp.EventArgs
{
	public sealed class SocketEventArgs : SocketAsyncEventArgs
	{
		// TODO implement dispose for AutoResetEvent

		private static readonly TimeSpan SendTimeout = TimeSpan.FromMinutes(5);

		private readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(false);
		private readonly ComplexSocket _complexSocket;

		internal SocketEventArgs(ComplexSocket complexSocket)
			: this()
		{
			Contract.Requires(complexSocket != null);

			this._complexSocket = complexSocket;
			this.RemoteEndPoint = this.EndPoint;
		}

		private SocketEventArgs()
		{
			this.SocketClientAccessPolicyProtocol = SocketClientAccessPolicyProtocol.Http;
		}

		internal ComplexSocket ComplexSocket
		{
			get
			{
				return this._complexSocket;
			}
		}

		public EndPoint EndPoint
		{
			get
			{
				return this.ComplexSocket.EndPoint;
			}
		}

		public Socket Socket
		{
			get
			{
				return this.ComplexSocket.Socket;
			}
		}

		public AutoResetEvent AutoResetEvent
		{
			get
			{
				return this._autoResetEvent;
			}
		}

		public bool Send(byte[] buffer, int offset, int count)
		{
			this.SetBuffer(buffer, offset, count);
			var socket = this.Socket;
			var async = socket.SendAsync(this); // TODO maybe create extensions for ComplexSocket
			if (async)
			{
				this.AutoResetEvent.WaitOne(SendTimeout);
			}
			var exception = this.ConnectByNameError;
			if (exception != null)
			{
				return false;
			}
			return true;
		}
	}
}
