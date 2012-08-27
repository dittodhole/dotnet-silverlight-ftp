using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace sharpLightFtp.Extensions
{
	internal static class FtpFileSystemObjectExtensions
	{
		internal static IEnumerable<FtpDirectory> GetHierarchy(this FtpFileSystemObject ftpFileSystemObject)
		{
			Contract.Requires(ftpFileSystemObject != null);

			var current = ftpFileSystemObject.ParentDirectory;
			while (current != null)
			{
				yield return current;
				current = current.ParentDirectory;
			}
		}
	}
}
