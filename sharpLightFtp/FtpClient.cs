using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using sharpLightFtp.EventArgs;
using sharpLightFtp.Extensions;

namespace sharpLightFtp
{
	// TODO readd logging
	public sealed class FtpClient : IDisposable
	{
		private readonly Lazy<FtpFeatures> _ftpFeatures;
		private readonly object _lockControlComplexSocket = new object();

		private bool _authenticated;
		private ComplexSocket _controlComplexSocket;
		private FtpDirectory _currentFtpDirectory = FtpDirectory.Root; // TODO we could also determine if PWD is allowed or if we need to persist the current directory TODO: how to check for manual CWD commands

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
			var uriBuilder = new UriBuilder(uri);

			this.Username = uriBuilder.UserName;
			this.Password = uriBuilder.Password;
			this.Server = uriBuilder.Host;
			this.Port = uriBuilder.Port;
		}

		public FtpClient()
		{
			this.SocketReceiveBufferSize = 1 << 13; // 8192
			this.SocketSendBufferSize = 1 << 13; // 8192
			this.ChunkReceiveBufferSize = 1400;
			this.ChunkSendBufferSize = 1400;
			this.Encoding = Encoding.UTF8;
			this.ConnectTimeout = TimeSpan.FromSeconds(30);
			this.ReceiveTimeout = TimeSpan.FromSeconds(30);
			this.SendTimeout = TimeSpan.FromMinutes(5);
			this.WaitBeforeReceiveTimeSpan = TimeSpan.Zero;
			this.SocketClientAccessPolicyProtocol = SocketClientAccessPolicyProtocol.Http;

			this._ftpFeatures = new Lazy<FtpFeatures>(() =>
			{
				var ftpReply = this.Execute(this._controlComplexSocket,
				                            "FEAT");
				if (!ftpReply.Success)
				{
					return FtpFeatures.Unknown;
				}

				var messages = ftpReply.Messages;
				var ftpFeatures = FtpClientHelper.ParseFtpFeatures(messages);

				return ftpFeatures;
			});
		}

		public FtpFeatures FtpFeatures
		{
			get
			{
				return this._ftpFeatures.Value;
			}
		}

		public Encoding Encoding { get; set; }
		public TimeSpan ConnectTimeout { get; set; }
		public TimeSpan ReceiveTimeout { get; set; }
		public TimeSpan SendTimeout { get; set; }
		public string Server { get; set; }
		public string Username { get; set; }
		public string Password { get; set; }
		public SocketClientAccessPolicyProtocol SocketClientAccessPolicyProtocol { get; set; }
		public int Port { get; set; }
		public int SocketSendBufferSize { get; set; }
		public int SocketReceiveBufferSize { get; set; }
		public int ChunkSendBufferSize { get; set; }
		public int ChunkReceiveBufferSize { get; set; }
		public TimeSpan WaitBeforeReceiveTimeSpan { get; set; }

		public void Dispose()
		{
			lock (this._lockControlComplexSocket)
			{
				var controlComplexSocket = this._controlComplexSocket;
				if (controlComplexSocket == null)
				{
					return;
				}

				controlComplexSocket.Dispose();
			}
		}

		public event EventHandler<SocketRequestEventArg> SocketRequest;
		public event EventHandler<SocketResponseEventArg> SocketResponse;
		public event EventHandler<UploadProgressEventArgs> UploadProgress;
		public event EventHandler<DownloadProgressEventArgs> DownloadProgress;

