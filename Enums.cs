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
}
