using System;
using System.IO;
using System.Text;

namespace sharpLightFtp.Extensions
{
	internal static class ComplexSocketExtensions
	{
		internal static bool Send(this ComplexSocket complexSocket,
		                          string command,
		                          Encoding encoding,
		                          TimeSpan timeout)
		{
			if (!command.EndsWith(Environment.NewLine))
			{
				command = String.Concat(command,
				                        Environment.NewLine);
			}

			var buffer = encoding.GetBytes(command);
			using (var memoryStream = new MemoryStream(buffer))
			{
				var success = complexSocket.Send(memoryStream,
				                                 timeout);
				return success;
			}
		}
	}
}
