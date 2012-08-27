using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;

namespace sharpLightFtp.Extensions
{
	internal static class FtpClientExtensions
	{
		internal static ComplexSocket GetControlComplexSocket(this FtpClient ftpClient)
		{
			Contract.Requires(ftpClient != null);
			Contract.Requires(!string.IsNullOrWhiteSpace(ftpClient.Server));

			var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			var endPoint = new DnsEndPoint(ftpClient.Server, ftpClient.Port);
			var complexSocket = new ComplexSocket(socket, endPoint, true);

			return complexSocket;
		}
	}
}
