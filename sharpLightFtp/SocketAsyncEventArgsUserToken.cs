using System;
using System.Threading;

namespace sharpLightFtp
{
	public sealed class SocketAsyncEventArgsUserToken
	{
		private readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(false);
		private readonly TimeSpan _timeout;

		internal SocketAsyncEventArgsUserToken(TimeSpan timeout)
		{
			this._timeout = timeout;
		}

		private AutoResetEvent AutoResetEvent
		{
			get
			{
				return this._autoResetEvent;
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
