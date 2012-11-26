using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using sharpLightFtp.EventArgs;

namespace sharpLightFtp.Extensions
{
	internal static class ComplexSocketExtensions
	{
		private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(30);
		private static readonly TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(30);
		private static readonly TimeSpan SendTimeout = TimeSpan.FromMinutes(5);

		internal static bool Connect(this ComplexSocket complexSocket)
		{
			Contract.Requires(complexSocket != null);

			var socket = complexSocket.Socket;

			using (var sendSocketEventArgs = complexSocket.GetSocketEventArgs())
			{
				var async = socket.ConnectAsync(sendSocketEventArgs);
				if (async)
				{
					var receivedSignalWithinTime = sendSocketEventArgs.AutoResetEvent.WaitOne(ConnectTimeout);
					if (!receivedSignalWithinTime)
					{
						return false;
					}
				}

				var exception = sendSocketEventArgs.ConnectByNameError;
				if (exception != null)
				{
					return false;
				}
			}

			return true;
		}

		internal static bool Authenticate(this ComplexSocket complexSocket, string username, string password, Encoding encoding)
		{
			Contract.Requires(complexSocket != null);
			Contract.Requires(complexSocket.IsControlSocket);
			Contract.Requires(encoding != null);

			var complexResult = complexSocket.SendAndReceive(encoding, "USER {0}", username);
			if (complexResult.FtpResponseType == FtpResponseType.PositiveIntermediate)
			{
				complexResult = complexSocket.SendAndReceive(encoding, "PASS {0}", password);
				if (!complexResult.Success)
				{
					return false;
				}
			}
			else if (!complexResult.Success)
			{
				return false;
			}

			return true;
		}

		internal static ComplexResult SendAndReceive(this ComplexSocket complexSocket, Encoding encoding, string commandFormat, object arg0)
		{
			Contract.Requires(complexSocket != null);
			Contract.Requires(encoding != null);
			Contract.Requires(!string.IsNullOrWhiteSpace(commandFormat));

			var command = string.Format(commandFormat, arg0);
			var complexResult = complexSocket.SendAndReceive(encoding, command);

			return complexResult;
		}

		internal static ComplexResult SendAndReceive(this ComplexSocket complexSocket, Encoding encoding, string command)
		{
			Contract.Requires(complexSocket != null);
			Contract.Requires(encoding != null);
			Contract.Requires(!string.IsNullOrWhiteSpace(command));

			var success = complexSocket.Send(encoding, command);
			if (!success)
			{
				return ComplexResult.FailedComplexResult;
			}

			var complexResult = complexSocket.Receive(encoding);

			return complexResult;
		}

		internal static bool Send(this ComplexSocket complexSocket, Encoding encoding, string commandFormat, object arg0)
		{
			Contract.Requires(complexSocket != null);
			Contract.Requires(!string.IsNullOrWhiteSpace(commandFormat));

			var command = string.Format(commandFormat, arg0);
			var success = complexSocket.Send(encoding, command);

			return success;
		}

		internal static bool Send(this ComplexSocket complexSocket, Encoding encoding, string command)
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
			var success = complexSocket.Send(memoryStream);

			return success;
		}

		internal static bool Send(this ComplexSocket complexSocket, Stream stream)
		{
			Contract.Requires(stream != null);

			var socket = complexSocket.Socket;
			var sendBufferSize = socket.SendBufferSize;
			var buffer = new byte[sendBufferSize];
			int read;
			while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
			{
				var socketEventArgs = complexSocket.GetSocketEventArgs();
				socketEventArgs.SetBuffer(buffer, 0, read);
				var async = socket.SendAsync(socketEventArgs);
				if (async)
				{
					var receivedSignalWithinTime = socketEventArgs.AutoResetEvent.WaitOne(SendTimeout);
					if (!receivedSignalWithinTime)
					{
						return false;
					}
				}
				var exception = socketEventArgs.ConnectByNameError;
				if (exception != null)
				{
					return false;
				}
			}
			return true;
		}

		internal static ComplexResult Receive(this ComplexSocket complexSocket, Encoding encoding)
		{
			Contract.Requires(complexSocket != null);
			Contract.Requires(encoding != null);
			
			using (var socketEventArgs = complexSocket.GetSocketEventArgs())
			{
				var responseBuffer = complexSocket.GetReceiveBuffer();
				socketEventArgs.SetBuffer(responseBuffer, 0, responseBuffer.Length);
				var ftpResponseType = FtpResponseType.None;
				var messages = new List<string>();
				var stringResponseCode = string.Empty;
				var responseCode = 0;
				var responseMessage = string.Empty;

				while (complexSocket.ReceiveChunk(socketEventArgs))
				{
					var data = socketEventArgs.GetData(encoding);
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

				var complexResult = new ComplexResult(ftpResponseType, responseCode, responseMessage, messages);

				return complexResult;
			}
		}

		private static bool ReceiveChunk(this ComplexSocket complexSocket, SocketEventArgs socketEventArgs)
		{
			Contract.Requires(complexSocket != null);
			Contract.Requires(socketEventArgs != null);

			var socket = complexSocket.Socket;
			var async = socket.ReceiveAsync(socketEventArgs);
			if (!async)
			{
				return true;
			}

			var receivedSignalWithinTime = socketEventArgs.AutoResetEvent.WaitOne(ReceiveTimeout);
			if (!receivedSignalWithinTime)
			{
				return false;
			}

			return true;
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
