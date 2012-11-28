using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using sharpLightFtp.EventArgs;
using sharpLightFtp.Extensions;

namespace sharpLightFtp
{
	public sealed class FtpClient : IDisposable
	{
		private readonly object _lockControlComplexSocket = new object();
		private readonly Type _typeOfFtpFeatures = typeof (FtpFeatures);
		private ComplexSocket _controlComplexSocket;
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

		public FtpClient()
		{
			this.Encoding = Encoding.UTF8;
			this.ConnectTimeout = TimeSpan.FromSeconds(30);
			this.ReceiveTimeout = TimeSpan.FromSeconds(30);
			this.SendTimeout = TimeSpan.FromMinutes(5);
			this.SendAndReceiveTimeout = TimeSpan.FromSeconds(30);
		}

		public Encoding Encoding { get; set; }
		public TimeSpan ConnectTimeout { get; set; }
		public TimeSpan ReceiveTimeout { get; set; }
		public TimeSpan SendTimeout { get; set; }
		public TimeSpan SendAndReceiveTimeout { get; set; }
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

		#region IDisposable Members

		public void Dispose()
		{
			{
				var controlComplexSocket = this._controlComplexSocket;
				if (controlComplexSocket != null)
				{
					controlComplexSocket.Dispose();
				}
			}
		}

		#endregion

		public event EventHandler<BaseFtpCommandFailedEventArgs> FtpCommandFailed;

		internal void RaiseFtpCommandFailedAsync(BaseFtpCommandFailedEventArgs e)
		{
			var handler = this.FtpCommandFailed;
			if (handler != null)
			{
				Task.Factory.StartNew(() => handler.Invoke(this, e));
			}
		}

		public IEnumerable<FtpListItem> GetListing(string path)
		{
			FtpListType ftpListType;
			if (this._features.HasFlag(FtpFeatures.MLSD))
			{
				ftpListType = FtpListType.MLSD;
			}
			else if (this._features.HasFlag(FtpFeatures.MLST))
			{
				ftpListType = FtpListType.MLST;
			}
			else
			{
				ftpListType = FtpListType.LIST;
			}

			var rawListing = this.GetRawListing(path, ftpListType);
			var ftpListItems = FtpListItem.ParseList(rawListing, ftpListType);

			return ftpListItems;
		}

		public IEnumerable<string> GetRawListing(string path, FtpListType ftpListType)
		{
			{
				var success = this.BasicConnect();
				if (!success)
				{
					return Enumerable.Empty<string>();
				}
			}

			lock (this._lockControlComplexSocket)
			{
				string command;
				switch (ftpListType)
				{
					case FtpListType.MLST:
						command = "MLST";
						break;
					case FtpListType.LIST:
						command = "LIST";
						break;
					case FtpListType.MLSD:
						command = "MLSD";
						break;
					default:
						throw new NotImplementedException();
				}

				var concreteCommand = string.Format("{0} {1}", command, path);

				var controlComplexSocket = this._controlComplexSocket;
				if (this._features.HasFlag(FtpFeatures.PRET))
				{
					// On servers that advertise PRET (DrFTPD), the PRET command must be executed before a passive connection is opened.
					var complexResult = controlComplexSocket.SendAndReceive(this.SendAndReceiveTimeout, this.Encoding, "PRET {0}", concreteCommand);
					var success = complexResult.Success;
					if (!success)
					{
						return Enumerable.Empty<string>();
					}
				}

				ComplexSocket transferComplexSocket;
				{
					transferComplexSocket = this.SetPassive();
					if (transferComplexSocket == null)
					{
						return Enumerable.Empty<string>();
					}
				}
				using (transferComplexSocket)
				{
					{
						// send LIST/MLSD/MLST-command via control socket
						var complexResult = controlComplexSocket.SendAndReceive(this.SendAndReceiveTimeout, this.Encoding, concreteCommand);
						var success = complexResult.Success;
						if (!success)
						{
							return Enumerable.Empty<string>();
						}
					}
					{
						// receive listing via transfer socket
						var connected = transferComplexSocket.Connect(this.ConnectTimeout);
						if (!connected)
						{
							return Enumerable.Empty<string>();
						}

						var complexResult = transferComplexSocket.Receive(this.ReceiveTimeout, this.Encoding);
						var messages = complexResult.Messages;

						return messages;
					}
				}
			}
		}

		public bool CreateDirectory(string path)
		{
			Contract.Requires(!string.IsNullOrWhiteSpace(path));

			ComplexResult complexResult;
			var success = this.TryCreateDirectoryInternal(path, out complexResult);

			return success;
		}

		private bool TryCreateDirectoryInternal(string path, out ComplexResult complexResult)
		{
			Contract.Requires(!string.IsNullOrWhiteSpace(path));

			{
				var success = this.BasicConnect();
				if (!success)
				{
					complexResult = ComplexResult.FailedComplexResult;
					return false;
				}
			}

			lock (this._lockControlComplexSocket)
			{
				complexResult = this._controlComplexSocket.SendAndReceive(this.SendAndReceiveTimeout, this.Encoding, "MKD {0}", path);
				var success = complexResult.Success;

				return success;
			}
		}

