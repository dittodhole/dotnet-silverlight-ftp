using System;
using System.Diagnostics.Contracts;
using System.Net.Sockets;

namespace sharpLightFtp.EventArgs
{
	public sealed class FtpAuthenticationFailedEventArgs : BaseFtpCommandFailedEventArgs
	{
		private readonly ComplexSocket _complexSocket;
		private readonly string _message;

		internal FtpAuthenticationFailedEventArgs(ComplexSocket complexSocket, string message)
		{
			Contract.Requires(complexSocket != null);

			this._complexSocket = complexSocket;
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

		public override FtpClient FtpClient
		{
			get
			{
				return this.ComplexSocket.FtpClient;
			}
		}

		public override TimeSpan Timeout
		{
			get
			{
				return TimeSpan.Zero;
			}
		}

		public override ComplexSocket ComplexSocket
		{
			get
			{
				return this._complexSocket;
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
