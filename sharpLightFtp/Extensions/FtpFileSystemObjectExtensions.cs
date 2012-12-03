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
			if (current == null)
			{
				yield break;
			}
			do
			{
				yield return current;
			} while ((current = current.ParentDirectory) != null);
		}
	}
}
