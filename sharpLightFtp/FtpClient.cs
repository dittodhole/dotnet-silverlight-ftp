using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using sharpLightFtp.Extensions;

namespace sharpLightFtp
{
	public sealed class FtpClient : FtpClientBase,
	                                IDisposable
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

			ComplexResult complexResult;

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

				if (this._features.HasFlag(FtpFeatures.PRET))
				{
					// On servers that advertise PRET (DrFTPD), the PRET command must be executed before a passive connection is opened.
					{
						var complexFtpCommand = new ComplexFtpCommand(this._controlComplexSocket, this.Encoding)
						{
							Command = string.Format("PRET {0}", concreteCommand)
						};
						var success = complexFtpCommand.Send();
						if (!success)
						{
							return Enumerable.Empty<string>();
						}
					}
					{
						complexResult = this._controlComplexSocket.Receive(this.Encoding);
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
						var complexFtpCommand = new ComplexFtpCommand(this._controlComplexSocket, this.Encoding)
						{
							Command = concreteCommand
						};
						var success = complexFtpCommand.Send();
						if (!success)
						{
							return Enumerable.Empty<string>();
						}
					}
					{
						// receive listing via transfer socket
						var connected = transferComplexSocket.Connect();
						if (!connected)
						{
							return Enumerable.Empty<string>();
						}
						complexResult = transferComplexSocket.Receive(this.Encoding);
					}
				}
			}

			var messages = complexResult.Messages;

			return messages;
		}

		public bool CreateDirectory(string path)
		{
			Contract.Requires(!string.IsNullOrWhiteSpace(path));

			{
				var success = this.BasicConnect();
				if (!success)
				{
					return false;
				}
			}

			lock (this._controlComplexSocket)
			{
				var complexFtpCommand = new ComplexFtpCommand(this._controlComplexSocket, this.Encoding)
				{
					Command = string.Format("MKD {0}", path)
				};
				{
					var success = complexFtpCommand.Send();
					if (!success)
					{
						return false;
					}
				}
				var complexResult = this._controlComplexSocket.Receive(this.Encoding);
				{
					var success = complexResult.Success;
					if (!success)
					{
						return false;
					}
				}
			}

			return true;
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
				{
					var hierarchy = ftpFile.GetHierarchy().Reverse();

					foreach (var element in hierarchy)
					{
						changeWorkingDirectory:
						var name = element.Name;
						var complexFtpCommand = new ComplexFtpCommand(this._controlComplexSocket, this.Encoding)
						{
							Command = string.Format("CWD {0}", name)
						};
						var success = complexFtpCommand.Send();
						if (!success)
						{
							return false;
						}
						var complexResult = this._controlComplexSocket.Receive(this.Encoding);
						success = complexResult.Success;
						if (!success)
						{
							success = this.CreateDirectory(name);
							if (!success)
							{
								return false;
							}

							goto changeWorkingDirectory;
						}
					}
				}

				ComplexSocket transferComplexSocket;
				{
					transferComplexSocket = this.SetPassive();
					if (transferComplexSocket == null)
					{
						return false;
					}
				}
				using (transferComplexSocket)
				{
					{
						// sending STOR-command via control socket
						var fileName = ftpFile.Name;
						var complexFtpCommand = new ComplexFtpCommand(this._controlComplexSocket, this.Encoding)
						{
							Command = string.Format("STOR {0}", fileName)
						};
						{
							var success = complexFtpCommand.Send();
							if (!success)
							{
								return false;
							}
						}
					}
					{
						// sending content via transfer socket
						var connected = transferComplexSocket.Connect();
						if (!connected)
						{
							return false;
						}

						var transferSocket = transferComplexSocket.Socket;
						{
							var buffer = new byte[transferSocket.SendBufferSize];
							int read;
							while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
							{
								var socketEventArgs = transferComplexSocket.EndPoint.GetSocketEventArgs();
								socketEventArgs.SetBuffer(buffer, 0, read);
								transferSocket.SendAsync(socketEventArgs);

								socketEventArgs.AutoResetEvent.WaitOne();

								var exception = socketEventArgs.ConnectByNameError;
								if (exception != null)
								{
									return false;
								}
							}
						}
					}
				}
				{
					// receiving STOR-response via control socket
					var complexResult = this._controlComplexSocket.Receive(this.Encoding);
					if (complexResult.FtpResponseType == FtpResponseType.PositiveIntermediate)
					{
						complexResult = this._controlComplexSocket.Receive(this.Encoding);
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

			var success = ExecuteQueue(queue);

			return success;
		}

		private bool EnsureConnection()
		{
			lock (this._lockControlComplexSocket)
			{
				if (this._controlComplexSocket != null)
				{
					if (this._controlComplexSocket.Connected)
					{
						return true;
					}
				}
				else
				{
					var controlComplexSocket = this.GetControlComplexSocket();
					var queue = new Queue<Func<bool>>();
					{
						queue.Enqueue(controlComplexSocket.Connect);
						queue.Enqueue(() =>
						{
							var complexResult = controlComplexSocket.Receive(this.Encoding);
							var success = complexResult.Success;
							return success;
						});
						queue.Enqueue(() => controlComplexSocket.Authenticate(this.Username, this.Password, this.Encoding));
					}

					{
						var success = ExecuteQueue(queue);
						if (!success)
						{
							controlComplexSocket.IsFailed = true;
						}
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

				var complexFtpCommand = new ComplexFtpCommand(this._controlComplexSocket, this.Encoding)
				{
					Command = "FEAT"
				};
				var success = complexFtpCommand.Send();
				if (!success)
				{
					return false;
				}

				var complexResult = this._controlComplexSocket.Receive(this.Encoding);
				success = complexResult.Success;
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

				var complexFtpCommand = new ComplexFtpCommand(this._controlComplexSocket, this.Encoding)
				{
					Command = "PASV"
				};
				{
					var success = complexFtpCommand.Send();
					if (!success)
					{
						return null;
					}
				}

				var complexResult = this._controlComplexSocket.Receive(this.Encoding);
				{
					var success = complexResult.Success;
					if (!success)
					{
						return null;
					}
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

				var transferComplexSocket = GetTransferComplexSocket(ipAddress, port);
				return transferComplexSocket;
			}
		}
	}
}
