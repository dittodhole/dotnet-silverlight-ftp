using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using sharpLightFtp.EventArgs;

namespace sharpLightFtp.Extensions
{
	internal static class ComplexSocketExtensions
	{
		internal static bool Connect(this ComplexSocket complexSocket, TimeSpan timeout)
		{
			Contract.Requires(complexSocket != null);

			using (var socketAsyncEventArgs = complexSocket.GetSocketAsyncEventArgs(timeout))
			{
				var success = complexSocket.DoInternal(socket => socket.ConnectAsync, socketAsyncEventArgs);
				if (!success)
				{
					return false;
				}
			}

			return true;
		}

		internal static bool Authenticate(this ComplexSocket complexSocket, TimeSpan timeout, Encoding encoding, string username, string password)
		{
			Contract.Requires(complexSocket != null);
			Contract.Requires(complexSocket.IsControlSocket);
			Contract.Requires(encoding != null);

			var complexResult = complexSocket.SendAndReceive(timeout, encoding, "USER {0}", username);
			if (complexResult.FtpResponseType == FtpResponseType.PositiveIntermediate)
			{
				complexResult = complexSocket.SendAndReceive(timeout, encoding, "PASS {0}", password);
				if (!complexResult.Success)
				{
					var message = string.Format("Could not authenticate with USER \"{0}\" and PASS \"{1}\"", username, password);
					var ftpCommandFailedEventArgs = new FtpAuthenticationFailedEventArgs(complexSocket, message);
					complexSocket.RaiseFtpCommandFailedAsync(ftpCommandFailedEventArgs);
					return false;
				}
			}
			else if (!complexResult.Success)
			{
				var message = string.Format("Could not authenticate with USER \"{0}\"", username);
				var ftpCommandFailedEventArgs = new FtpAuthenticationFailedEventArgs(complexSocket, message);
				complexSocket.RaiseFtpCommandFailedAsync(ftpCommandFailedEventArgs);
				return false;
			}

			return true;
		}

		internal static ComplexResult SendAndReceive(this ComplexSocket complexSocket, TimeSpan timeout, Encoding encoding, string commandFormat, object arg0)
		{
			Contract.Requires(complexSocket != null);
			Contract.Requires(encoding != null);
			Contract.Requires(!string.IsNullOrWhiteSpace(commandFormat));

			var command = string.Format(commandFormat, arg0);
			var complexResult = complexSocket.SendAndReceive(timeout, encoding, command);

			return complexResult;
		}

		internal static ComplexResult SendAndReceive(this ComplexSocket complexSocket, TimeSpan timeout, Encoding encoding, string command)
		{
			Contract.Requires(complexSocket != null);
			Contract.Requires(encoding != null);
			Contract.Requires(!string.IsNullOrWhiteSpace(command));

			var success = complexSocket.Send(timeout, encoding, command);
			if (!success)
			{
				return ComplexResult.FailedComplexResult;
			}

			var complexResult = complexSocket.Receive(timeout, encoding);

			return complexResult;
		}

		internal static bool Send(this ComplexSocket complexSocket, TimeSpan timeout, Encoding encoding, string commandFormat, object arg0)
		{
			Contract.Requires(complexSocket != null);
			Contract.Requires(!string.IsNullOrWhiteSpace(commandFormat));

			var command = string.Format(commandFormat, arg0);
			var success = complexSocket.Send(timeout, encoding, command);

			return success;
		}

		internal static bool Send(this ComplexSocket complexSocket, TimeSpan timeout, Encoding encoding, string command)
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
			var success = complexSocket.Send(timeout, memoryStream);

			return success;
		}

		internal static bool Send(this ComplexSocket complexSocket, TimeSpan timeout, Stream stream)
		{
			Contract.Requires(stream != null);

			var buffer = complexSocket.GetSendBuffer();
			int read;
			while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
			{
				var socketAsyncEventArgs = complexSocket.GetSocketAsyncEventArgs(timeout);
				socketAsyncEventArgs.SetBuffer(buffer, 0, read);
				var success = complexSocket.DoInternal(socket => socket.SendAsync, socketAsyncEventArgs);
				if (!success)
				{
					return false;
				}
			}

			return true;
		}

