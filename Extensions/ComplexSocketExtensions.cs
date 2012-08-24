using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace sharpLightFtp.Extensions
{
	internal static class ComplexSocketExtensions
	{
		internal static bool Connect(this ComplexSocket complexSocket)
		{
			Contract.Requires(complexSocket != null);

			var transferSocket = complexSocket.Socket;
			var endPoint = complexSocket.EndPoint;

			var sendSocketEventArgs = endPoint.GetSocketEventArgs();
			var async = transferSocket.ConnectAsync(sendSocketEventArgs);
			if (async)
			{
				sendSocketEventArgs.AutoResetEvent.WaitOne();
			}

			var exception = sendSocketEventArgs.ConnectByNameError;

			return exception == null;
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
			var success = complexFtpCommand.Send();
			if (!success)
			{
				return false;
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
				success = complexFtpCommand.Send();
				if (!success)
				{
					return false;
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
			var endPoint = complexSocket.EndPoint;

			var receiveSocketEventArgs = endPoint.GetSocketEventArgs();
			{
				var responseBuffer = new byte[socket.ReceiveBufferSize];
				receiveSocketEventArgs.SetBuffer(responseBuffer, 0, responseBuffer.Length);
			}

			bool caughtInTheLoop;
			var ftpResponseType = FtpResponseType.None;
			var messages = new List<string>();
			var responseCode = string.Empty;
			var responseMessage = string.Empty;
			var timeout = TimeSpan.FromSeconds(10);
			do
			{
				bool executedInTime;
				var async = socket.ReceiveAsync(receiveSocketEventArgs);
				if (async)
				{
					executedInTime = receiveSocketEventArgs.AutoResetEvent.WaitOne(timeout);
				}
				else
				{
					executedInTime = true;
				}

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
							responseCode = match.Groups[1].Value;
						}
						if (match.Groups.Count > 2)
						{
							responseMessage = match.Groups[2].Value;
						}
						if (!string.IsNullOrWhiteSpace(responseCode))
						{
							var firstCharacter = responseCode.First();
							var character = firstCharacter.ToString();
							ftpResponseType = (FtpResponseType)Convert.ToInt32(character);
						}
					}
					else
					{
						messages.Add(line);
					}
				}

				if (!executedInTime)
				{
					break;
				}
				caughtInTheLoop = ftpResponseType == FtpResponseType.None;
			} while (caughtInTheLoop);

			var complexResult = new ComplexResult(ftpResponseType, responseCode, responseMessage, messages)
			{
				SocketAsyncEventArgs = receiveSocketEventArgs
			};

			return complexResult;
		}
	}
}
