using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace sharpLightFtp.Test
{
	[TestClass]
	public class FtpClientHelperTests
	{
		[TestMethod]
		public void TestParseFtpFeatures()
		{
			var messages = new List<string>
			{
				" MDTM",
				" MFMT",
				" TVFS",
				" UTF8",
				" MFF modify;UNIX.group;UNIX.mode;",
				" MLST modify*;perm*;size*;type*;unique*;UNIX.group*;UNIX.mode*;UNIX.owner*;",
				" LANG ja-JP;ko-KR;bg-BG;zh-CN;it-IT;zh-TW;ru-RU;en-US*;fr-FR",
				" REST STREAM",
				" SIZE"
			};
			var ftpFeatures = FtpClientHelper.ParseFtpFeatures(messages);

			Assert.IsTrue(ftpFeatures.HasFlag(FtpFeatures.MLST));
			Assert.IsFalse(ftpFeatures.HasFlag(FtpFeatures.MLSD));
			//Assert.IsTrue(ftpFeatures.HasFlag(FtpFeatures.SIZE));
			//Assert.IsTrue(ftpFeatures.HasFlag(FtpFeatures.MDTM));
			//Assert.IsTrue(ftpFeatures.HasFlag(FtpFeatures.REST));
			//Assert.IsFalse(ftpFeatures.HasFlag(FtpFeatures.EPSV));
			//Assert.IsFalse(ftpFeatures.HasFlag(FtpFeatures.EPRT));
			//Assert.IsFalse(ftpFeatures.HasFlag(FtpFeatures.MDTMDIR));
			//Assert.IsTrue(ftpFeatures.HasFlag(FtpFeatures.UTF8));
			//Assert.IsTrue(ftpFeatures.HasFlag(FtpFeatures.MFMT));
			//Assert.IsFalse(ftpFeatures.HasFlag(FtpFeatures.MFCT));
			//Assert.IsTrue(ftpFeatures.HasFlag(FtpFeatures.MFF));
			//Assert.IsFalse(ftpFeatures.HasFlag(FtpFeatures.STAT));
		}

		[TestMethod]
		public void TestParseFtpReplyForIssue6()
		{
			const string data = @"227 Entering Passive Mode (63,245,215,56,204,65)";
			var ftpReply = FtpClientHelper.ParseFtpReply(data);
			Assert.AreEqual(ftpReply.ResponseCode,
			                227);

			var ipEndPoint = FtpClientHelper.ParseIPEndPoint(ftpReply);
			Assert.IsNotNull(ipEndPoint);
		}

		[TestMethod]
		public void TestDirectoryChanges1()
		{
			var sourceFtpDirectory = FtpDirectory.Create("/foo/foo1");
			var targetFtpDirectory = FtpDirectory.Create("/foo/foo2");
			var directoryChanges = FtpClientHelper.DirectoryChanges(sourceFtpDirectory,
			                                                        targetFtpDirectory);

			var joinedDirectoryChanges = string.Join(",",
			                                         directoryChanges);
			Assert.AreEqual("..,foo2",
			                joinedDirectoryChanges);
		}

		[TestMethod]
		public void TestDirectoryChanges2()
		{
			var sourceFtpDirectory = FtpDirectory.Create("/foo/foo1/foo2");
			var targetFtpDirectory = FtpDirectory.Create("/foo/foo2");
			var directoryChanges = FtpClientHelper.DirectoryChanges(sourceFtpDirectory,
			                                                        targetFtpDirectory);

			var joinedDirectoryChanges = string.Join(",",
			                                         directoryChanges);
			Assert.AreEqual("..,..,foo2",
			                joinedDirectoryChanges);
		}

		[TestMethod]
		public void TestDirectoryChanges3()
		{
			var sourceFtpDirectory = FtpDirectory.Create("/foo/foo1");
			var targetFtpDirectory = FtpDirectory.Create("/foo/foo2/foo3");
			var directoryChanges = FtpClientHelper.DirectoryChanges(sourceFtpDirectory,
			                                                        targetFtpDirectory);

			var joinedDirectoryChanges = string.Join(",",
			                                         directoryChanges);
			Assert.AreEqual("..,foo2,foo3",
			                joinedDirectoryChanges);
		}

		[TestMethod]
		public void TestDirectoryChanges4()
		{
			var sourceFtpDirectory = FtpDirectory.Create("/foo/foo1");
			var targetFtpDirectory = FtpDirectory.Create("/");
			var directoryChanges = FtpClientHelper.DirectoryChanges(sourceFtpDirectory,
			                                                        targetFtpDirectory);

			var joinedDirectoryChanges = string.Join(",",
			                                         directoryChanges);
			Assert.AreEqual("..,..",
			                joinedDirectoryChanges);
		}
	}
}
