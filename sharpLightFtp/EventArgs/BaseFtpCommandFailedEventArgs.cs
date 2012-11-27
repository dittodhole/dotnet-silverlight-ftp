using System;
using System.Diagnostics.Contracts;

namespace sharpLightFtp.EventArgs
{
	public abstract class BaseFtpCommandFailedEventArgs : System.EventArgs
	{
		private readonly SocketEventArgs _socketEventArgs;

		protected BaseFtpCommandFailedEventArgs(SocketEventArgs socketEventArgs)
		{
			Contract.Requires(socketEventArgs != null);

			this._socketEventArgs = socketEventArgs;
		}

		public abstract bool TimedOut { get; }

		public TimeSpan Timeout
		{
			get
			{
				return this.SocketEventArgs.Timeout;
			}
		}

		public ComplexSocket ComplexSocket
		{
			get
			{
				return this.SocketEventArgs.ComplexSocket;
			}
		}

		public Exception Exception
		{
			get
			{
				return this.SocketEventArgs.ConnectByNameError;
			}
		}

		public SocketEventArgs SocketEventArgs
		{
			get
			{
				return this._socketEventArgs;
			}
		}
	}
}
