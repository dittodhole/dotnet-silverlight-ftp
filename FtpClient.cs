using System;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace sharpLightFtp
{
	public class FtpClient
	{
		public FtpClient(string username, string password, string server, int port)
			: this(username, password, server)
		{
			this.Port = port;
		}

		public FtpClient(string username, string password, string server)
			: this(username, password)
		{
			this.Server = server;
		}

		public FtpClient(string username, string password)
			: this()
		{
			this.Username = username;
			this.Password = password;
		}

		public FtpClient()
		{
			this.Encoding = Encoding.UTF8;
		}

		public FtpClient(Uri uri)
			: this()
		{
			Contract.Requires(uri != null);
			Contract.Requires(string.Equals(uri.Scheme, Uri.UriSchemeFtp));

			var uriBuilder = new UriBuilder(uri);

			this.Username = uriBuilder.UserName;
			this.Password = uriBuilder.Password;
			this.Server = uriBuilder.Host;
			this.Port = uriBuilder.Port;
		}

		public string Server { get; set; }
		public int Port { get; set; }
		public string Username { get; set; }
		public string Password { get; set; }
		public Encoding Encoding { get; set; }

		/*
		public event Action<string, bool> DirectoryExistsCompleted;

		private void RaiseDirectoryExistsCompleted(string path, bool directoryExists)
		{
			var handler = this.DirectoryExistsCompleted;
			if (handler != null)
			{
				handler.Invoke(path, directoryExists);
			}
		}

		public void DirectoryExistsAsync(string path)
		{
			var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			var remoteEndPoint = new DnsEndPoint(this.Server, this.Port);
			var socketAsyncEventArgs = new SocketAsyncEventArgs
			{
				RemoteEndPoint = remoteEndPoint
			};
			socketAsyncEventArgs.Completed += (sender, eventArgs) =>
			{
				var success = eventArgs.SocketError == SocketError.Success;
				if (!success)
				{
					this.RaiseDirectoryExistsCompleted(path, false);
				}

				this.SendUsername(socket, this.Username, previousResult =>
				{
					if (!previousResult)
					{
						this.RaiseDirectoryExistsCompleted(path, false);
					}

					this.SendPassword(socket, this.Password, result =>
					{
						this.RaiseDirectoryExistsCompleted(path, result);
					});
				});
			};
			socket.ConnectAsync(socketAsyncEventArgs);
		}
		*/

		public event Action<FtpCommandCompletedEventArgs> TestConnectionCompleted;

		public void RaiseTestConnectionCompleted(FtpCommandCompletedEventArgs ftpCommandCompletedEventArgs)
		{
			var handler = this.TestConnectionCompleted;
			if (handler != null)
			{
				handler.Invoke(ftpCommandCompletedEventArgs);
			}
		}

		public void TestConnectionAsync()
		{
			Contract.Requires(!string.IsNullOrWhiteSpace(this.Server));

			var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			var host = string.Concat(Uri.UriSchemeFtp, Uri.SchemeDelimiter, this.Server);
			var remoteEndPoint = new DnsEndPoint(host, this.Port);
			var socketAsyncEventArgs = new SocketAsyncEventArgs
			{
				RemoteEndPoint = remoteEndPoint
			};
			socketAsyncEventArgs.Completed += (sender, eventArgs) =>
			{
				if (!eventArgs.IsSuccess())
				{
					var ftpCommandCompletedEventArgs = new FtpCommandCompletedEventArgs
					{
						Exception = eventArgs.GetException(),
						Success = eventArgs.IsSuccess()
					};
					this.RaiseTestConnectionCompleted(ftpCommandCompletedEventArgs);
					return;
				}
				this.SendUsername(socket, this.Username, e0 =>
				{
					if (!e0.IsSuccess())
					{
						var ftpCommandCompletedEventArgs = new FtpCommandCompletedEventArgs
						{
							Exception = eventArgs.GetException(),
							Success = false
						};
						this.RaiseTestConnectionCompleted(ftpCommandCompletedEventArgs);
						return;
					}

					this.SendPassword(socket, this.Password, e1 =>
					{
						var ftpCommandCompletedEventArgs = new FtpCommandCompletedEventArgs()
						{
							Exception = e1.GetException(),
							Success = e1.IsSuccess()
						};
						this.RaiseTestConnectionCompleted(ftpCommandCompletedEventArgs);
					});
				});
			};
			socket.ConnectAsync(socketAsyncEventArgs);
		}

		private void SendUsername(Socket socket, string username, Action<SocketAsyncEventArgs> nextCommandFunc)
		{
			Contract.Requires(socket != null);

			if (string.IsNullOrWhiteSpace(username))
			{
				if (nextCommandFunc != null)
				{
					var socketAsyncEventArgs = new SocketAsyncEventArgs();
					nextCommandFunc.Invoke(socketAsyncEventArgs);
				}
				return;
			}

			var command = string.Format("username: {0}", username);
			this.SendCommand(socket, command, nextCommandFunc);
		}

		private void SendPassword(Socket socket, string password, Action<SocketAsyncEventArgs> nextCommandFunc)
		{
			Contract.Requires(socket != null);

			if (string.IsNullOrWhiteSpace(password))
			{
				if (nextCommandFunc != null)
				{
					var socketAsyncEventArgs = new SocketAsyncEventArgs();
					nextCommandFunc.Invoke(socketAsyncEventArgs);
				}
				return;
			}

			var command = string.Format("password: {0}", password);
			this.SendCommand(socket, command, nextCommandFunc);
		}

		private void SendCommand(Socket socket, string command, Action<SocketAsyncEventArgs> nextCommandFunc)
		{
			Contract.Requires(socket != null);
			Contract.Requires(!string.IsNullOrWhiteSpace(command));

			var commandBuffer = this.Encoding.GetBytes(command);

			var socketAsyncEventArgs = new SocketAsyncEventArgs();
			socketAsyncEventArgs.SetBuffer(commandBuffer, 0, commandBuffer.Length);
			socketAsyncEventArgs.Completed += (sender, e) =>
			{
				if (nextCommandFunc == null)
				{
					return;
				}

				nextCommandFunc.Invoke(e);
			};
			socket.SendAsync(socketAsyncEventArgs);
		}
	}
}
