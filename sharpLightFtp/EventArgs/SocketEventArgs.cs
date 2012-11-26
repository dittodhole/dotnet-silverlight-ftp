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

		internal SocketEventArgs(ComplexSocket complexSocket)
			: this(complexSocket.EndPoint) {}

		public SocketEventArgs(EndPoint endPoint)
			: this()
		{
			Contract.Requires(endPoint != null);

			this.RemoteEndPoint = endPoint;
		}

		private SocketEventArgs()
		{
			this.SocketClientAccessPolicyProtocol = SocketClientAccessPolicyProtocol.Http;
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
