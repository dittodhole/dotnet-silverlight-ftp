using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace sharpLightFtp
{
	public abstract class FtpClientBase
	{
		public Encoding Encoding { get; set; }

		protected static bool ExecuteQueue(Queue<Func<bool>> queue)
		{
			Contract.Requires(queue != null);
			Contract.Requires(queue.Any());

			bool success;
			do
			{
				var predicate = queue.Dequeue();
				success = predicate.Invoke();
			} while (queue.Any());

			return success;
		}
	}
}
