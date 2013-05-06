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
			Assert.IsTrue(ftpFeatures.HasFlag(FtpFeatures.SIZE));
			Assert.IsTrue(ftpFeatures.HasFlag(FtpFeatures.MDTM));
			Assert.IsTrue(ftpFeatures.HasFlag(FtpFeatures.REST));
			Assert.IsFalse(ftpFeatures.HasFlag(FtpFeatures.EPSV));
			Assert.IsFalse(ftpFeatures.HasFlag(FtpFeatures.EPRT));
			Assert.IsFalse(ftpFeatures.HasFlag(FtpFeatures.MDTMDIR));
			Assert.IsTrue(ftpFeatures.HasFlag(FtpFeatures.UTF8));
			Assert.IsFalse(ftpFeatures.HasFlag(FtpFeatures.PRET));
			Assert.IsTrue(ftpFeatures.HasFlag(FtpFeatures.MFMT));
			Assert.IsFalse(ftpFeatures.HasFlag(FtpFeatures.MFCT));
			Assert.IsTrue(ftpFeatures.HasFlag(FtpFeatures.MFF));
			Assert.IsFalse(ftpFeatures.HasFlag(FtpFeatures.STAT));
		}
	}
}
