using System;

namespace sharpLightFtp
{
	public class FtpException : Exception
	{
		public FtpException() {}

		public FtpException(string message)
			: base(message) {}
	}
}
