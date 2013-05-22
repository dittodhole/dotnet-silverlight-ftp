using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace sharpLightFtp.Test
{
	[TestClass]
	public sealed class FtpClientTest
	{
		[TestMethod]
		public void TestIssue3()
		{
			var ftpClient = new FtpClient("anonymous",
			                              string.Empty,
			                              "ftp://ftp.mozilla.org");
			var ftpListItems = ftpClient.GetListing("/");
			Assert.IsNotNull(ftpListItems);
			Assert.IsTrue(ftpListItems.Any());
		}
	}
}
