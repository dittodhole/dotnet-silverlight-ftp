using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

		public FtpClient(string username,
		                 string password,
		                 string server,
		                 int port)
			: this(username,
			       password,
			       server)
		{
			this.Port = port;
		}

		public FtpClient(string username,
		                 string password,
		                 string server)
			: this(username,
			       password)
		{
			this.Server = server;
		}

		public FtpClient(string username,
		                 string password)
			: this()
		{
			this.Username = username;
			this.Password = password;
		}

		public FtpClient(Uri uri)
			: this()
		{
			Contract.Requires(uri != null);
			Contract.Requires(string.Equals(uri.Scheme,
			                                Uri.UriSchemeFtp));

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
			this.SocketClientAccessPolicyProtocol = SocketClientAccessPolicyProtocol.Http;
		}

		public Encoding Encoding { get; set; }
		public TimeSpan ConnectTimeout { get; set; }
		public TimeSpan ReceiveTimeout { get; set; }
		public TimeSpan SendTimeout { get; set; }
		public string Server { get; set; }
		public string Username { get; set; }
		public string Password { get; set; }
		public SocketClientAccessPolicyProtocol SocketClientAccessPolicyProtocol { get; set; }

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
				Task.Factory.StartNew(() => handler.Invoke(this,
				                                           e));
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

			var rawListing = this.GetRawListing(path,
			                                    ftpListType);
			var ftpListItems = FtpListItem.ParseList(rawListing,
			                                         ftpListType);

			return ftpListItems;
		}

		public IEnumerable<string> GetRawListing(string path,
		                                         FtpListType ftpListType)
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

				var concreteCommand = string.Format("{0} {1}",
				                                    command,
				                                    path);

				var controlComplexSocket = this._controlComplexSocket;
				if (this._features.HasFlag(FtpFeatures.PRET))
				{
					// On servers that advertise PRET (DrFTPD), the PRET command must be executed before a passive connection is opened.
					{
						var success = controlComplexSocket.Send(string.Format("PRET {0}",
						                                                      concreteCommand),
						                                        this.Encoding,
						                                        this.SendTimeout);
						if (!success)
						{
							return Enumerable.Empty<string>();
						}
					}
					{
						var complexResult = controlComplexSocket.Receive(this.Encoding,
						                                                 this.ReceiveTimeout);
						var success = complexResult.Success;
						if (!success)
						{
							return Enumerable.Empty<string>();
						}
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
						{
							var success = controlComplexSocket.Send(concreteCommand,
							                                        this.Encoding,
							                                        this.SendTimeout);
							if (!success)
							{
								return Enumerable.Empty<string>();
							}
						}
						{
							var complexResult = controlComplexSocket.Receive(this.Encoding,
							                                                 this.ReceiveTimeout);
							var success = complexResult.Success;
							if (!success)
							{
								return Enumerable.Empty<string>();
							}
						}
					}
					{
						// receive listing via transfer socket
						var connected = transferComplexSocket.Connect(this.ConnectTimeout);
						if (!connected)
						{
							return Enumerable.Empty<string>();
						}

						var complexResult = transferComplexSocket.Receive(this.Encoding,
						                                                  this.ReceiveTimeout);
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
			var success = this.TryCreateDirectoryInternal(path,
			                                              out complexResult);

			return success;
		}

		private bool TryCreateDirectoryInternal(string path,
		                                        out ComplexResult complexResult)
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
				{
					var success = this._controlComplexSocket.Send(string.Format("MKD {0}",
					                                                            path),
					                                              this.Encoding,
					                                              this.SendTimeout);
					if (!success)
					{
						complexResult = null;
						return false;
					}
				}
				{
					complexResult = this._controlComplexSocket.Receive(this.Encoding,
					                                                   this.ReceiveTimeout);
					var success = complexResult.Success;

					return success;
				}
			}
		}

		public bool Upload(Stream stream,
		                   FtpFile ftpFile)
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
					var hierarchy = ftpFile.GetHierarchy()
					                       .Reverse();

					foreach (var element in hierarchy)
					{
						var name = element.Name;
						{
							var success = controlComplexSocket.Send(string.Format("CWD {0}",
							                                                      name),
							                                        this.Encoding,
							                                        this.SendTimeout);
							if (!success)
							{
								return false;
							}
						}
						var complexResult = controlComplexSocket.Receive(this.Encoding,
						                                                 this.ReceiveTimeout);
						var ftpResponseType = complexResult.FtpResponseType;
						switch (ftpResponseType)
						{
							case FtpResponseType.PermanentNegativeCompletion:
								// some parsing of the actual ComplexResult.ResponseCode should be done in here
								// i assume 5xx-state means "directory does not exist" all the time, which might be wrong
								var success = this.TryCreateDirectoryInternal(name,
								                                              out complexResult);
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
						var success = controlComplexSocket.Send(string.Format("STOR {0}",
						                                                      fileName),
						                                        this.Encoding,
						                                        this.SendTimeout);
						if (!success)
						{
							return false;
						}
					}
					{
						// connect to transfer socket
						var connected = transferComplexSocket.Connect(this.ConnectTimeout);
						if (!connected)
						{
							return false;
						}
					}
					{
						// receiving STOR-response via control socket (should be 150/125)
						var complexResult = controlComplexSocket.Receive(this.Encoding,
						                                                 this.ReceiveTimeout);
						var success = complexResult.Success;
						if (!success)
						{
							return false;
						}
					}
					{
						// sending content via transfer socket
						var success = transferComplexSocket.Send(stream,
						                                         this.SendTimeout);
						if (!success)
						{
							return false;
						}
					}
				}

				{
					FtpResponseType ftpResponseType;
					do
					{
						var complexResult = controlComplexSocket.Receive(this.Encoding,
						                                                 this.ReceiveTimeout);
						var success = complexResult.Success;
						if (!success)
						{
							return false;
						}

						ftpResponseType = complexResult.FtpResponseType;
					} while (ftpResponseType != FtpResponseType.PositiveCompletion);

					return true;
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
						var complexResult = controlComplexSocket.Receive(this.Encoding,
						                                                 this.ReceiveTimeout);
						var success = complexResult.Success;

						return success;
					});
					queue.Enqueue(() => controlComplexSocket.Authenticate(this.Username,
					                                                      this.Password,
					                                                      this.Encoding,
					                                                      this.SendTimeout,
					                                                      this.ReceiveTimeout));
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

				{
					var success = this._controlComplexSocket.Send("FEAT",
					                                              this.Encoding,
					                                              this.SendTimeout);
					if (!success)
					{
						return false;
					}
				}
				{
					var complexResult = this._controlComplexSocket.Receive(this.Encoding,
					                                                       this.ReceiveTimeout);
					var success = complexResult.Success;
					if (!success)
					{
						return false;
					}

					this._features = FtpFeatures.NONE;

					var complexEnums = (from name in Enum.GetNames(this._typeOfFtpFeatures)
					                    let enumName = name.ToUpper()
					                    let enumValue = Enum.Parse(this._typeOfFtpFeatures,
					                                               enumName,
					                                               true)
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

				{
					var success = this._controlComplexSocket.Send("PASV",
					                                              this.Encoding,
					                                              this.SendTimeout);
					if (!success)
					{
						return null;
					}
				}
				{
					var complexResult = this._controlComplexSocket.Receive(this.Encoding,
					                                                       this.ReceiveTimeout);
					var success = complexResult.Success;
					if (!success)
					{
						return null;
					}

					var matches = Regex.Match(complexResult.ResponseMessage,
					                          "([0-9]+),([0-9]+),([0-9]+),([0-9]+),([0-9]+),([0-9]+)");
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
						if (!Byte.TryParse(value,
						                   out octet))
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
							if (!Int32.TryParse(value,
							                    out p1))
							{
								return null;
							}
						}
						int p2;
						{
							var value = matches.Groups[6].Value;
							if (!Int32.TryParse(value,
							                    out p2))
							{
								return null;
							}
						}
						//port = p1 * 256 + p2;
						port = (p1 << 8) + p2;
					}

					var transferComplexSocket = this.GetTransferComplexSocket(ipAddress,
					                                                          port);
					return transferComplexSocket;
				}
			}
		}

		private ComplexSocket GetControlComplexSocket()
		{
			var endPoint = new DnsEndPoint(this.Server,
			                               this.Port);
			var complexSocket = new ComplexSocket(this,
			                                      endPoint,
			                                      true);

			return complexSocket;
		}

		private ComplexSocket GetTransferComplexSocket(IPAddress ipAddress,
		                                               int port)
		{
			Contract.Requires(ipAddress != null);

			var endPoint = new IPEndPoint(ipAddress,
			                              port);
			var complexSocket = new ComplexSocket(this,
			                                      endPoint,
			                                      false);

			return complexSocket;
		}
	}
}
