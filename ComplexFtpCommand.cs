using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace sharpLightFtp
{
	internal class ComplexFtpCommand
	{
		internal string Command;
		internal Encoding Encoding;
		internal EndPoint EndPoint;
		internal Socket Socket;

		internal void Validate()
		{
			Contract.Assert(!string.IsNullOrWhiteSpace(this.Command));
			Contract.Assert(this.Encoding != null);
			Contract.Assert(this.EndPoint != null);
			Contract.Assert(this.Socket != null);
		}
	}
}
