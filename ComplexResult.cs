using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using sharpLightFtp.EventArgs;

namespace sharpLightFtp
{
	public sealed class ComplexResult
	{
		private readonly FtpResponseType _ftpResponseType;
		private readonly List<string> _messages = new List<string>();
		private readonly string _responseCode;
		private readonly string _responseMessage;

		public SocketEventArgs SocketAsyncEventArgs;

		public ComplexResult(string data)
		{
			var lines = data.Split(Environment.NewLine.ToArray(), StringSplitOptions.RemoveEmptyEntries);
			foreach (var line in lines)
			{
				var match = Regex.Match(line, @"^(\d{3})\s(.*)$");
				if (match.Success)
				{
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
				else
				{
					this._messages.Add(line);
				}
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

		public IEnumerable<string> Messages
		{
			get
			{
				return this._messages;
			}
		}

		public FtpResponseType FtpResponseType
		{
			get
			{
				return this._ftpResponseType;
			}
		}

		public bool Success
		{
			get
			{
				switch (this.FtpResponseType)
				{
					case FtpResponseType.PositiveIntermediate:
					case FtpResponseType.PositiveCompletion:
					case FtpResponseType.PositivePreliminary:
						return true;
					default:
						return false;
				}
			}
		}
	}
}
