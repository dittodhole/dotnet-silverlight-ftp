using System.Net.Sockets;

namespace sharpLightFtp.EventArgs
{
	public sealed class FtpCommandTimedOutEventArgs : BaseFtpCommandWithSocketEventArgsFailedEventArgs
	{
		internal FtpCommandTimedOutEventArgs(SocketAsyncEventArgs socketAsyncEventArgs)
			: base(socketAsyncEventArgs) {}

		public override bool TimedOut
		{
			get
			{
				return true;
			}
		}
	}
}
