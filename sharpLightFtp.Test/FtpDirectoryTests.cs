using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace sharpLightFtp.Test
{
	[TestClass]
	public sealed class FtpDirectoryTests
	{
		[TestMethod]
		public void TestCreation1()
		{
			var ftpDirectory = FtpDirectory.Create("/foo/");
			var directoryName = ftpDirectory.DirectoryName;

			Assert.AreEqual("foo",
			                directoryName);
		}

		[TestMethod]
		public void TestCreation2()
		{
			var ftpDirectory = FtpDirectory.Create("/foo/foo1");
			var directoryName = ftpDirectory.DirectoryName;

			Assert.AreEqual("foo1",
			                directoryName);
		}

		[TestMethod]
		public void TestCreation3()
		{
			var ftpDirectory = FtpDirectory.Create("foo");
			var directoryName = ftpDirectory.DirectoryName;

			Assert.AreEqual("foo",
			                directoryName);
		}

		[TestMethod]
		public void TestCreation4()
		{
			var rootFtpDirectory = FtpDirectory.Root;
			var ftpDirectory = FtpDirectory.Create(rootFtpDirectory,
			                                       "test2");
			var directoryName = ftpDirectory.DirectoryName;

			Assert.AreEqual("test2",
			                directoryName);
		}

		[TestMethod]
		public void TestCreation5()
		{
			var ftpDirectory = FtpDirectory.Create("/foo1/foo2/foo3");
			var parentFtpDirectory = ftpDirectory.GetParentFtpDirectory();
			var parentDirectoryName = parentFtpDirectory.DirectoryName;

			Assert.AreEqual("foo2",
			                parentDirectoryName);
		}
	}
}
