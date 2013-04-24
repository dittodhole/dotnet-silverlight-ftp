using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;
using sharpLightFtp.EventArgs;

namespace sharpLightFtp.Extensions
{
	internal static class ComplexSocketExtensions
	{
		internal static bool Authenticate(this ComplexSocket complexSocket, string username, string password, Encoding encoding, TimeSpan timeout)
		{
			Contract.Requires(complexSocket != null);
			Contract.Requires(complexSocket.IsControlSocket);
			Contract.Requires(encoding != null);

			var complexResult = complexSocket.SendAndReceive(string.Format("USER {0}", username), encoding, timeout);
			if (complexResult.FtpResponseType == FtpResponseType.PositiveIntermediate)
			{
				complexResult = complexSocket.SendAndReceive(string.Format("PASS {0}", password), encoding, timeout);
				if (!complexResult.Success)
				{
					var message = string.Format("Could not authenticate with USER '{0}' and PASS '{1}'", username, password);
					var ftpCommandFailedEventArgs = new FtpAuthenticationFailedEventArgs(complexSocket, message);
					complexSocket.RaiseFtpCommandFailedAsync(ftpCommandFailedEventArgs);
					return false;
				}
			}
			else if (!complexResult.Success)
			{
				var message = string.Format("Could not authenticate with USER '{0}'", username);
				var ftpCommandFailedEventArgs = new FtpAuthenticationFailedEventArgs(complexSocket, message);
				complexSocket.RaiseFtpCommandFailedAsync(ftpCommandFailedEventArgs);
				return false;
			}

			return true;
		}

		internal static ComplexResult SendAndReceive(this ComplexSocket complexSocket, string command, Encoding encoding, TimeSpan timeout)
		{
			Contract.Requires(complexSocket != null);
			Contract.Requires(encoding != null);
			Contract.Requires(!string.IsNullOrWhiteSpace(command));

			var success = complexSocket.Send(command, encoding, timeout);
			if (!success)
			{
				return ComplexResult.FailedComplexResult;
			}

			var complexResult = complexSocket.Receive(encoding, timeout);

			return complexResult;
		}

		internal static bool Send(this ComplexSocket complexSocket, string command, Encoding encoding, TimeSpan timeout)
		{
			Contract.Requires(complexSocket != null);
			Contract.Requires(encoding != null);
			Contract.Requires(!string.IsNullOrWhiteSpace(command));

			if (!command.EndsWith(Environment.NewLine))
			{
				command = string.Concat(command, Environment.NewLine);
			}

			var buffer = encoding.GetBytes(command);
			var memoryStream = new MemoryStream(buffer);
			var success = complexSocket.Send(memoryStream, timeout);

			return success;
		}
	}
}
