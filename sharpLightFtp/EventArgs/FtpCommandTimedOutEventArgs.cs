using System.Net.Sockets;

namespace sharpLightFtp.EventArgs
{
	public sealed class FtpCommandTimedOutEventArgs : BaseFtpCommandWithSocketEventArgsFailedEventArgs
	{
		internal FtpCommandTimedOutEventArgs(SocketAsyncEventArgs socketAsyncEventArgsUserToken)
			: base(socketAsyncEventArgsUserToken) {}

		public override bool TimedOut
		{
			get
			{
				return true;
			}
		}
	}
}
