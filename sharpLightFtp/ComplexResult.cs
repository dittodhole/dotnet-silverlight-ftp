using System;
using System.Collections.Generic;
using sharpLightFtp.EventArgs;

namespace sharpLightFtp
{
	internal sealed class ComplexResult : IDisposable
	{
		private readonly FtpResponseType _ftpResponseType;
		private readonly List<string> _messages = new List<string>();
		private readonly string _responseCode;
		private readonly string _responseMessage;

		internal SocketEventArgs SocketAsyncEventArgs;

		internal ComplexResult(FtpResponseType ftpResponseType, string responseCode, string responseMessage, IEnumerable<string> messages)
		{
			this._ftpResponseType = ftpResponseType;
			this._responseCode = responseCode;
			this._responseMessage = responseMessage;
			this._messages.AddRange(messages);
		}

		internal string ResponseCode
		{
			get
			{
				return this._responseCode;
			}
		}

		internal string ResponseMessage
		{
			get
			{
				return this._responseMessage;
			}
		}

		internal IEnumerable<string> Messages
		{
			get
			{
				return this._messages;
			}
		}

		internal FtpResponseType FtpResponseType
		{
			get
			{
				return this._ftpResponseType;
			}
		}

		internal bool Success
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

		#region IDisposable Members

		public void Dispose()
		{
			var socketAsyncEventArgs = this.SocketAsyncEventArgs;
			if (socketAsyncEventArgs != null)
			{
				socketAsyncEventArgs.Dispose();
			}
		}

		#endregion
	}
}
