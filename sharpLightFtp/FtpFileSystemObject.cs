using System.Diagnostics.Contracts;
using System.IO;
using System.Text.RegularExpressions;

namespace sharpLightFtp
{
	public abstract class FtpFileSystemObject
	{
		private string _fullName;

		protected FtpFileSystemObject(string path)
		{
			Contract.Requires(!string.IsNullOrWhiteSpace(path));

			this._fullName = path;
		}

		public string Name
		{
			get
			{
				return this.GetFileName();
			}
			set
			{
				this._fullName = Path.Combine(this._fullName ?? string.Empty,
				                              value);
			}
		}

		public FtpDirectory GetParentDirectory()
		{
			var path = this.GetDirectoryName();
			if (string.IsNullOrWhiteSpace(path))
			{
				return null;
			}
			if (string.Equals(path,
			                  "\\"))
			{
				return null;
			}
			var ftpDirectory = new FtpDirectory(path);

			return ftpDirectory;
		}

		private string GetDirectoryName()
		{
			if (string.Equals(this._fullName,
			                  "/"))
			{
				return null;
			}
			return Path.GetDirectoryName(this._fullName);
		}

		private string GetFileName()
		{
			return Path.GetFileName(this._fullName);
		}

		protected string CleanPath(string path)
		{
			var firstClean = path.Replace('\\',
			                              '/');
			var cleanedPath = Regex.Replace(firstClean,
			                                @"/+",
			                                "/");

			return cleanedPath;
		}
	}
}
