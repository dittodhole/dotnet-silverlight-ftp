using System;

// originally designed by http://netftp.codeplex.com/

namespace sharpLightFtp
{
	public enum FtpResponseType
	{
		None = 0,
		PositivePreliminary = 1,
		PositiveCompletion = 2,
		PositiveIntermediate = 3,
		TransientNegativeCompletion = 4,
		PermanentNegativeCompletion = 5
	}

	[Flags]
	public enum FtpFeatures
	{
		// TODO name the enums properly!
		Unknown = 0,
		MLST = 1 << 0,
		MLSD = 1 << 1,
		//SIZE = 1 << 2,
		//MDTM = 1 << 3,
		//REST = 1 << 4,
		//EPSV = 1 << 5,
		//EPRT = 1 << 6,
		//MDTMDIR = 1 << 8,
		//UTF8 = 1 << 9,
		//MFMT = 1 << 11,
		//MFCT = 1 << 12,
		//MFF = 1 << 13,
		//STAT = 1 << 14
	}

	public enum FtpListType
	{
		// TODO name the enums properly!
		LIST,
		MLSD,
		MLST
	}

	public enum FtpObjectType
	{
		Unknown,
		Directory,
		File,
		Link,
		Device
	}
}
