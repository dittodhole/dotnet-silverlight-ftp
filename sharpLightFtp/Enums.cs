using System;

// originally designed by http://netftp.codeplex.com/

namespace sharpLightFtp
{
	public enum FtpResponseType
	{
		/// <summary>No response</summary>
		None = 0,

		/// <summary>Success</summary>
		PositivePreliminary = 1,

		/// <summary>Successs</summary>
		PositiveCompletion = 2,

		/// <summary>Succcess</summary>
		PositiveIntermediate = 3,

		/// <summary>Temporary failure</summary>
		TransientNegativeCompletion = 4,

		/// <summary>Permanent failure</summary>
		PermanentNegativeCompletion = 5
	}

	[Flags]
	public enum FtpFeatures
	{
		// TODO name the enums properly!
		Unknown = 0,
		MLST = 1 << 0,
		MLSD = 1 << 1,
		SIZE = 1 << 2,
		MDTM = 1 << 3,
		REST = 1 << 4,
		EPSV = 1 << 5,
		EPRT = 1 << 6,
		MDTMDIR = 1 << 8,
		UTF8 = 1 << 9,
		PRET = 1 << 10
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