		public bool Upload(Stream stream, FtpFile ftpFile)
		{
			Contract.Requires(stream != null);
			Contract.Requires(stream.CanRead);
			Contract.Requires(ftpFile != null);

			{
				var success = this.BasicConnect();
				if (!success)
				{
					return false;
				}
			}

			lock (this._lockControlComplexSocket)
			{
				var controlComplexSocket = this._controlComplexSocket;
				{
					var hierarchy = ftpFile.GetHierarchy().Reverse();

					foreach (var element in hierarchy)
					{
						var name = element.Name;
						var complexResult = controlComplexSocket.SendAndReceive(this.SendAndReceiveTimeout, this.Encoding, "CWD {0}", name);
						var ftpResponseType = complexResult.FtpResponseType;
						switch (ftpResponseType)
						{
							case FtpResponseType.PermanentNegativeCompletion:
								// some parsing of the actual ComplexResult.ResponseCode should be done in here
								// i assume 5xx-state means "directory does not exist" all the time, which might be wrong
								var success = this.TryCreateDirectoryInternal(name, out complexResult);
								if (!success)
								{
									return false;
								}
								goto case FtpResponseType.PositiveCompletion;
							case FtpResponseType.PositiveCompletion:
								continue;
							default:
								return false;
						}
					}
				}

				var transferComplexSocket = this.SetPassive();
				if (transferComplexSocket == null)
				{
					return false;
				}

				using (transferComplexSocket)
				{
					{
						// sending STOR-command via control socket
						var fileName = ftpFile.Name;
						var success = controlComplexSocket.Send(this.SendTimeout, this.Encoding, "STOR {0}", fileName);
						if (!success)
						{
							return false;
						}
					}
					{
						// sending content via transfer socket
						var connected = transferComplexSocket.Connect(this.ConnectTimeout);
						if (!connected)
						{
							return false;
						}

						var success = transferComplexSocket.Send(this.SendTimeout, stream);
						if (!success)
						{
							return false;
						}
					}
				}
				{
					// receiving STOR-response via control socket
					var complexResult = controlComplexSocket.Receive(this.ReceiveTimeout, this.Encoding);
					if (complexResult.FtpResponseType == FtpResponseType.PositiveIntermediate)
					{
						// sometimes we are fast enough to catch the 3xx state ... yep, i know ... *face palm*
						complexResult = controlComplexSocket.Receive(this.ReceiveTimeout, this.Encoding);
					}
					var success = complexResult.Success;

					return success;
				}
			}
		}

		private bool BasicConnect()
		{
			var queue = new Queue<Func<bool>>();
			{
				queue.Enqueue(this.EnsureConnection);
				queue.Enqueue(this.EnsureFeatures);
			}

			var success = queue.ExecuteQueue();

			return success;
		}

		private bool EnsureConnection()
		{
			lock (this._lockControlComplexSocket)
			{
				var complexSocket = this._controlComplexSocket;
				if (complexSocket != null)
				{
					if (complexSocket.Connected)
					{
						return true;
					}
				}
				var controlComplexSocket = this.GetControlComplexSocket();
				var queue = new Queue<Func<bool>>();
				{
					queue.Enqueue(() => controlComplexSocket.Connect(this.ConnectTimeout));
					queue.Enqueue(() =>
					{
						var complexResult = controlComplexSocket.Receive(this.ReceiveTimeout, this.Encoding);
						var success = complexResult.Success;

						return success;
					});
					queue.Enqueue(() => controlComplexSocket.Authenticate(this.SendAndReceiveTimeout, this.Encoding, this.Username, this.Password));
				}

				{
					var success = queue.ExecuteQueue();
					if (!success)
					{
						return false;
					}
				}
				this._controlComplexSocket = controlComplexSocket;

				return true;
			}
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
				if (this._features != FtpFeatures.EMPTY)
				{
					return true;
				}

				var complexResult = this._controlComplexSocket.SendAndReceive(this.SendAndReceiveTimeout, this.Encoding, "FEAT");
				var success = complexResult.Success;
				if (!success)
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

		private ComplexSocket SetPassive()
		{
			lock (this._lockControlComplexSocket)
			{
				var connected = this.EnsureConnection();
				if (!connected)
				{
					return null;
				}

				var complexResult = this._controlComplexSocket.SendAndReceive(this.SendAndReceiveTimeout, this.Encoding, "PASV");
				var success = complexResult.Success;
				if (!success)
				{
					return null;
				}

				var matches = Regex.Match(complexResult.ResponseMessage, "([0-9]+),([0-9]+),([0-9]+),([0-9]+),([0-9]+),([0-9]+)");
				if (!matches.Success)
				{
					return null;
				}
				if (matches.Groups.Count != 7)
				{
					return null;
				}

				var octets = new byte[4];
				for (var i = 1; i <= 4; i++)
				{
					var value = matches.Groups[i].Value;
					byte octet;
					if (!byte.TryParse(value, out octet))
					{
						return null;
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
							return null;
						}
					}
					int p2;
					{
						var value = matches.Groups[6].Value;
						if (!int.TryParse(value, out p2))
						{
							return null;
						}
					}
					//port = p1 * 256 + p2;
					port = (p1 << 8) + p2;
				}

				var transferComplexSocket = this.GetTransferComplexSocket(ipAddress, port);
				return transferComplexSocket;
			}
		}
	}
}
