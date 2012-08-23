using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace sharpLightFtp
{
	public sealed class FtpClient : FtpClientBase
	{
		private int _port;

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

		public int Port
		{
			get
			{
				return this._port;
			}
			set
			{
				Contract.Requires(0 <= value);
				Contract.Requires(value <= 65535);

				this._port = value;
			}
		}

		public string Username { get; set; }
		public string Password { get; set; }

		public event Action<FtpCommandCompletedEventArgs> TestConnectionCompleted = args => args.DisposeSocket();

		private void RaiseTestConnectionCompleted(FtpCommandCompletedEventArgs ftpCommandCompletedEventArgs)
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
			var host = this.Server;
			var port = this.Port;
			var endPoint = new DnsEndPoint(host, port);
			var socketAsyncEventArgs = GetSocketAsyncEventArgs(endPoint);
			socketAsyncEventArgs.Completed += (sender, args0) =>
			{
				var queue = new Queue<Func<SocketAsyncEventArgs>>();
				{
					queue.Enqueue(() => this.SendUsername(socket, endPoint, this.Username));
					queue.Enqueue(() => this.SendPassword(socket, endPoint, this.Password));
				}

				Action<SocketAsyncEventArgs> finalAction = args =>
				{
					if (args == null)
					{
						
					}
					var ftpCommandCompletedEventArgs = new FtpCommandCompletedEventArgs
					{
						Socket = socket,
						Exception = args.GetException(),
						Success = args.IsSuccess()
					};
					this.RaiseTestConnectionCompleted(ftpCommandCompletedEventArgs);
				};
				ExecuteQueue(queue, finalAction);
			};
			socket.ConnectAsync(socketAsyncEventArgs);
		}

		private static void ExecuteQueue(Queue<Func<SocketAsyncEventArgs>> queue, Action<SocketAsyncEventArgs> finalAction)
		{
			Contract.Requires(queue.Any());

			SocketAsyncEventArgs socketAsyncEventArgs = null;
			while (queue.Any())
			{
				var predicate = queue.Dequeue();
				socketAsyncEventArgs = predicate.Invoke();
				var isSuccess = socketAsyncEventArgs.IsSuccess();
				if (!isSuccess)
				{
					finalAction.Invoke(socketAsyncEventArgs);
					return;
				}
			}

			Contract.Assert(socketAsyncEventArgs != null);

			finalAction.Invoke(socketAsyncEventArgs);
		}
	}
}
