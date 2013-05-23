using System.Net;

namespace sharpLightFtp.Extensions
{
	public static class FtpClientExtensions
	{
		internal static ComplexSocket CreateControlComplexSocket(this FtpClient ftpClient)
		{
			// TODO add check for ftpClient.Port 0 - 0xffff

			var endPoint = new DnsEndPoint(ftpClient.Server,
			                               ftpClient.Port);

			var complexSocket = new ComplexSocket(ftpClient,
			                                      endPoint,
			                                      true);

			return complexSocket;
		}

		internal static ComplexSocket CreateTransferComplexSocket(this FtpClient ftpClient,
		                                                          IPEndPoint ipEndPoint)
		{
			// TODO add check for ftpClient.Port 0 - 0xffff

			var complexSocket = new ComplexSocket(ftpClient,
			                                      ipEndPoint,
			                                      false);

			return complexSocket;
		}
	}
}
