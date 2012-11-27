namespace sharpLightFtp.EventArgs
{
	public sealed class FtpCommandTimedOutEventArgs : BaseFtpCommandFailedEventArgs
	{
		public FtpCommandTimedOutEventArgs(SocketEventArgs socketEventArgs)
			: base(socketEventArgs) {}

		public override bool TimedOut
		{
			get
			{
				return true;
			}
		}
	}
}
