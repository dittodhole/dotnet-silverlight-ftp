using System.Net.Sockets;

namespace sharpLightFtp.EventArgs
{
	public sealed class FtpCommandFailedEventArgs : BaseFtpCommandWithSocketEventArgsFailedEventArgs
	{
		internal FtpCommandFailedEventArgs(SocketAsyncEventArgs socketAsyncEventArgs)
			: base(socketAsyncEventArgs) {}

		public override bool TimedOut
		{
			get
			{
				return false;
			}
		}
	}
}
