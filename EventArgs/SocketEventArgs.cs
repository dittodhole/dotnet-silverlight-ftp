using System;
using System.Net.Sockets;
using System.Threading;

namespace sharpLightFtp.EventArgs
{
	public sealed class SocketEventArgs : SocketAsyncEventArgs,
	                                      IDisposable
	{
		private readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

		public AutoResetEvent AutoResetEvent
		{
			get
			{
				return this._autoResetEvent;
			}
		}

		#region IDisposable Members

		void IDisposable.Dispose()
		{
			if (this._autoResetEvent != null)
			{
				this._autoResetEvent.Dispose();
			}
			this.Dispose();
		}

		#endregion
	}
}
