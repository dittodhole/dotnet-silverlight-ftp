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

		protected static void ExecuteQueueAsync(Queue<Func<ComplexResult>> queue, Action<ComplexResult> finalAction)
		{
			Contract.Requires(queue != null);
			Contract.Requires(queue.Any());
			Contract.Requires(finalAction != null);

			ThreadPool.QueueUserWorkItem(callBack => ExecuteQueue(queue, finalAction));
		}

		private static void ExecuteQueue(Queue<Func<ComplexResult>> queue, Action<ComplexResult> finalAction)
		{
			Contract.Requires(queue != null);
			Contract.Requires(queue.Any());
			Contract.Requires(finalAction != null);

			ComplexResult complexResult = null;
			while (queue.Any())
			{
				var predicate = queue.Dequeue();
				complexResult = predicate.Invoke();
				var ftpResponseType = complexResult.FtpResponseType;
				switch (ftpResponseType)
				{
					case FtpResponseType.PositiveCompletion:
					case FtpResponseType.PositiveIntermediate:
					case FtpResponseType.PositivePreliminary:
						continue;
					case FtpResponseType.None:
					case FtpResponseType.PermanentNegativeCompletion:
					case FtpResponseType.TransientNegativeCompletion:
						finalAction.Invoke(complexResult);
						return;
				}
			}

			Contract.Assert(complexResult != null);

			finalAction.Invoke(complexResult);
		}
	}
}
