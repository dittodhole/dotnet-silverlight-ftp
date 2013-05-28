using System;
using System.IO;

namespace sharpLightFtp
{
	public abstract class FtpFileSystemObject
	{
		public const string ParentChangeCommand = "..";
		private readonly string _fullName;

		protected FtpFileSystemObject(string path)
		{
			if (String.IsNullOrEmpty(path))
			{
				throw new ArgumentException("path can not be null or empty",
				                            "path");
			}
			if (path.Contains(ParentChangeCommand))
			{
				throw new ArgumentException(String.Format("path can not contain '{0}'",
				                                          ParentChangeCommand),
				                            "path");
			}
			this._fullName = path.TrimEnd(new[]
			{
				Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar
			});
		}

		public string FullName
		{
			get
			{
				return this._fullName;
			}
		}

		public FtpDirectory GetParentFtpDirectory()
		{
			var containingDirectory = this.GetParentDirectory();
			var ftpDirectory = FtpDirectory.Create(containingDirectory);

			return ftpDirectory;
		}

		internal string GetParentDirectory()
		{
			var directoryName = Path.GetDirectoryName(this._fullName);
			var containingDirectory = directoryName;

			return containingDirectory;
		}

		protected string GetFileName()
		{
			var fileName = Path.GetFileName(this._fullName);

			return fileName;
		}
	}
}
