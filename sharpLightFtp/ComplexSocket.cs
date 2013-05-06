using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using sharpLightFtp.EventArgs;
using sharpLightFtp.Extensions;

namespace sharpLightFtp
{
	public sealed class ComplexSocket : IDisposable
	{
		private readonly EndPoint _endPoint;
		private readonly FtpClient _ftpClient;
		private readonly bool _isControlSocket;

		private readonly Socket _socket = new Socket(AddressFamily.InterNetwork,
		                                             SocketType.Stream,
		                                             ProtocolType.Tcp);

		internal ComplexSocket(FtpClient ftpClient,
		                       EndPoint endPoint,
		                       bool isControlSocket)
		{
			Contract.Requires(ftpClient != null);
			Contract.Requires(endPoint != null);

			this._ftpClient = ftpClient;
			this._endPoint = endPoint;
			this._isControlSocket = isControlSocket;
		}

		private Socket Socket
		{
			get
			{
				return this._socket;
			}
		}

		private EndPoint EndPoint
		{
			get
			{
				return this._endPoint;
			}
		}

		public bool IsControlSocket
		{
			get
			{
				return this._isControlSocket;
			}
		}

		public bool Connected
		{
			get
			{
				return this._socket.Connected;
			}
		}

		public FtpClient FtpClient
		{
			get
			{
				return this._ftpClient;
			}
		}

		#region IDisposable Members

		public void Dispose()
		{
			this.Socket.Dispose();
		}

		#endregion

		private byte[] GetReceiveBuffer()
		{
			var socket = this.Socket;
			var receiveBufferSize = socket.ReceiveBufferSize;
			var buffer = new byte[receiveBufferSize];

			return buffer;
		}

		private byte[] GetSendBuffer()
		{
			var socket = this.Socket;
			var sendBufferSize = socket.SendBufferSize;
			var buffer = new byte[sendBufferSize];

			return buffer;
		}

		internal void RaiseFtpCommandFailedAsync(BaseFtpCommandFailedEventArgs e)
		{
			this.FtpClient.RaiseFtpCommandFailedAsync(e);
		}

		internal bool Connect(TimeSpan timeout)
		{
			using (var socketAsyncEventArgs = this.GetSocketAsyncEventArgs(timeout))
			{
				var success = this.DoInternal(socket => socket.ConnectAsync,
				                              socketAsyncEventArgs);
				if (!success)
				{
					return false;
				}
			}

			return true;
		}

		private bool ReceiveChunk(SocketAsyncEventArgs socketAsyncEventArgs)
		{
			Contract.Requires(socketAsyncEventArgs != null);

			var success = this.DoInternal(socket => socket.ReceiveAsync,
			                              socketAsyncEventArgs);

			return success;
		}

		private bool DoInternal(Func<Socket, Func<SocketAsyncEventArgs, bool>> predicate,
		                        SocketAsyncEventArgs socketAsyncEventArgs)
		{
			Contract.Requires(predicate != null);
			Contract.Requires(socketAsyncEventArgs != null);

			var socket = this.Socket;
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
					this.RaiseFtpCommandFailedAsync(ftpCommandFailedEventArgs);
					return false;
				}
			}

			var exception = socketAsyncEventArgs.ConnectByNameError;
			if (exception != null)
			{
				var ftpCommandFailedEventArgs = new FtpCommandFailedEventArgs(socketAsyncEventArgs);
				this.RaiseFtpCommandFailedAsync(ftpCommandFailedEventArgs);
				return false;
			}

			// TODO maybe a check against socketAsyncEventArgs.SocketError == SocketError.Success would be more correct

			return true;
		}

		private SocketAsyncEventArgs GetSocketAsyncEventArgs(TimeSpan timeout)
		{
			var ftpClient = this.FtpClient;
			var socketClientAccessPolicyProtocol = ftpClient.SocketClientAccessPolicyProtocol;
			var endPoint = this.EndPoint;
			var asyncEventArgsUserToken = new SocketAsyncEventArgsUserToken(this,
			                                                                timeout);
			var socketAsyncEventArgs = new SocketAsyncEventArgs
			{
				RemoteEndPoint = endPoint,
				SocketClientAccessPolicyProtocol = socketClientAccessPolicyProtocol,
				UserToken = asyncEventArgsUserToken
			};
			socketAsyncEventArgs.Completed += (sender,
			                                   args) =>
			{
				var userToken = args.UserToken;
				var socketAsyncEventArgsUserToken = (SocketAsyncEventArgsUserToken) userToken;
				socketAsyncEventArgsUserToken.Signal();
			};

			return socketAsyncEventArgs;
		}

		internal bool Send(Stream stream,
		                   TimeSpan timeout)
		{
			Contract.Requires(stream != null);

			var buffer = this.GetSendBuffer();
			int read;
			while ((read = stream.Read(buffer,
			                           0,
			                           buffer.Length)) > 0)
			{
				var socketAsyncEventArgs = this.GetSocketAsyncEventArgs(timeout);
				socketAsyncEventArgs.SetBuffer(buffer,
				                               0,
				                               read);
				var success = this.DoInternal(socket => socket.SendAsync,
				                              socketAsyncEventArgs);
				if (!success)
				{
					return false;
				}
			}

			return true;
		}

		internal FtpReply Receive(Encoding encoding,
		                               TimeSpan timeout)
		{
			Contract.Requires(encoding != null);

			using (var socketAsyncEventArgs = this.GetSocketAsyncEventArgs(timeout))
			{
				var responseBuffer = this.GetReceiveBuffer();
				socketAsyncEventArgs.SetBuffer(responseBuffer,
				                               0,
				                               responseBuffer.Length);
				var ftpResponseType = FtpResponseType.None;
				var messages = new List<string>();
				var stringResponseCode = string.Empty;
				var responseCode = 0;
				var responseMessage = string.Empty;

				while (this.ReceiveChunk(socketAsyncEventArgs))
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

				var complexResult = new FtpReply(ftpResponseType,
				                                      responseCode,
				                                      responseMessage,
				                                      messages);

				return complexResult;
			}
		}
	}
}
