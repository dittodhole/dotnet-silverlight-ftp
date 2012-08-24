using System;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace sharpLightFtp
{
	internal sealed class ComplexFtpCommand : IDisposable
	{
		private readonly ComplexSocket _complexSocket;
		private readonly Encoding _encoding;

		internal string Command;

		internal ComplexFtpCommand(ComplexSocket complexSocket, Encoding encoding)
		{
			Contract.Requires(complexSocket != null);
			Contract.Requires(encoding != null);

			this._complexSocket = complexSocket;
			this._encoding = encoding;
		}

		internal ComplexFtpCommand(Socket socket, EndPoint endPoint, Encoding encoding)
			: this(new ComplexSocket(socket, endPoint), encoding) {}

		internal ComplexSocket ComplexSocket
		{
			get
			{
				return this._complexSocket;
			}
		}

		public Encoding Encoding
		{
			get
			{
				return this._encoding;
			}
		}

		#region IDisposable Members

		public void Dispose()
		{
			this.ComplexSocket.Dispose();
		}

		#endregion
	}
}
