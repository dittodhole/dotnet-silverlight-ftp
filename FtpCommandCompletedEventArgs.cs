using System;

namespace sharpLightFtp
{
	public class FtpCommandCompletedEventArgs : SocketCompletedEventArgs
	{
		public Exception Exception { get; set; }
		public bool Success { get; set; }
	}
}