		public IEnumerable<FtpListItem> GetListing(string path)
		{
			IEnumerable<string> rawListing;
			FtpListType ftpListType;

			lock (this._lockControlComplexSocket)
			{
				var controlComplexSocket = this.EnsureConnectionAndAuthenticationWithoutLock();
				if (controlComplexSocket == null)
				{
					return Enumerable.Empty<FtpListItem>();
				}

				string command;
				if (this.FtpFeatures.HasFlag(FtpFeatures.MLSD))
				{
					ftpListType = FtpListType.MLSD;
					command = "MLSD";
				}
				else if (this.FtpFeatures.HasFlag(FtpFeatures.MLST))
				{
					ftpListType = FtpListType.MLST;
					command = "MLST";
				}
				else
				{
					ftpListType = FtpListType.LIST;
					command = "LIST";
				}

				var ftpDirectory = FtpDirectory.Create(path);

				{
					var success = this.GotoParentDirectory(controlComplexSocket,
					                                       ftpDirectory);
					if (!success)
					{
						return Enumerable.Empty<FtpListItem>();
					}
				}

				{
					var success = this.ChangeWorkingDirectory(controlComplexSocket,
					                                          ftpDirectory.DirectoryName);
					if (!success)
					{
						return Enumerable.Empty<FtpListItem>();
					}
				}

				{
					// sending PASV
					// reading PASV
					var transferComplexSocket = this.GetPassiveComplexSocket(controlComplexSocket);
					if (transferComplexSocket == null)
					{
						return Enumerable.Empty<FtpListItem>();
					}

					FtpReply ftpReply;

					using (transferComplexSocket)
					{
						// sending LIST/...
						// open PASV
						// reading LIST/... (150 Here comes the directory listing)
						ftpReply = this.Execute(controlComplexSocket,
						                        () => transferComplexSocket.Connect(this.ConnectTimeout),
						                        command);
						if (!ftpReply.Success)
						{
							return Enumerable.Empty<FtpListItem>();
						}

						{
							this.WaitBeforeReceive();

							// reading transfer

							string data;
							var success = transferComplexSocket.Socket.ReceiveIntoString(this.ChunkReceiveBufferSize,
							                                                             () => transferComplexSocket.GetSocketAsyncEventArgsWithUserToken(this.ReceiveTimeout),
							                                                             this.Encoding,
							                                                             out data);
							if (!success)
							{
								return Enumerable.Empty<FtpListItem>();
							}

							rawListing = data.Split(Environment.NewLine.ToCharArray(),
							                        StringSplitOptions.RemoveEmptyEntries);
						}
					}

					// reading LIST/... (226 Directory send OK)
					if (!this.AlreadyCompletedOrFinalFtpReplySuccess(controlComplexSocket,
					                                                 ftpReply))
					{
						return Enumerable.Empty<FtpListItem>();
					}
				}
			}

			var ftpListItems = FtpListItem.ParseList(rawListing,
			                                         ftpListType);

			return ftpListItems;
		}

		public bool CreateDirectory(string path)
		{
			lock (this._lockControlComplexSocket)
			{
				var controlComplexSocket = this.EnsureConnectionAndAuthenticationWithoutLock();
				if (controlComplexSocket == null)
				{
					return false;
				}

				var ftpDirectory = FtpDirectory.Create(path);
				var success = this.GotoParentDirectory(controlComplexSocket,
				                                       ftpDirectory,
				                                       true);

				return success;
			}
		}

		public bool Upload(Stream stream,
		                   FtpFile ftpFile,
		                   bool createDirectoryIfNotExists = true)
		{
			lock (this._lockControlComplexSocket)
			{
				var controlComplexSocket = this.EnsureConnectionAndAuthenticationWithoutLock();
				if (controlComplexSocket == null)
				{
					return false;
				}

				{
					var success = this.GotoParentDirectory(controlComplexSocket,
					                                       ftpFile,
					                                       createDirectoryIfNotExists);
					if (!success)
					{
						return false;
					}
				}

				// sending PASV
				// reading PASV
				var transferComplexSocket = this.GetPassiveComplexSocket(controlComplexSocket);
				if (transferComplexSocket == null)
				{
					return false;
				}

				FtpReply ftpReply;
				using (transferComplexSocket)
				{
					// sending STOR
					// open transfer socket
					// reading STOR (150 ...)
					ftpReply = this.Execute(controlComplexSocket,
					                        () => transferComplexSocket.Connect(this.ConnectTimeout),
					                        "STOR {0}",
					                        ftpFile.FileName);
					if (!ftpReply.Success)
					{
						return false;
					}

					{
						// sending transfer socket
						var success = transferComplexSocket.Socket.Send(this.ChunkSendBufferSize,
						                                                () => controlComplexSocket.GetSocketAsyncEventArgsWithUserToken(this.SendTimeout),
						                                                stream,
						                                                (bytesSent,
						                                                 bytesTotal) =>
						                                                {
							                                                var uploadProgressEventArgs = new UploadProgressEventArgs(bytesSent,
							                                                                                                          bytesTotal);
							                                                this.OnUploadProgress(uploadProgressEventArgs);
						                                                });
						if (!success)
						{
							return false;
						}
					}
				}

				// reading STOR (226 ...)
				if (!this.AlreadyCompletedOrFinalFtpReplySuccess(controlComplexSocket,
				                                                 ftpReply))
				{
					return false;
				}
			}

			return true;
		}

