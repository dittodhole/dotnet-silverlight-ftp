using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace sharpLightFtp.Extensions
{
	public static class FtpFileSystemObjectExtensions
	{
		public static IEnumerable<string> GetHierarchy(this FtpDirectory ftpDirectory)
		{
			var fullName = ftpDirectory.FullName;
			if (string.IsNullOrEmpty(fullName))
			{
				return Enumerable.Empty<string>();
			}
			var directories = fullName.Split(new[]
			{
				Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar
			},
			                                 StringSplitOptions.RemoveEmptyEntries);

			return directories;
		}
	}
}
