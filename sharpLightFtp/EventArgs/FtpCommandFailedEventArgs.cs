namespace sharpLightFtp.EventArgs
{
	public sealed class FtpCommandFailedEventArgs : BaseFtpCommandFailedEventArgs
	{
		public FtpCommandFailedEventArgs(SocketEventArgs socketEventArgs)
			: base(socketEventArgs) {}

		public override bool TimedOut
		{
			get
			{
				return false;
			}
		}
	}
}
