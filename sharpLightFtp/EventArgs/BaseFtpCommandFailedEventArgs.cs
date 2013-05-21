using System;
using System.Net.Sockets;

namespace sharpLightFtp.EventArgs
{
	public abstract class BaseFtpCommandFailedEventArgs : System.EventArgs
	{
		public abstract bool TimedOut { get; }
		public abstract SocketError SocketError { get; }
		public abstract TimeSpan Timeout { get; }
		public abstract Exception Exception { get; }
		public abstract SocketAsyncOperation LastOperation { get; }
	}
}
