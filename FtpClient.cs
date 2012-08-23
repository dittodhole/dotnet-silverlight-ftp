using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Net.Sockets;
using System.Text;
using sharpLightFtp.EventArgs;
using sharpLightFtp.Extensions;

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

		public event EventHandler<FtpCommandCompletedEventArgs> TestConnectionCompleted = (sender, args) => args.DisposeSocket();

		private void RaiseTestConnectionCompleted(object sender, FtpCommandCompletedEventArgs ftpCommandCompletedEventArgs)
		{
			Contract.Requires(ftpCommandCompletedEventArgs != null);

			var handler = this.TestConnectionCompleted;
			if (handler != null)
			{
				handler.Invoke(sender, ftpCommandCompletedEventArgs);
			}
		}

		public void TestConnectionAsync()
		{
			var complexSocket = this.GetComplexSocket();
			var socket = complexSocket.Socket;

			var queue = new Queue<Func<SocketAsyncEventArgs>>();
			{
				queue.Enqueue(() => complexSocket.Connect(this.Encoding));
				queue.Enqueue(() => complexSocket.Authenticate(this.Username, this.Password, this.Encoding));
			}

			Action<SocketAsyncEventArgs> finalAction = asyncEventArgs =>
			{
				var ftpCommandCompletedEventArgs = new FtpCommandCompletedEventArgs
				{
					Socket = socket,
					Exception = asyncEventArgs.GetException(),
					Success = asyncEventArgs.IsSuccess()
				};
				this.RaiseTestConnectionCompleted(this, ftpCommandCompletedEventArgs);
			};
			ExecuteQueueAsync(queue, finalAction);
		}

		public event EventHandler<FtpCommandCompletedEventArgs> GetFeaturesCompleted = (sender, args) => args.DisposeSocket();

		public void RaiseGetFeaturesCompleted(object sender, FtpCommandCompletedEventArgs ftpCommandCompletedEventArgs)
		{
			Contract.Requires(ftpCommandCompletedEventArgs != null);

			var handler = this.GetFeaturesCompleted;
			if (handler != null)
			{
				handler.Invoke(sender, ftpCommandCompletedEventArgs);
			}
		}

		public void GetFeaturesAsync()
		{
			var complexSocket = this.GetComplexSocket();
			var endPoint = complexSocket.EndPoint;
			var socket = complexSocket.Socket;

			var queue = new Queue<Func<SocketAsyncEventArgs>>();
			{
				queue.Enqueue(() => complexSocket.Connect(this.Encoding));
				queue.Enqueue(() => complexSocket.Authenticate(this.Username, this.Password, this.Encoding));
				queue.Enqueue(() => complexSocket.SendFeatures(this.Encoding));
			}

			Action<SocketAsyncEventArgs> finalAction = asyncEventArgs =>
			{
				var ftpCommandCompletedEventArgs = new FtpCommandCompletedEventArgs
				{
					Socket = socket,
					Exception = asyncEventArgs.GetException(),
					Success = asyncEventArgs.IsSuccess()
				};
				this.RaiseGetFeaturesCompleted(this, ftpCommandCompletedEventArgs);
			};
			ExecuteQueueAsync(queue, finalAction);
		}
	}
}
