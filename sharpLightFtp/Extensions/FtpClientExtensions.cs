using System.Diagnostics.Contracts;
using System.Net;

namespace sharpLightFtp.Extensions
{
	internal static class FtpClientExtensions
	{
		internal static ComplexSocket GetControlComplexSocket(this FtpClient ftpClient)
		{
			Contract.Requires(ftpClient != null);

			var endPoint = new DnsEndPoint(ftpClient.Server, ftpClient.Port);
			var complexSocket = new ComplexSocket(ftpClient, endPoint, true);

			return complexSocket;
		}

		internal static ComplexSocket GetTransferComplexSocket(this FtpClient ftpClient, IPAddress ipAddress, int port)
		{
			Contract.Requires(ipAddress != null);
			Contract.Requires(ipAddress != null);

			var endPoint = new IPEndPoint(ipAddress, port);
			var complexSocket = new ComplexSocket(ftpClient, endPoint, false);

			return complexSocket;
		}
	}
}