		internal static ComplexResult Receive(this ComplexSocket complexSocket, TimeSpan timeout, Encoding encoding)
		{
			Contract.Requires(complexSocket != null);
			Contract.Requires(encoding != null);

			using (var socketAsyncEventArgs = complexSocket.GetSocketAsyncEventArgs(timeout))
			{
				var responseBuffer = complexSocket.GetReceiveBuffer();
				socketAsyncEventArgs.SetBuffer(responseBuffer, 0, responseBuffer.Length);
				var ftpResponseType = FtpResponseType.None;
				var messages = new List<string>();
				var stringResponseCode = string.Empty;
				var responseCode = 0;
				var responseMessage = string.Empty;

				while (complexSocket.ReceiveChunk(socketAsyncEventArgs))
				{
					var data = socketAsyncEventArgs.GetData(encoding);
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

				var complexResult = new ComplexResult(ftpResponseType, responseCode, responseMessage, messages);

				return complexResult;
			}
		}

		internal static SocketAsyncEventArgs GetSocketAsyncEventArgs(this ComplexSocket complexSocket, TimeSpan timeout)
		{
			Contract.Requires(complexSocket != null);

			var ftpClient = complexSocket.FtpClient;
			var socketClientAccessPolicyProtocol = ftpClient.SocketClientAccessPolicyProtocol;
			var endPoint = complexSocket.EndPoint;
			var asyncEventArgsUserToken = new SocketAsyncEventArgsUserToken(complexSocket, timeout);
			var socketAsyncEventArgs = new SocketAsyncEventArgs
			{
				RemoteEndPoint = endPoint,
				SocketClientAccessPolicyProtocol = socketClientAccessPolicyProtocol,
				UserToken = asyncEventArgsUserToken
			};
			socketAsyncEventArgs.Completed += (sender, args) =>
			{
				var userToken = args.UserToken;
				var socketAsyncEventArgsUserToken = (SocketAsyncEventArgsUserToken) userToken;
				socketAsyncEventArgsUserToken.Signal();
			};

			return socketAsyncEventArgs;
		}

		private static bool ReceiveChunk(this ComplexSocket complexSocket, SocketAsyncEventArgs socketAsyncEventArgs)
		{
			Contract.Requires(complexSocket != null);
			Contract.Requires(socketAsyncEventArgs != null);

			var success = complexSocket.DoInternal(socket => socket.ReceiveAsync, socketAsyncEventArgs);

			return success;
		}

		internal static bool DoInternal(this ComplexSocket complexSocket, Func<Socket, Func<SocketAsyncEventArgs, bool>> predicate, SocketAsyncEventArgs socketAsyncEventArgs)
		{
			Contract.Requires(complexSocket != null);
			Contract.Requires(predicate != null);
			Contract.Requires(socketAsyncEventArgs != null);

			var socket = complexSocket.Socket;
			var socketPredicate = predicate.Invoke(socket);
			var async = socketPredicate.Invoke(socketAsyncEventArgs);
			if (async)
			{
				var userToken = socketAsyncEventArgs.UserToken;
				var socketAsyncEventArgsUserToken = (SocketAsyncEventArgsUserToken) userToken;
				var receivedSignalWithinTime = socketAsyncEventArgsUserToken.WaitForSignal();
				if (!receivedSignalWithinTime)
				{
					var ftpCommandFailedEventArgs = new FtpCommandTimedOutEventArgs(socketAsyncEventArgs);
					complexSocket.RaiseFtpCommandFailedAsync(ftpCommandFailedEventArgs);
					return false;
				}
			}

			var exception = socketAsyncEventArgs.ConnectByNameError;
			if (exception != null)
			{
				var ftpCommandFailedEventArgs = new FtpCommandFailedEventArgs(socketAsyncEventArgs);
				complexSocket.RaiseFtpCommandFailedAsync(ftpCommandFailedEventArgs);
				return false;
			}

			// TODO maybe a check against socketAsyncEventArgs.SocketError == SocketError.Success would be more correct

			return true;
		}
	}
}
