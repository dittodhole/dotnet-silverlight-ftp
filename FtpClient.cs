using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using sharpLightFtp.Extensions;

namespace sharpLightFtp
{
	public sealed class FtpClient : FtpClientBase
	{
		private readonly object _lockControlComplexSocket = new object();
		private readonly object _lockTransferComplexSocket = new object();
		private readonly Type _typeOfFtpFeatures = typeof (FtpFeatures);
		private ComplexSocket _controlComplexSocket;
		private ComplexSocket _transferComplexSocket;
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
		{
			this.Username = username;
			this.Password = password;
		}

		public FtpClient(Uri uri)
		{
			Contract.Requires(uri != null);
			Contract.Requires(String.Equals(uri.Scheme, Uri.UriSchemeFtp));

			var uriBuilder = new UriBuilder(uri);

			this.Username = uriBuilder.UserName;
			this.Password = uriBuilder.Password;
			this.Server = uriBuilder.Host;
			this.Port = uriBuilder.Port;
		}

		public FtpClient() {}

		public string Server { get; set; }
		public string Username { get; set; }
		public string Password { get; set; }

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

		public bool GetFeatures()
		{
			var queue = new Queue<Func<bool>>();
			{
				queue.Enqueue(this.EnsureConnection);
				queue.Enqueue(this.EnsureFeatures);
				queue.Enqueue(this.SetPassive);
			}

			return ExecuteQueue(queue);
		}

		private bool EnsureConnection()
		{
			lock (this._lockControlComplexSocket)
			{
				if (this._controlComplexSocket == null)
				{
					var controlComplexSocket = this.GetControlComplexSocket();
					var queue = new Queue<Func<bool>>();
					{
						queue.Enqueue(() => controlComplexSocket.ConnectToControlSocket(this.Encoding));
						queue.Enqueue(() => controlComplexSocket.Authenticate(this.Username, this.Password, this.Encoding));
					}

					var success = ExecuteQueue(queue);
					if (!success)
					{
						controlComplexSocket.IsFailed = true;
					}
					this._controlComplexSocket = controlComplexSocket;
				}
			}

			var isFailed = this._controlComplexSocket.IsFailed;

			return !isFailed;
		}

		private bool EnsureFeatures()
		{
			lock (this._lockControlComplexSocket)
			{
				var connected = this.EnsureConnection();
				if (!connected)
				{
					return false;
				}
				if (this._features
				    != FtpFeatures.EMPTY)
				{
					return true;
				}
				var complexResult = this._controlComplexSocket.GetFeatures(this.Encoding);
				if (!complexResult.Success)
				{
					return false;
				}

				this._features = FtpFeatures.NONE;

				var complexEnums = (from name in Enum.GetNames(this._typeOfFtpFeatures)
				                    let enumName = name.ToUpper()
				                    let enumValue = Enum.Parse(this._typeOfFtpFeatures, enumName, true)
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
						if (upperMessage.Contains(enumName))
						{
							var enumValue = complexEnum.EnumValue;
							this._features |= enumValue;
						}
					}
				}
			}

			return true;
		}

		private bool SetPassive()
		{
			lock (this._lockControlComplexSocket)
			{
				var connected = this.EnsureConnection();
				if (!connected)
				{
					return false;
				}

				var complexResult = this._controlComplexSocket.SetPassive(this.Encoding);
				if (!complexResult.Success)
				{
					return false;
				}

				var matches = Regex.Match(complexResult.ResponseMessage, "([0-9]+),([0-9]+),([0-9]+),([0-9]+),([0-9]+),([0-9]+)");
				if (!matches.Success)
				{
					return false;
				}
				if (matches.Groups.Count != 7)
				{
					return false;
				}

				var octets = new byte[4];
				for (var i = 1; i <= 4; i++)
				{
					var value = matches.Groups[i].Value;
					byte octet;
					if (!byte.TryParse(value, out octet))
					{
						return false;
					}
					octets[i - 1] = octet;
				}

				var ipAddress = new IPAddress(octets);
				int port;
				{
					int p1;
					{
						var value = matches.Groups[5].Value;
						if (!int.TryParse(value, out p1))
						{
							return false;
						}
					}
					int p2;
					{
						var value = matches.Groups[6].Value;
						if (!int.TryParse(value, out p2))
						{
							return false;
						}
					}
					//port = p1 * 256 + p2;
					port = (p1 << 8) + p2;
				}

				this._transferComplexSocket = GetTransferComplexSocket(ipAddress, port);

				connected = this._transferComplexSocket.ConnectToTransferSocket();
				if (!connected)
				{
					this._transferComplexSocket.IsFailed = true;
				}
			}

			var isFailed = this._transferComplexSocket.IsFailed;
			
			return !isFailed;
		}
	}
}
