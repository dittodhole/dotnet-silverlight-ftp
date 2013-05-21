using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace sharpLightFtp.Extensions
{
	internal static class SocketExtensions
	{
		internal static byte[] GetReceiveBuffer(this Socket socket)
		{
			var receiveBufferSize = socket.ReceiveBufferSize;
			var buffer = new byte[receiveBufferSize];

			return buffer;
		}

		internal static byte[] GetSendBuffer(this Socket socket)
		{
			var sendBufferSize = socket.SendBufferSize;
			var buffer = new byte[sendBufferSize];

			return buffer;
		}

		internal static FtpReply Receive(this Socket socket,
		                                 SocketAsyncEventArgs socketAsyncEventArgs,
		                                 Encoding encoding)
		{
			var responseBuffer = socket.GetReceiveBuffer();
			socketAsyncEventArgs.SetBuffer(responseBuffer,
			                               0,
			                               responseBuffer.Length);
			var ftpResponseType = FtpResponseType.None;
			var messages = new List<string>();
			var stringResponseCode = string.Empty;
			var responseCode = 0;
			var responseMessage = string.Empty;

			while (SocketHelper.WrapAsyncCall(socket.ReceiveAsync,
			                                  socketAsyncEventArgs) == InternalCommandResult.Success)
			{
				var data = socketAsyncEventArgs.GetData(encoding);
				if (string.IsNullOrWhiteSpace(data))
				{
					break;
				}
				var lines = data.Split(Environment.NewLine.ToCharArray(),
				                       StringSplitOptions.RemoveEmptyEntries);
				foreach (var line in lines)
				{
					var match = Regex.Match(line,
					                        @"^(\d{3})\s(.*)$");
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
							var currentCulture = Thread.CurrentThread.CurrentCulture;
							var character = firstCharacter.ToString(currentCulture);
							var intFtpResponseType = Convert.ToInt32(character);
							ftpResponseType = (FtpResponseType) intFtpResponseType;
							responseCode = Int32.Parse(stringResponseCode);
						}
					}
					else
					{
						messages.Add(line);
					}
				}

				var finished = ftpResponseType != FtpResponseType.None;
				if (finished)
				{
					break;
				}
			}

			var ftpReply = new FtpReply(ftpResponseType,
			                            responseCode,
			                            responseMessage,
			                            messages);

			return ftpReply;
		}
	}
}
