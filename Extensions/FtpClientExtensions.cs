using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;

namespace sharpLightFtp.Extensions
{
	internal static class FtpClientExtensions
	{
		internal static ComplexSocket GetComplexSocket(this FtpClient ftpClient, int? port = null)
		{
			Contract.Requires(ftpClient != null);
			Contract.Requires(!string.IsNullOrWhiteSpace(ftpClient.Server));

			var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			var endPoint = new DnsEndPoint(ftpClient.Server, port ?? ftpClient.Port);
			var complexSocket = new ComplexSocket(socket, endPoint);

			return complexSocket;
		}
	}
}
