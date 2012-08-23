using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text.RegularExpressions;

namespace sharpLightFtp
{
	public class ComplexResult
	{
		private readonly FtpResponseType _ftpResponseType;
		private readonly string _responseCode;
		private readonly string _responseMessage;

		public ComplexResult(string data)
		{
			var match = Regex.Match(data, @"^(\d{3})\s(.*)$");

			Contract.Assert(match.Success);

			if (match.Groups.Count > 1)
			{
				this._responseCode = match.Groups[1].Value;
			}
			if (match.Groups.Count > 2)
			{
				this._responseMessage = match.Groups[2].Value;
			}
			if (!string.IsNullOrWhiteSpace(this._responseCode))
			{
				var firstCharacter = this._responseCode.First();
				var character = firstCharacter.ToString();
				this._ftpResponseType = (FtpResponseType) Convert.ToInt32(character);
			}
		}

		public string ResponseCode
		{
			get
			{
				return this._responseCode;
			}
		}

		public string ResponseMessage
		{
			get
			{
				return this._responseMessage;
			}
		}

		public FtpResponseType FtpResponseType
		{
			get
			{
				return this._ftpResponseType;
			}
		}
	}
}
