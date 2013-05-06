using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;
using sharpLightFtp.EventArgs;

namespace sharpLightFtp.Extensions
{
	internal static class ComplexSocketExtensions
	{
		internal static bool Authenticate(this ComplexSocket complexSocket,
		                                  string username,
		                                  string password,
		                                  Encoding encoding,
		                                  TimeSpan sendTimeout,
		                                  TimeSpan receiveTimeout)
		{
			Contract.Requires(complexSocket != null);
			Contract.Requires(complexSocket.IsControlSocket);
			Contract.Requires(!string.IsNullOrEmpty(username));
			Contract.Requires(encoding != null);

			{
				var success = complexSocket.Send(string.Format("USER {0}",
				                                               username),
				                                 encoding,
				                                 sendTimeout);
				if (!success)
				{
					return false;
				}
			}
			{
				var complexResult = complexSocket.Receive(encoding,
				                                          receiveTimeout);
				if (complexResult.FtpResponseType == FtpResponseType.PositiveIntermediate)
				{
					{
						var success = complexSocket.Send(string.Format("PASS {0}",
						                                               password),
						                                 encoding,
						                                 sendTimeout);
						if (!success)
						{
							return false;
						}
					}
					{
						complexResult = complexSocket.Receive(encoding,
						                                      receiveTimeout);
						if (!complexResult.Success)
						{
							var message = string.Format("Could not authenticate with USER '{0}' and PASS '{1}'",
							                            username,
							                            password);
							var ftpCommandFailedEventArgs = new FtpAuthenticationFailedEventArgs(complexSocket,
							                                                                     message);
							complexSocket.RaiseFtpCommandFailedAsync(ftpCommandFailedEventArgs);
							return false;
						}
					}
				}
				else if (!complexResult.Success)
				{
					var message = string.Format("Could not authenticate with USER '{0}'",
					                            username);
					var ftpCommandFailedEventArgs = new FtpAuthenticationFailedEventArgs(complexSocket,
					                                                                     message);
					complexSocket.RaiseFtpCommandFailedAsync(ftpCommandFailedEventArgs);
					return false;
				}
			}

			return true;
		}

		internal static bool Send(this ComplexSocket complexSocket,
		                          string command,
		                          Encoding encoding,
		                          TimeSpan timeout)
		{
			Contract.Requires(complexSocket != null);
			Contract.Requires(!string.IsNullOrEmpty(command));
			Contract.Requires(encoding != null);

			if (!command.EndsWith(Environment.NewLine))
			{
				command = string.Concat(command,
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
