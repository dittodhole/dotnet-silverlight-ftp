namespace sharpLightFtp.EventArgs
{
	public sealed class UploadProgressEventArgs : System.EventArgs
	{
		private readonly long _bytesSent;
		private readonly long _bytesTotal;

		public UploadProgressEventArgs(long bytesSent,
		                               long bytesTotal)
		{
			this._bytesSent = bytesSent;
			this._bytesTotal = bytesTotal;
		}

		public long BytesSent
		{
			get
			{
				return this._bytesSent;
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
