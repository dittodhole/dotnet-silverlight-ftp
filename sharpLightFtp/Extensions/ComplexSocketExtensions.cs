using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using sharpLightFtp.EventArgs;

namespace sharpLightFtp.Extensions
{
	internal static class ComplexSocketExtensions
	{

		internal static bool Connect(this ComplexSocket complexSocket)
		{
			Contract.Requires(complexSocket != null);

			var socket = complexSocket.Socket;

			var sendSocketEventArgs = complexSocket.GetSocketEventArgs();
			using (sendSocketEventArgs)
			{
				var async = socket.ConnectAsync(sendSocketEventArgs);
				if (async)
				{
					sendSocketEventArgs.AutoResetEvent.WaitOne();
				}

				var exception = sendSocketEventArgs.ConnectByNameError;
				var success = exception == null;

				return success;
			}
		}

		internal static bool Authenticate(this ComplexSocket complexSocket, string username, string password, Encoding encoding)
		{
			Contract.Requires(complexSocket != null);
			Contract.Requires(complexSocket.IsControlSocket);
			Contract.Requires(encoding != null);

			var complexFtpCommand = new ComplexFtpCommand(complexSocket, encoding)
			{
				Command = string.Format("USER {0}", username)
			};
			{
				var success = complexFtpCommand.Send();
				if (!success)
				{
					return false;
				}
			}
			var complexResult = complexSocket.Receive(encoding);
			if (!complexResult.Success)
			{
				return false;
			}
			if (complexResult.FtpResponseType == FtpResponseType.PositiveIntermediate)
			{
				complexFtpCommand = new ComplexFtpCommand(complexSocket, encoding)
				{
					Command = string.Format("PASS {0}", password)
				};
				{
					var success = complexFtpCommand.Send();
					if (!success)
					{
						return false;
					}
				}
				complexResult = complexSocket.Receive(encoding);
				if (!complexResult.Success)
				{
					return false;
				}
			}

			return true;
		}

		internal static ComplexResult Receive(this ComplexSocket complexSocket, Encoding encoding)
		{
			Contract.Requires(complexSocket != null);

			var socket = complexSocket.Socket;

			var receiveSocketEventArgs = complexSocket.GetSocketEventArgs();
			{
				var responseBuffer = new byte[socket.ReceiveBufferSize];
				receiveSocketEventArgs.SetBuffer(responseBuffer, 0, responseBuffer.Length);
			}

			var ftpResponseType = FtpResponseType.None;
			var messages = new List<string>();
			var stringResponseCode = string.Empty;
			var responseCode = 0;
			var responseMessage = string.Empty;
			var timeout = TimeSpan.FromSeconds(10);

			using (receiveSocketEventArgs)
			{
				bool ftpResponseTypeMissing;
				bool executedInTime;
				do
				{
					var async = socket.ReceiveAsync(receiveSocketEventArgs);
					executedInTime = !async || receiveSocketEventArgs.AutoResetEvent.WaitOne(timeout);
					var data = receiveSocketEventArgs.GetData(encoding);
					if (string.IsNullOrWhiteSpace(data))
					{
						break;
					}
					var lines = data.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
					foreach (var line in lines)
					{
						var match = Regex.Match(line, @"^(\d{3})\s(.*)$");
						if (match.Success)
						{
							if (match.Groups.Count > 1)
							{
								stringResponseCode = match.Groups[1].Value;
							}
							if (match.Groups.Count > 2)
							{
								responseMessage = match.Groups[2].Value;
							}
							if (!string.IsNullOrWhiteSpace(stringResponseCode))
							{
								var firstCharacter = stringResponseCode.First();
								var character = firstCharacter.ToString();
								var intFtpResponseType = Convert.ToInt32(character);
								ftpResponseType = (FtpResponseType) intFtpResponseType;
								responseCode = int.Parse(stringResponseCode);
							}
						}
						else
						{
							messages.Add(line);
						}
					}

					ftpResponseTypeMissing = ftpResponseType == FtpResponseType.None;
				} while (ftpResponseTypeMissing && executedInTime);
			}

			var complexResult = new ComplexResult(ftpResponseType, responseCode, responseMessage, messages);

			return complexResult;
		}

		internal static SocketEventArgs GetSocketEventArgs(this ComplexSocket complexSocket)
		{
			Contract.Requires(complexSocket != null);

			var socketEventArgs = new SocketEventArgs(complexSocket);
			socketEventArgs.Completed += (sender, socketAsyncEventArgs) =>
			{
				var syncEventArgs = (SocketEventArgs) socketAsyncEventArgs;
				syncEventArgs.AutoResetEvent.Set();
				// sharpLightFtp.Extensions.ComplexFtpCommandExtensions.Send(this ComplexFtpCommand complexFtpCommand) is waiting for it
			};

			return socketEventArgs;
		}
	}
}
