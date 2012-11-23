using System;
using System.Net.Sockets;
using System.Threading;

namespace sharpLightFtp.EventArgs
{
	public sealed class SocketEventArgs : SocketAsyncEventArgs
	{
		// TODO implement dispose for AutoResetEvent

		private static readonly TimeSpan SendTimeout = TimeSpan.FromMinutes(5);

		private readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

		public AutoResetEvent AutoResetEvent
		{
			get
			{
				return this._autoResetEvent;
			}
		}

		public bool Send(byte[] buffer, int offset, int count)
		{
			this.SetBuffer(buffer, offset, count);
			var socket = this.ConnectSocket; // TODO check if correct property!
			var async = socket.SendAsync(this);
			if (async)
			{
				this.AutoResetEvent.WaitOne(SendTimeout);
			}
			var exception = this.ConnectByNameError;
			if (exception != null)
			{
				return false;
			}
			return true;
		}
	}
}
