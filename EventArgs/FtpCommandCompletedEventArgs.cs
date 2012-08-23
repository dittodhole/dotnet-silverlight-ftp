using System;

namespace sharpLightFtp.EventArgs
{
	public sealed class FtpCommandCompletedEventArgs : SocketCompletedEventArgs
	{
		public Exception Exception;
		public bool Success;
	}
}
