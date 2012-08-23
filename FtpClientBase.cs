using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using sharpLightFtp.Extensions;

namespace sharpLightFtp
{
	public abstract class FtpClientBase
	{
		public Encoding Encoding { get; set; }

		protected static void ExecuteQueueAsync(Queue<Func<SocketAsyncEventArgs>> queue, Action<SocketAsyncEventArgs> finalAction)
		{
			Contract.Requires(queue != null);
			Contract.Requires(queue.Any());
			Contract.Requires(finalAction != null);
			ThreadPool.QueueUserWorkItem(callBack => ExecuteQueue(queue, finalAction));
		}

		private static void ExecuteQueue(Queue<Func<SocketAsyncEventArgs>> queue, Action<SocketAsyncEventArgs> finalAction)
		{
			Contract.Requires(queue != null);
			Contract.Requires(queue.Any());
			Contract.Requires(finalAction != null);

			SocketAsyncEventArgs socketAsyncEventArgs = null;
			while (queue.Any())
			{
				var predicate = queue.Dequeue();
				socketAsyncEventArgs = predicate.Invoke();
				var isSuccess = socketAsyncEventArgs.IsSuccess();
				if (!isSuccess)
				{
					finalAction.Invoke(socketAsyncEventArgs);
					return;
				}
			}

			Contract.Assert(socketAsyncEventArgs != null);

			finalAction.Invoke(socketAsyncEventArgs);
		}
	}
}
