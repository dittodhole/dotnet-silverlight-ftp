using System.Net.Sockets;

namespace sharpLightFtp.EventArgs
{
	public sealed class FtpCommandFailedEventArgs : BaseFtpCommandFailedEventArgs
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
