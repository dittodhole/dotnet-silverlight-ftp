using System;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;

namespace sharpLightFtp
{
	internal sealed class ComplexSocket : IDisposable
	{
		private readonly EndPoint _endPoint;
		private readonly Socket _socket;

		internal ComplexSocket(Socket socket, EndPoint endPoint)
		{
			Contract.Requires(socket != null);
			Contract.Requires(endPoint != null);

			this._socket = socket;
			this._endPoint = endPoint;
		}

		internal Socket Socket
		{
			get
			{
				return this._socket;
			}
		}

		internal EndPoint EndPoint
		{
			get
			{
				return this._endPoint;
			}
		}

		public void Dispose()
		{
			this.Socket.Dispose();
		}
	}
}
