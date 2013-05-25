namespace sharpLightFtp
{
	public sealed class FtpDirectory : FtpFileSystemObject
	{
		internal static readonly FtpDirectory Root = new FtpDirectory("/");

		public FtpDirectory(string path)
			: base(path) {}
	}
}
