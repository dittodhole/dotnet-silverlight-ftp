using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace sharpLightFtp
{
	public abstract class FtpClientBase
	{
		protected FtpClientBase()
		{
			this.Encoding = Encoding.UTF8;
			this.ConnectTimeout = TimeSpan.FromSeconds(30);
			this.ReceiveTimeout = TimeSpan.FromSeconds(30);
			this.SendTimeout = TimeSpan.FromMinutes(5);
			this.SendAndReceiveTimeout = TimeSpan.FromSeconds(30);
		}

		public Encoding Encoding { get; set; }
		public TimeSpan ConnectTimeout { get; set; }
		public TimeSpan ReceiveTimeout { get; set; }
		public TimeSpan SendTimeout { get; set; }
		public TimeSpan SendAndReceiveTimeout { get; set; }

		protected static bool ExecuteQueue(Queue<Func<bool>> queue)
		{
			Contract.Requires(queue != null);
			Contract.Requires(queue.Any());

			while (queue.Any())
			{
				var predicate = queue.Dequeue();
				var success = predicate.Invoke();
				if (!success)
				{
					return false;
				}
			}

			return true;
		}

		internal static ComplexSocket GetTransferComplexSocket(IPAddress ipAddress, int port)
		{
			Contract.Requires(ipAddress != null);

			var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			var endPoint = new IPEndPoint(ipAddress, port);
			var complexSocket = new ComplexSocket(socket, endPoint, false);

			return complexSocket;
		}
	}
}
