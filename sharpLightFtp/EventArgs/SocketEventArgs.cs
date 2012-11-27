using System;
using System.Diagnostics.Contracts;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace sharpLightFtp.EventArgs
{
	public sealed class SocketEventArgs : SocketAsyncEventArgs
	{
		// TODO implement dispose for AutoResetEvent

		private readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(false);
		private readonly ComplexSocket _complexSocket;
		private readonly TimeSpan _timeout;

		public SocketEventArgs(ComplexSocket complexSocket, TimeSpan timeout)
			: this()
		{
			this._complexSocket = complexSocket;
			this._timeout = timeout;
			this.RemoteEndPoint = complexSocket.EndPoint;
		}

		private SocketEventArgs()
		{
			this.SocketClientAccessPolicyProtocol = SocketClientAccessPolicyProtocol.Http;
		}

		private AutoResetEvent AutoResetEvent
		{
			get
			{
				return this._autoResetEvent;
			}
		}

		public ComplexSocket ComplexSocket
		{
			get
			{
				return this._complexSocket;
			}
		}

		public TimeSpan Timeout
		{
			get
			{
				return this._timeout;
			}
		}

		internal void Signal()
		{
			this.AutoResetEvent.Set();
		}

		internal bool WaitForSignal()
		{
			return this.AutoResetEvent.WaitOne(this.Timeout);
		}

		public string GetData(Encoding encoding)
		{
			Contract.Requires(encoding != null);

			var buffer = this.Buffer;
			var offset = this.Offset;
			var bytesTransferred = this.BytesTransferred;
			var data = encoding.GetString(buffer, offset, bytesTransferred);

			return data;
		}
	}
}
