using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using sharpLightFtp.Extensions;

namespace sharpLightFtp
{
	public sealed class FtpClient : FtpClientBase
	{
		private FtpFeatures _features = FtpFeatures.EMPTY;
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

		public bool TestConnection()
		{
			using (var controlComplexSocket = this.GetComplexSocket())
			{
				var queue = new Queue<Func<bool>>();
				{
					queue.Enqueue(() => controlComplexSocket.Connect(this.Encoding));
					queue.Enqueue(() => controlComplexSocket.Authenticate(this.Username, this.Password, this.Encoding));
				}

				return ExecuteQueue(queue);
			}
		}

		public bool GetFeatures()
		{
			using (var controlComplexSocket = this.GetComplexSocket())
			{
				var queue = new Queue<Func<bool>>();
				{
					queue.Enqueue(() => controlComplexSocket.Connect(this.Encoding));
					queue.Enqueue(() => controlComplexSocket.Authenticate(this.Username, this.Password, this.Encoding));
					queue.Enqueue(() => this.EnsureFeatures(controlComplexSocket));
				}

				return ExecuteQueue(queue);
			}
		}

		private bool EnsureFeatures(ComplexSocket complexSocket)
		{
			Contract.Requires(complexSocket != null);

			if (this._features
			    != FtpFeatures.EMPTY)
			{
				return true;
			}

			var complexResult = complexSocket.GetFeatures(this.Encoding);
			if (!complexResult.Success)
			{
				return false;
			}

			this._features |= FtpFeatures.NONE;

			var enumType = typeof (FtpFeatures);
			var complexEnums = (from name in Enum.GetNames(enumType)
			                    let enumName = name.ToUpper()
			                    let enumValue = Enum.Parse(enumType, enumName, true)
			                    select new
			                    {
				                    EnumName = enumName,
				                    EnumValue = (FtpFeatures) enumValue
			                    }).ToList();
			foreach (var message in complexResult.Messages)
			{
				var upperMessage = message.ToUpper();
				foreach (var complexEnum in complexEnums)
				{
					var enumName = complexEnum.EnumName;
					if (!upperMessage.Contains(enumName))
					{
						continue;
					}
					var enumValue = complexEnum.EnumValue;
					this._features |= enumValue;
				}
			}

			return true;
		}
	}
}
