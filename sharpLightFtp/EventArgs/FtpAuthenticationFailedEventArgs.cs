using System;
using System.Net.Sockets;

namespace sharpLightFtp.EventArgs
{
	public sealed class FtpAuthenticationFailedEventArgs : BaseFtpCommandFailedEventArgs
	{
		private readonly string _message;

		internal FtpAuthenticationFailedEventArgs(string message)
		{
			this._message = message;
		}

		public override bool TimedOut
		{
			get
			{
				return false;
			}
		}

		public override SocketError SocketError
		{
			get
			{
				return SocketError.AccessDenied;
			}
		}

		public override TimeSpan Timeout
		{
			get
			{
				return TimeSpan.Zero;
			}
		}

		public override Exception Exception
		{
			get
			{
				return new Exception(this._message);
			}
		}

		public override SocketAsyncOperation LastOperation
		{
			get
			{
				return SocketAsyncOperation.None;
			}
		}
	}
}
