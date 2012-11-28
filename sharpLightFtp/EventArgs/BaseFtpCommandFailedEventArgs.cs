using System;
using System.Diagnostics.Contracts;
using System.Net.Sockets;

namespace sharpLightFtp.EventArgs
{
	public abstract class BaseFtpCommandFailedEventArgs : System.EventArgs
	{
		private readonly ComplexSocket _complexSocket;
		private readonly Exception _exception;
		private readonly SocketAsyncOperation _lastOperation;
		private readonly SocketError _socketError;
		private readonly TimeSpan _timeout;

		protected BaseFtpCommandFailedEventArgs(SocketAsyncEventArgs socketAsyncEventArgs)
		{
			Contract.Requires(socketAsyncEventArgs != null);

			// TODO clear things out: is it a good idea to keep socketAsyncEventArgs in here,
			// because we are firing off the using eventHandler asynchronously

			var userToken = socketAsyncEventArgs.UserToken;
			var socketAsyncEventArgsUserToken = (SocketAsyncEventArgsUserToken) userToken;

			this._complexSocket = socketAsyncEventArgsUserToken.ComplexSocket;
			this._timeout = socketAsyncEventArgsUserToken.Timeout;
			this._exception = socketAsyncEventArgs.ConnectByNameError;
			this._socketError = socketAsyncEventArgs.SocketError;
			this._lastOperation = socketAsyncEventArgs.LastOperation;
		}

		public abstract bool TimedOut { get; }

		public SocketError SocketError
		{
			get
			{
				return this._socketError;
			}
		}

		public FtpClient FtpClient
		{
			get
			{
				return this.ComplexSocket.FtpClient;
			}
		}

		public TimeSpan Timeout
		{
			get
			{
				return this._timeout;
			}
		}

		public ComplexSocket ComplexSocket
		{
			get
			{
				return this._complexSocket;
			}
		}

		public Exception Exception
		{
			get
			{
				return this._exception;
			}
		}

		public SocketAsyncOperation LastOperation
		{
			get
			{
				return this._lastOperation;
			}
		}
	}
}
