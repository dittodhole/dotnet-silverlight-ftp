using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace sharpLightFtp.Extensions
{
	public static class QueueExtensions
	{
		public static bool ExecuteQueue(this Queue<Func<bool>> queue)
		{
			Contract.Requires(queue != null);
			Contract.Requires(queue.Any());

			while (queue.Any())
			{
				var predicate = queue.Dequeue();
				var success = predicate.Invoke();
				if (!success)
				{
					return false;
				}
			}

			return true;
		}
	}
}
