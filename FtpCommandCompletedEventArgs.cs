using System;

namespace sharpLightFtp
{
	public class FtpCommandCompletedEventArgs : EventArgs
	{
		public Exception Exception { get; set; }
		public bool Success { get; set; }
	}
}
