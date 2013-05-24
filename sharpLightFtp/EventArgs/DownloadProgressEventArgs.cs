namespace sharpLightFtp.EventArgs
{
	public sealed class DownloadProgressEventArgs : System.EventArgs
	{
		private readonly long _bytesReceived;
		private readonly long _bytesTotal;

		internal DownloadProgressEventArgs(long bytesReceived,
		                                   long bytesTotal)
		{
			this._bytesReceived = bytesReceived;
			this._bytesTotal = bytesTotal;
		}

		public long BytesReceived
		{
			get
			{
				return this._bytesReceived;
			}
		}

		public long BytesTotal
		{
			get
			{
				return this._bytesTotal;
			}
		}
	}
}
