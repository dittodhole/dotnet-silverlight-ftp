﻿using System;

namespace sharpLightFtp
{
	public enum FtpResponseType
	{
		/// <summary>
		/// 	No response
		/// </summary>
		None = 0,

		/// <summary>
		/// 	Success
		/// </summary>
		PositivePreliminary = 1,

		/// <summary>
		/// 	Successs
		/// </summary>
		PositiveCompletion = 2,

		/// <summary>
		/// 	Succcess
		/// </summary>
		PositiveIntermediate = 3,

		/// <summary>
		/// 	Temporary failure
		/// </summary>
		TransientNegativeCompletion = 4,

		/// <summary>
		/// 	Permanent failure
		/// </summary>
		PermanentNegativeCompletion = 5
	}

	[Flags]
	public enum FtpFeatures
	{
		/// <summary>
		/// 	Features haven't been loaded yet
		/// </summary>
		EMPTY = -1,

		/// <summary>
		/// 	This server said it doesn't support anything!
		/// </summary>
		NONE = 0,

		/// <summary>
		/// 	Supports the MLST command
		/// </summary>
		MLST = 1,

		/// <summary>
		/// 	Supports the MLSD command
		/// </summary>
		MLSD = 2,
		/*
		/// <summary>
		/// 	Supports the SIZE command
		/// </summary>
		SIZE = 4,

		/// <summary>
		/// 	Supports the MDTM command
		/// </summary>
		MDTM = 8,

		/// <summary>
		/// 	Supports download/upload stream resumes
		/// </summary>
		REST = 16,

		/// <summary>
		/// 	Supports the EPSV command
		/// </summary>
		EPSV = 32,

		/// <summary>
		/// 	Supports the EPRT command
		/// </summary>
		EPRT = 64,

		/// <summary>
		/// 	Supports retrieving modification times on directories
		/// </summary>
		MDTMDIR = 128,

		/// <summary>
		/// 	Supports for UTF8
		/// </summary>
		UTF8 = 256,

		/// <summary>
		/// 	PRET Command used in distributed ftp server software DrFTPD
		/// </summary>
		//PRET = 512*/
	}
}
