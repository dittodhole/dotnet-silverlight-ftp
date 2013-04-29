using System;
using System.Diagnostics.Contracts;
using System.Threading;

namespace sharpLightFtp
{
	public sealed class SocketAsyncEventArgsUserToken
	{
		private readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(false);
		private readonly ComplexSocket _complexSocket;
		private readonly TimeSpan _timeout;

		internal SocketAsyncEventArgsUserToken(ComplexSocket complexSocket,
		                                       TimeSpan timeout)
		{
			Contract.Requires(complexSocket != null);

			this._complexSocket = complexSocket;
			this._timeout = timeout;
		}

		private AutoResetEvent AutoResetEvent
		{
			get
			{
				return this._autoResetEvent;
			}
		}

		public ComplexSocket ComplexSocket
		{
			get
			{
				return this._complexSocket;
			}
		}

		public TimeSpan Timeout
		{
			get
			{
				return this._timeout;
			}
		}

		internal void Signal()
		{
			this.AutoResetEvent.Set();
		}

		internal bool WaitForSignal()
		{
			return this.AutoResetEvent.WaitOne(this.Timeout);
		}
	}
}