		public bool Download(FtpFile ftpFile,
		                     Stream stream)
		{
			lock (this._lockControlComplexSocket)
			{
				var controlComplexSocket = this.EnsureConnectionAndAuthenticationWithoutLock();
				if (controlComplexSocket == null)
				{
					return false;
				}

				{
					var success = this.GotoParentDirectory(controlComplexSocket,
					                                       ftpFile);
					if (!success)
					{
						return false;
					}
				}

				long bytesTotal;

				// sending SIZE
				// reading SIZE
				{
					var ftpReply = this.Execute(controlComplexSocket,
					                            "SIZE {0}",
					                            ftpFile.FileName);
					if (!ftpReply.Success)
					{
						return false;
					}

					if (!long.TryParse(ftpReply.ResponseMessage,
					                   out bytesTotal))
					{
						return false;
					}
				}

				// sending PASV
				// reading PASV
				var transferComplexSocket = this.GetPassiveComplexSocket(controlComplexSocket);
				if (transferComplexSocket == null)
				{
					return false;
				}

				{
					FtpReply ftpReply;

					using (transferComplexSocket)
					{
						// sending RETR
						// open transfer socket
						// reading RETR (150 Opening BINARY mode data connection...)
						ftpReply = this.Execute(controlComplexSocket,
						                        () => transferComplexSocket.Connect(this.ConnectTimeout),
						                        "RETR {0}",
						                        ftpFile.FileName);
						if (!ftpReply.Success)
						{
							return false;
						}

						{
							this.WaitBeforeReceive();

							// reading transfer socket
							var success = transferComplexSocket.Socket.ReceiveIntoStream(this.ChunkReceiveBufferSize,
							                                                             () => transferComplexSocket.GetSocketAsyncEventArgsWithUserToken(this.ReceiveTimeout),
							                                                             stream,
							                                                             (bytesReceived) =>
							                                                             {
								                                                             var downloadProgressEventArgs = new DownloadProgressEventArgs(bytesReceived,
								                                                                                                                           bytesTotal);
								                                                             this.OnDownloadProgress(downloadProgressEventArgs);
							                                                             });
							if (!success)
							{
								return false;
							}
						}
					}

					// reading RETR (226 Transfer complete)
					if (!this.AlreadyCompletedOrFinalFtpReplySuccess(controlComplexSocket,
					                                                 ftpReply))
					{
						return false;
					}
				}
			}

			return true;
		}

		public bool Delete(FtpFile ftpFile)
		{
			lock (this._lockControlComplexSocket)
			{
				var controlComplexSocket = this.EnsureConnectionAndAuthenticationWithoutLock();
				if (controlComplexSocket == null)
				{
					return false;
				}

				var success = this.GotoParentDirectory(controlComplexSocket,
				                                       ftpFile);
				if (!success)
				{
					return false;
				}

				var ftpReply = this.Execute(controlComplexSocket,
				                            "DELE {0}",
				                            ftpFile.FileName);
				if (!ftpReply.Success)
				{
					return false;
				}
			}

			return true;
		}

		public bool Delete(FtpDirectory ftpDirectory)
		{
			lock (this._lockControlComplexSocket)
			{
				var controlComplexSocket = this.EnsureConnectionAndAuthenticationWithoutLock();
				if (controlComplexSocket == null)
				{
					return false;
				}

				var success = this.GotoParentDirectory(controlComplexSocket,
				                                       ftpDirectory);
				if (!success)
				{
					return false;
				}

				var ftpReply = this.Execute(controlComplexSocket,
				                            "RMD {0}",
				                            ftpDirectory.DirectoryName);
				if (!ftpReply.Success)
				{
					return false;
				}
			}

			return true;
		}

		public FtpDirectory GetCurrentFtpDirectory()
		{
			return this._currentFtpDirectory;
		}

		private ComplexSocket GetPassiveComplexSocket(ComplexSocket controlComplexSocket)
		{
			var ftpReply = this.Execute(controlComplexSocket,
			                            "PASV");
			if (!ftpReply.Success)
			{
				return null;
			}

			var ipEndPoint = FtpClientHelper.ParseIPEndPoint(ftpReply);
			if (ipEndPoint == null)
			{
				return null;
			}

			var transferComplexSocket = this.CreateTransferComplexSocket(ipEndPoint);

			return transferComplexSocket;
		}

