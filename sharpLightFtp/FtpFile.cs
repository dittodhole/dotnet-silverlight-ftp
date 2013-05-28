namespace sharpLightFtp
{
	public sealed class FtpFile : FtpFileSystemObject
	{
		public FtpFile(string path)
			: base(path) {}

		public string FileName
		{
			get
			{
				var fileName = this.GetFileName();

				return fileName;
			}
		}
	}
}
