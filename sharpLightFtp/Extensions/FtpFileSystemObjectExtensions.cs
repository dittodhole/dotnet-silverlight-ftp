using System.Collections.Generic;

namespace sharpLightFtp.Extensions
{
	internal static class FtpFileSystemObjectExtensions
	{
		internal static IEnumerable<FtpDirectory> GetHierarchy(this FtpFileSystemObject ftpFileSystemObject)
		{
			var current = ftpFileSystemObject.GetParentDirectory();
			if (current == null)
			{
				yield break;
			}
			do
			{
				yield return current;
			} while ((current = current.GetParentDirectory()) != null);
		}
	}
}
