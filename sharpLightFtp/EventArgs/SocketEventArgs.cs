using System.Net.Sockets;
using System.Threading;

namespace sharpLightFtp.EventArgs
{
	public sealed class SocketEventArgs : SocketAsyncEventArgs
	{
		private readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

		public AutoResetEvent AutoResetEvent
		{
			get
			{
				return this._autoResetEvent;
			}
		}
	}
}
