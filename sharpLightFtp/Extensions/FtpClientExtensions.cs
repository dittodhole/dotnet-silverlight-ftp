using System;
using System.Diagnostics.Contracts;
using System.Net;

namespace sharpLightFtp.Extensions
{
	public static class FtpClientExtensions
	{
		internal static ComplexSocket CreateControlComplexSocket(this FtpClient ftpClient)
		{
			Contract.Requires(ftpClient != null);
			Contract.Requires(!String.IsNullOrEmpty(ftpClient.Server));

			// TODO add check for ftpClient.Port 0 - 0xffff

			var endPoint = new DnsEndPoint(ftpClient.Server,
			                               ftpClient.Port);

			var complexSocket = new ComplexSocket(ftpClient,
			                                      endPoint,
			                                      true);

			return complexSocket;
		}

		internal static ComplexSocket CreateTransferComplexSocket(this FtpClient ftpClient,
		                                                          IPAddress ipAddress,
		                                                          int port)
		{
			Contract.Requires(ftpClient != null);
			Contract.Requires(ipAddress != null);

			// TODO add check for ftpClient.Port 0 - 0xffff

			var endPoint = new IPEndPoint(ipAddress,
			                              port);

			var complexSocket = new ComplexSocket(ftpClient,
			                                      endPoint,
			                                      false);

			return complexSocket;
		}
	}
}
