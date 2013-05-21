using System;
using System.Net.Sockets;

namespace sharpLightFtp.EventArgs
{
	public abstract class BaseFtpCommandWithSocketEventArgsFailedEventArgs : BaseFtpCommandFailedEventArgs
	{
		private readonly Exception _exception;
		private readonly SocketAsyncOperation _lastOperation;
		private readonly SocketError _socketError;
		private readonly TimeSpan _timeout;

		protected BaseFtpCommandWithSocketEventArgsFailedEventArgs(SocketAsyncEventArgs socketAsyncEventArgs)
		{
			var userToken = socketAsyncEventArgs.UserToken;
			var socketAsyncEventArgsUserToken = (SocketAsyncEventArgsUserToken) userToken;

			this._timeout = socketAsyncEventArgsUserToken.Timeout;
			this._exception = socketAsyncEventArgs.ConnectByNameError;
			this._socketError = socketAsyncEventArgs.SocketError;
			this._lastOperation = socketAsyncEventArgs.LastOperation;
		}

		public override SocketError SocketError
		{
			get
			{
				return this._socketError;
			}
		}

		public override TimeSpan Timeout
		{
			get
			{
				return this._timeout;
			}
		}

		public override Exception Exception
		{
			get
			{
				return this._exception;
			}
		}

		public override SocketAsyncOperation LastOperation
		{
			get
			{
				return this._lastOperation;
			}
		}
	}
}