		public FtpReply Execute(string command,
		                        params object[] args)
		{
			lock (this._lockControlComplexSocket)
			{
				var controlComplexSocket = this.EnsureConnectionAndAuthenticationWithoutLock();
				if (controlComplexSocket == null)
				{
					return FtpReply.FailedFtpReply;
				}

				var ftpReply = this.Execute(controlComplexSocket,
				                            command,
				                            args);
				return ftpReply;
			}
		}

		private FtpReply Execute(ComplexSocket controlComplexSocket,
		                         string command,
		                         params object[] args)
		{
			return this.Execute(controlComplexSocket,
			                    null,
			                    command,
			                    args);
		}

		private FtpReply Execute(ComplexSocket controlComplexSocket,
		                         Func<bool> interimPredicate,
		                         string command,
		                         params object[] args)
		{
			{
				var success = this.WrappedControlSocketSend(controlComplexSocket,
				                                            command,
				                                            args);
				if (!success)
				{
					return FtpReply.FailedFtpReply;
				}
			}
			if (interimPredicate != null)
			{
				var success = interimPredicate.Invoke();
				if (!success)
				{
					return FtpReply.FailedFtpReply;
				}
			}
			{
				var ftpReply = this.WrappedControlSocketReceive(controlComplexSocket);

				return ftpReply;
			}
		}

		private bool GotoParentDirectory(ComplexSocket controlComplexSocket,
		                                 FtpFileSystemObject ftpFileSystemObject,
		                                 bool createDirectoryIfNotExists = false)
		{
			var ftpDirectory = ftpFileSystemObject.GetParentFtpDirectory();
			var directoryChanges = FtpClientHelper.DirectoryChanges(this._currentFtpDirectory,
			                                                        ftpDirectory);

			foreach (var directoryChange in directoryChanges)
			{
				if (string.Equals(directoryChange,
				                  FtpFileSystemObject.ParentChangeCommand))
				{
					var ftpReply = this.Execute(controlComplexSocket,
					                            "CDUP");
					if (ftpReply.Success)
					{
						this._currentFtpDirectory = this._currentFtpDirectory.GetParentFtpDirectory();
					}
				}
				else
				{
					var success = this.ChangeWorkingDirectory(controlComplexSocket,
					                                          directoryChange,
					                                          createDirectoryIfNotExists);
					if (!success)
					{
						return false;
					}
				}
			}
			return true;
		}

		private bool ChangeWorkingDirectory(ComplexSocket controlComplexSocket,
		                                    string directory,
		                                    bool createDirectoryIfNotExists = false)
		{
			var ftpReply = this.Execute(controlComplexSocket,
			                            "CWD {0}",
			                            directory);
			switch (ftpReply.FtpResponseType)
			{
				case FtpResponseType.PermanentNegativeCompletion:
					// TODO some parsing of the actual FtpReply.ResponseCode should be done in here. i assume 5xx-state means "directory does not exist" all the time, which might be wrong
					if (!createDirectoryIfNotExists)
					{
						return false;
					}
					ftpReply = this.Execute(controlComplexSocket,
					                        "MKD {0}",
					                        directory);
					var success = ftpReply.Success;
					if (!success)
					{
						return false;
					}
					goto case FtpResponseType.PositiveCompletion;
				case FtpResponseType.PositiveCompletion:
					this._currentFtpDirectory = FtpDirectory.Create(this._currentFtpDirectory,
					                                                directory);
					break;
				default:
					return false;
			}
			return true;
		}

		private void OnSocketRequest(SocketRequestEventArg e)
		{
			var handler = this.SocketRequest;
			if (handler != null)
			{
				handler.Invoke(this,
				               e);
			}
		}

		private void OnSocketResponse(SocketResponseEventArg e)
		{
			var handler = this.SocketResponse;
			if (handler != null)
			{
				handler.Invoke(this,
				               e);
			}
		}

		private void OnUploadProgress(UploadProgressEventArgs e)
		{
			var handler = this.UploadProgress;
			if (handler != null)
			{
				handler.Invoke(this,
				               e);
			}
		}

		private void OnDownloadProgress(DownloadProgressEventArgs e)
		{
			var handler = this.DownloadProgress;
			if (handler != null)
			{
				handler.Invoke(this,
				               e);
			}
		}

		#region ensuring connection and authentication

