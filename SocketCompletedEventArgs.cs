using System;
using System.Net.Sockets;

namespace sharpLightFtp
{
	public abstract class SocketCompletedEventArgs : EventArgs
	{
		public Socket Socket { get; set; }

		internal void DisposeSocket()
		{
			var socket = this.Socket;
			if (socket != null)
			{
				socket.Dispose();
			}
		}
	}
}
