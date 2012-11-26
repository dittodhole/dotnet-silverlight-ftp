using System.Diagnostics.Contracts;
using System.Net;
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

		internal SocketEventArgs(ComplexSocket complexSocket)
			: this()
		{
			Contract.Requires(complexSocket != null);

			this._complexSocket = complexSocket;
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
