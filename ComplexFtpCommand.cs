using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace sharpLightFtp
{
	internal sealed class ComplexFtpCommand
	{
		private readonly ComplexSocket _complexSocket;
		private readonly Encoding _encoding;

		internal string[] Commands;

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

		internal string Command
		{
			get
			{
				return this.Commands.SingleOrDefault();
			}
			set
			{
				this.Commands = new[]
				{
					value
				};
			}
		}

		public Encoding Encoding
		{
			get
			{
				return this._encoding;
			}
		}
	}
}
