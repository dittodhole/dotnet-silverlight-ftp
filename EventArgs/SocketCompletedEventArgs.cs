using System.Net.Sockets;

namespace sharpLightFtp.EventArgs
{
	public abstract class SocketCompletedEventArgs : System.EventArgs
	{
		internal Socket Socket;

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
