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
		private readonly string _responseCode;
		private readonly List<string> _messages = new List<string>();

		public SocketEventArgs SocketAsyncEventArgs;

		public ComplexResult(string data)
		{
			var lines = data.Split(Environment.NewLine.ToArray(), StringSplitOptions.RemoveEmptyEntries);
			foreach (var line in lines)
			{
				var match = Regex.Match(line, @"^(\d{3})\s(.*)$");
				if (!match.Success)
				{
					continue;
				}

				if (match.Groups.Count > 1)
				{
					this._responseCode = match.Groups[1].Value;
				}
				if (match.Groups.Count > 2)
				{
					var message = match.Groups[2].Value;
					this._messages.Add(message);
				}
				if (!string.IsNullOrWhiteSpace(this._responseCode))
				{
					var firstCharacter = this._responseCode.First();
					var character = firstCharacter.ToString();
					this._ftpResponseType = (FtpResponseType) Convert.ToInt32(character);
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
				return string.Join(Environment.NewLine, this._messages);
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
