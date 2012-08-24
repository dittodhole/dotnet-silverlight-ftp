using System;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;

namespace sharpLightFtp
{
	internal sealed class ComplexSocket : IDisposable
	{
		private readonly EndPoint _endPoint;
		private readonly bool _isControlSocket;
		private readonly Socket _socket;

		internal ComplexSocket(Socket socket, EndPoint endPoint, bool isControlSocket)
			: this(socket, endPoint)
		{
			this._isControlSocket = isControlSocket;
		}

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

		internal bool IsControlSocket
		{
			get
			{
				return this._isControlSocket;
			}
		}

		internal bool IsFailed { get; set; }

		#region IDisposable Members

		public void Dispose()
		{
			this.Socket.Dispose();
		}

		#endregion
	}
}