		private ComplexSocket EnsureConnectionAndAuthenticationWithoutLock()
		{
			{
				var success = this.EnsureConnectionWithoutLock();
				if (!success)
				{
					return null;
				}
			}
			{
				var success = this.EnsureAuthenticationWithoutLock();
				if (!success)
				{
					return null;
				}
			}

			return this._controlComplexSocket;
		}

		private bool EnsureConnectionWithoutLock()
		{
			var controlComplexSocket = this._controlComplexSocket ?? this.CreateControlComplexSocket();
			if (!controlComplexSocket.Connected)
			{
				this._authenticated = false;

				var connected = controlComplexSocket.Connect(this.ConnectTimeout);
				if (!connected)
				{
					controlComplexSocket = null;
				}
				else
				{
					var ftpReply = this.WrappedControlSocketReceive(controlComplexSocket);
					if (!ftpReply.Success)
					{
						controlComplexSocket = null;
					}
				}
			}

			this._controlComplexSocket = controlComplexSocket; // TODO remove this assignment in the future

			return this._controlComplexSocket != null;
		}

		private bool EnsureAuthenticationWithoutLock()
		{
			if (this._authenticated)
			{
				return true;
			}

			var ftpReply = this.Execute(this._controlComplexSocket,
			                            "USER {0}",
			                            this.Username);
			if (ftpReply.FtpResponseType == FtpResponseType.PositiveIntermediate)
			{
				ftpReply = this.Execute(this._controlComplexSocket,
				                        "PASS {0}",
				                        this.Password);
			}

			this._authenticated = ftpReply.Success;

			return this._authenticated;
		}

		#endregion

		#region communcation helpers

		/// <remarks>
		///     This code does sending specifically for the <paramref name="controlComplexSocket"/> and does some logging
		/// </remarks>
		private bool WrappedControlSocketSend(ComplexSocket controlComplexSocket,
		                                      string command,
		                                      params object[] args)
		{
			command = string.Format(command,
			                        args);
			if (!command.EndsWith(Environment.NewLine))
			{
				command = string.Concat(command,
				                        Environment.NewLine);
			}

			{
				var socketRequestEventArg = new SocketRequestEventArg(command);
				this.OnSocketRequest(socketRequestEventArg);
			}

			{
				var buffer = this.Encoding.GetBytes(command);
				using (var memoryStream = new MemoryStream(buffer))
				{
					var success = controlComplexSocket.Socket.Send(this.ChunkSendBufferSize,
					                                               () => controlComplexSocket.GetSocketAsyncEventArgsWithUserToken(this.SendTimeout),
					                                               memoryStream);
					return success;
				}
			}
		}

		/// <remarks>
		///     This code does receiving specifically for the <paramref name="controlComplexSocket"/> and does some logging
		/// </remarks>
		private FtpReply WrappedControlSocketReceive(ComplexSocket controlComplexSocket)
		{
			this.WaitBeforeReceive();

			var ftpReply = controlComplexSocket.Socket.Receive(this.ChunkReceiveBufferSize,
			                                                   () => controlComplexSocket.GetSocketAsyncEventArgsWithUserToken(this.ReceiveTimeout),
			                                                   this.Encoding);

			{
				var socketResponseEventArg = new SocketResponseEventArg(ftpReply.Data);
				this.OnSocketResponse(socketResponseEventArg);
			}

			return ftpReply;
		}

		/// <remarks>This is only needed in some specific scenarios - using code should know about - see issue #6</remarks>
		private void WaitBeforeReceive()
		{
			var waitBetweenSendAndReceive = this.WaitBeforeReceiveTimeSpan;
			if (waitBetweenSendAndReceive > TimeSpan.Zero)
			{
				Thread.Sleep(waitBetweenSendAndReceive);
			}
		}

		/// <remarks>
		///     Sometimes <paramref name="ftpReply"/> has 2 lines or more with different <type name="FtpResponseType"/>
		/// </remarks>
		private bool AlreadyCompletedOrFinalFtpReplySuccess(ComplexSocket controlComplexSocket,
		                                                    FtpReply ftpReply)
		{
			if (ftpReply.Completed)
			{
				return true;
			}

			ftpReply = this.WrappedControlSocketReceive(controlComplexSocket);
			var success = ftpReply.Success;
			if (!success)
			{
				return false;
			}

			return true;
		}

		#endregion
	}
}
