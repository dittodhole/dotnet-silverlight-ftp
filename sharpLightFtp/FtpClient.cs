using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using sharpLightFtp.EventArgs;
using sharpLightFtp.Extensions;
using sharpLightFtp.IO;

namespace sharpLightFtp
{
    // TODO readd logging
    public sealed class FtpClient : IDisposable
    {
        private readonly AsyncLock _mutex = new AsyncLock();
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
            this.WaitBeforeReceiveTimeSpan = TimeSpan.Zero;
            this.SocketClientAccessPolicyProtocol = SocketClientAccessPolicyProtocol.Http;
        }

        public Encoding Encoding { get; set; }
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
            using (this._mutex.Lock())
            {
                var controlComplexSocket = this._controlComplexSocket;
                if (controlComplexSocket == null)
                {
                    return;
                }

                controlComplexSocket.Dispose();
            }
        }

        public IEnumerable<FtpListItem> GetListing(string path)
        {
            return this.GetListing(path,
                                   CancellationToken.None);
        }

        public IEnumerable<FtpListItem> GetListing(string path,
                                                   CancellationToken cancellationToken)
        {
            return this.GetListingAsync(path,
                                        cancellationToken)
                       .Result;
        }

        public async Task<IEnumerable<FtpListItem>> GetListingAsync(string path)
        {
            return await this.GetListingAsync(path,
                                              CancellationToken.None);
        }

        public async Task<IEnumerable<FtpListItem>> GetListingAsync(string path,
                                                                    CancellationToken cancellationToken)
        {
            IEnumerable<string> rawListing;
            FtpListType ftpListType;

            using (await this._mutex.LockAsync(cancellationToken))
            {
                var controlComplexSocket = await this.EnsureConnectionAndAuthenticationAsync(cancellationToken);
                if (controlComplexSocket == null)
                {
                    return Enumerable.Empty<FtpListItem>();
                }

                string command;
                var ftpFeatures = await this.GetFtpFeaturesAsync(cancellationToken);
                if (ftpFeatures.HasFlag(FtpFeatures.MLSD))
                {
                    ftpListType = FtpListType.MLSD;
                    command = "MLSD";
                }
                else if (ftpFeatures.HasFlag(FtpFeatures.MLST))
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
                    var success = await this.GotoParentDirectoryAsync(controlComplexSocket,
                                                                      ftpDirectory,
                                                                      cancellationToken);
                    if (!success)
                    {
                        return Enumerable.Empty<FtpListItem>();
                    }
                }

                if (!String.IsNullOrEmpty(ftpDirectory.DirectoryName))
                {
                    var success = await this.ChangeWorkingDirectoryAsync(controlComplexSocket,
                                                                         ftpDirectory.DirectoryName,
                                                                         cancellationToken);
                    if (!success)
                    {
                        return Enumerable.Empty<FtpListItem>();
                    }
                }

                {
                    // sending PASV
                    // reading PASV
                    var transferComplexSocket = await this.GetPassiveComplexSocketAsync(controlComplexSocket,
                                                                                        cancellationToken);
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
                        ftpReply = await this.ExecuteWithoutMutexAsync(controlComplexSocket,
                                                                       cancellationToken,
                                                                       transferComplexSocket.ConnectAsync,
                                                                       command);
                        if (!ftpReply.Success)
                        {
                            return Enumerable.Empty<FtpListItem>();
                        }

                        {
                            this.WaitBeforeReceive();

                            // reading transfer

                            var receiveResult = await transferComplexSocket.Socket.ReceiveAsync(this.ChunkReceiveBufferSize,
                                                                                                transferComplexSocket.GetSocketAsyncEventArgs,
                                                                                                this.Encoding,
                                                                                                cancellationToken);
                            if (!receiveResult.Success)
                            {
                                return Enumerable.Empty<FtpListItem>();
                            }

                            rawListing = (receiveResult.Data ?? String.Empty).Split(Environment.NewLine.ToCharArray(),
                                                                                    StringSplitOptions.RemoveEmptyEntries);
                        }
                    }

                    // reading LIST/... (226 Directory send OK)
                    var result = await this.ReceiveAndLogSafeAsync(controlComplexSocket,
                                                                   ftpReply,
                                                                   cancellationToken);
                    if (!result)
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
            return this.CreateDirectory(path,
                                        CancellationToken.None);
        }

        public bool CreateDirectory(string path,
                                    CancellationToken cancellationToken)
        {
            return this.CreateDirectoryAsync(path,
                                             cancellationToken)
                       .Result;
        }

        public async Task<bool> CreateDirectoryAsync(string path)
        {
            return await this.CreateDirectoryAsync(path,
                                                   CancellationToken.None);
        }

        public async Task<bool> CreateDirectoryAsync(string path,
                                                     CancellationToken cancellationToken)
        {
            using (await this._mutex.LockAsync())
            {
                var controlComplexSocket = await this.EnsureConnectionAndAuthenticationAsync(cancellationToken);
                if (controlComplexSocket == null)
                {
                    return false;
                }

                var ftpDirectory = FtpDirectory.Create(path);
                var success = await this.GotoParentDirectoryAsync(controlComplexSocket,
                                                                  ftpDirectory,
                                                                  cancellationToken,
                                                                  true);

                return success;
            }
        }

        public bool Upload(Stream stream,
                           FtpFile ftpFile,
                           bool createDirectoryIfNotExists = true)
        {
            return this.Upload(stream,
                               ftpFile,
                               CancellationToken.None,
                               createDirectoryIfNotExists);
        }

        public bool Upload(Stream stream,
                           FtpFile ftpFile,
                           CancellationToken cancellationToken,
                           bool createDirectoryIfNotExists = true)
        {
            return this.UploadAsync(stream,
                                    ftpFile,
                                    cancellationToken,
                                    createDirectoryIfNotExists)
                       .Result;
        }

        public async Task<bool> UploadAsync(Stream stream,
                                            FtpFile ftpFile,
                                            bool createDirectoryIfNotExists = true)
        {
            return await this.UploadAsync(stream,
                                          ftpFile,
                                          CancellationToken.None,
                                          createDirectoryIfNotExists);
        }

        public async Task<bool> UploadAsync(Stream stream,
                                            FtpFile ftpFile,
                                            CancellationToken cancellationToken,
                                            bool createDirectoryIfNotExists = true)
        {
            using (await this._mutex.LockAsync(cancellationToken))
            {
                var controlComplexSocket = await this.EnsureConnectionAndAuthenticationAsync(cancellationToken);
                if (controlComplexSocket == null)
                {
                    return false;
                }

                {
                    var success = await this.GotoParentDirectoryAsync(controlComplexSocket,
                                                                      ftpFile,
                                                                      cancellationToken,
                                                                      createDirectoryIfNotExists);
                    if (!success)
                    {
                        return false;
                    }
                }

                // sending PASV
                // reading PASV
                var transferComplexSocket = await this.GetPassiveComplexSocketAsync(controlComplexSocket,
                                                                                    cancellationToken);
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
                    ftpReply = await this.ExecuteWithoutMutexAsync(controlComplexSocket,
                                                                   cancellationToken,
                                                                   transferComplexSocket.ConnectAsync,
                                                                   "STOR {0}",
                                                                   ftpFile.FileName);
                    if (!ftpReply.Success)
                    {
                        return false;
                    }

                    {
                        // sending transfer socket
                        var success = await transferComplexSocket.Socket.SendAsync(this.ChunkSendBufferSize,
                                                                                   controlComplexSocket.GetSocketAsyncEventArgs,
                                                                                   stream,
                                                                                   cancellationToken,
                                                                                   (bytesSent,
                                                                                    bytesTotal) =>
                                                                                   {
                                                                                       var uploadProgressEventArgs = new UploadProgressEventArgs(bytesSent,
                                                                                                                                                 bytesTotal);
                                                                                       this.OnUploadProgressAsync(uploadProgressEventArgs);
                                                                                   });
                        if (!success)
                        {
                            return false;
                        }
                    }
                }

                // reading STOR (226 ...)
                var result = await this.ReceiveAndLogSafeAsync(controlComplexSocket,
                                                               ftpReply,
                                                               cancellationToken);
                if (!result)
                {
                    return false;
                }
            }

            return true;
        }

        public bool Download(FtpFile ftpFile,
                             Stream stream)
        {
            return this.Download(ftpFile,
                                 stream,
                                 CancellationToken.None);
        }

        public bool Download(FtpFile ftpFile,
                             Stream stream,
                             CancellationToken cancellationToken)
        {
            return this.DownloadAsync(ftpFile,
                                      stream,
                                      cancellationToken)
                       .Result;
        }

        public async Task<bool> DownloadAsync(FtpFile ftpFile,
                                              Stream stream)
        {
            return await this.DownloadAsync(ftpFile,
                                            stream,
                                            CancellationToken.None);
        }

        public async Task<bool> DownloadAsync(FtpFile ftpFile,
                                              Stream stream,
                                              CancellationToken cancellationToken)
        {
            using (await this._mutex.LockAsync(cancellationToken))
            {
                var controlComplexSocket = await this.EnsureConnectionAndAuthenticationAsync(cancellationToken);
                if (controlComplexSocket == null)
                {
                    return false;
                }

                {
                    var success = await this.GotoParentDirectoryAsync(controlComplexSocket,
                                                                      ftpFile,
                                                                      cancellationToken);
                    if (!success)
                    {
                        return false;
                    }
                }

                long bytesTotal;

                // sending SIZE
                // reading SIZE
                {
                    var ftpReply = await this.ExecuteWithoutMutexAsync(controlComplexSocket,
                                                                       cancellationToken,
                                                                       "SIZE {0}",
                                                                       ftpFile.FileName);
                    if (!ftpReply.Success)
                    {
                        return false;
                    }

                    if (!Int64.TryParse(ftpReply.ResponseMessage,
                                        out bytesTotal))
                    {
                        return false;
                    }
                }

                // sending PASV
                // reading PASV
                var transferComplexSocket = await this.GetPassiveComplexSocketAsync(controlComplexSocket,
                                                                                    cancellationToken);
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
                        ftpReply = await this.ExecuteWithoutMutexAsync(controlComplexSocket,
                                                                       cancellationToken,
                                                                       transferComplexSocket.ConnectAsync,
                                                                       "RETR {0}",
                                                                       ftpFile.FileName);
                        if (!ftpReply.Success)
                        {
                            return false;
                        }

                        {
                            this.WaitBeforeReceive();

                            // reading transfer socket
                            var rawFtpResponse = await transferComplexSocket.Socket.ReceiveAsync(this.ChunkReceiveBufferSize,
                                                                                                 transferComplexSocket.GetSocketAsyncEventArgs,
                                                                                                 cancellationToken,
                                                                                                 bytesTotal,
                                                                                                 bytesReceived =>
                                                                                                 {
                                                                                                     var downloadProgressEventArgs = new DownloadProgressEventArgs(bytesReceived,
                                                                                                                                                                   bytesTotal);
                                                                                                     this.OnDownloadProgressAsync(downloadProgressEventArgs);
                                                                                                 });
                            if (!rawFtpResponse.Success)
                            {
                                return false;
                            }

                            stream.Write(rawFtpResponse.Buffer,
                                         0,
                                         rawFtpResponse.Buffer.Length);
                        }
                    }

                    // reading RETR (226 Transfer complete)
                    var result = await this.ReceiveAndLogSafeAsync(controlComplexSocket,
                                                                   ftpReply,
                                                                   cancellationToken);
                    if (!result)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool Delete(FtpFile ftpFile)
        {
            return this.Delete(ftpFile,
                               CancellationToken.None);
        }

        public bool Delete(FtpFile ftpFile,
                           CancellationToken cancellationToken)
        {
            return this.DeleteAsync(ftpFile,
                                    cancellationToken)
                       .Result;
        }

        public async Task<bool> DeleteAsync(FtpFile ftpFile)
        {
            return await this.DeleteAsync(ftpFile,
                                          CancellationToken.None);
        }

        public async Task<bool> DeleteAsync(FtpFile ftpFile,
                                            CancellationToken cancellationToken)
        {
            using (await this._mutex.LockAsync(cancellationToken))
            {
                var controlComplexSocket = await this.EnsureConnectionAndAuthenticationAsync(cancellationToken);
                if (controlComplexSocket == null)
                {
                    return false;
                }

                var success = await this.GotoParentDirectoryAsync(controlComplexSocket,
                                                                  ftpFile,
                                                                  cancellationToken);
                if (!success)
                {
                    return false;
                }

                var ftpReply = await this.ExecuteWithoutMutexAsync(controlComplexSocket,
                                                                   cancellationToken,
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
            return this.Delete(ftpDirectory,
                               CancellationToken.None);
        }

        public bool Delete(FtpDirectory ftpDirectory,
                           CancellationToken cancellationToken)
        {
            return this.DeleteAsync(ftpDirectory,
                                    cancellationToken)
                       .Result;
        }

        public async Task<bool> DeleteAsync(FtpDirectory ftpDirectory)
        {
            return await this.DeleteAsync(ftpDirectory,
                                          CancellationToken.None);
        }

        public async Task<bool> DeleteAsync(FtpDirectory ftpDirectory,
                                            CancellationToken cancellationToken)
        {
            using (await this._mutex.LockAsync(cancellationToken))
            {
                var controlComplexSocket = await this.EnsureConnectionAndAuthenticationAsync(cancellationToken);
                if (controlComplexSocket == null)
                {
                    return false;
                }

                var success = await this.GotoParentDirectoryAsync(controlComplexSocket,
                                                                  ftpDirectory,
                                                                  cancellationToken);
                if (!success)
                {
                    return false;
                }

                var ftpReply = await this.ExecuteWithoutMutexAsync(controlComplexSocket,
                                                                   cancellationToken,
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

        private async Task<ComplexSocket> GetPassiveComplexSocketAsync(ComplexSocket controlComplexSocket,
                                                                       CancellationToken cancellationToken)
        {
            var ftpReply = await this.ExecuteWithoutMutexAsync(controlComplexSocket,
                                                               cancellationToken,
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

            var transferComplexSocket = ComplexSocket.CreateForTransfer(this,
                                                                        ipEndPoint);

            return transferComplexSocket;
        }

        public async Task<FtpReply> ExecuteAsync(string command,
                                                 params object[] args)
        {
            return await this.ExecuteAsync(CancellationToken.None,
                                           command,
                                           args);
        }

        public async Task<FtpReply> ExecuteAsync(CancellationToken cancellationToken,
                                                 string command,
                                                 params object[] args)
        {
            using (await this._mutex.LockAsync())
            {
                var controlComplexSocket = await this.EnsureConnectionAndAuthenticationAsync(cancellationToken);
                if (controlComplexSocket == null)
                {
                    return FtpReply.Failed;
                }

                var ftpReply = await this.ExecuteWithoutMutexAsync(controlComplexSocket,
                                                                   cancellationToken,
                                                                   command,
                                                                   args);
                return ftpReply;
            }
        }

        #region ensuring connection and authentication

        private async Task<ComplexSocket> EnsureConnectionAndAuthenticationAsync(CancellationToken cancellationToken)
        {
            var controlComplexSocket = this._controlComplexSocket ?? ComplexSocket.CreateForControl(this);
            if (!controlComplexSocket.Connected)
            {
                this._authenticated = false;

                var success = await controlComplexSocket.ConnectAsync(cancellationToken);
                if (!success)
                {
                    controlComplexSocket = null;
                }
                else
                {
                    var ftpReply = await this.ReceiveAndLogAsync(controlComplexSocket,
                                                                 cancellationToken);
                    if (!ftpReply.Success)
                    {
                        controlComplexSocket = null;
                    }
                }
            }
            if (controlComplexSocket != null)
            {
                if (this._authenticated)
                {
                    return controlComplexSocket;
                }

                var ftpReply = await this.ExecuteWithoutMutexAsync(controlComplexSocket,
                                                                   cancellationToken,
                                                                   "USER {0}",
                                                                   this.Username);
                if (ftpReply.FtpResponseType == FtpResponseType.PositiveIntermediate)
                {
                    ftpReply = await this.ExecuteWithoutMutexAsync(controlComplexSocket,
                                                                   cancellationToken,
                                                                   "PASS {0}",
                                                                   this.Password);
                }

                this._authenticated = ftpReply.Success;
                if (!this._authenticated)
                {
                    controlComplexSocket = null;
                }
            }

            this._controlComplexSocket = controlComplexSocket;

            return controlComplexSocket;
        }

        #endregion

        #region actions

        private async Task<FtpReply> ExecuteWithoutMutexAsync(ComplexSocket controlComplexSocket,
                                                              CancellationToken cancellationToken,
                                                              string command,
                                                              params object[] args)
        {
            // TODO mutex in names should DIE!
            return await this.ExecuteWithoutMutexAsync(controlComplexSocket,
                                                       cancellationToken,
                                                       null,
                                                       command,
                                                       args);
        }

        private async Task<FtpReply> ExecuteWithoutMutexAsync(ComplexSocket controlComplexSocket,
                                                              CancellationToken cancellationToken,
                                                              Func<CancellationToken, Task<bool>> interimPredicate,
                                                              string command,
                                                              params object[] args)
        {
            // TODO mutex in names should DIE!
            {
                var success = await this.SendAndLogAsync(controlComplexSocket,
                                                         cancellationToken,
                                                         command,
                                                         args);
                if (!success)
                {
                    return FtpReply.Failed;
                }
            }
            if (interimPredicate != null)
            {
                var success = await interimPredicate.Invoke(cancellationToken);
                if (!success)
                {
                    return FtpReply.Failed;
                }
            }
            {
                var ftpReply = await this.ReceiveAndLogAsync(controlComplexSocket,
                                                             cancellationToken);

                return ftpReply;
            }
        }

        private async Task<bool> GotoParentDirectoryAsync(ComplexSocket controlComplexSocket,
                                                          FtpFileSystemObject ftpFileSystemObject,
                                                          CancellationToken cancellationToken,
                                                          bool createDirectoryIfNotExists = false)
        {
            var ftpDirectory = ftpFileSystemObject.GetParentFtpDirectory();
            var directoryChanges = FtpClientHelper.DirectoryChanges(this._currentFtpDirectory,
                                                                    ftpDirectory);

            foreach (var directoryChange in directoryChanges)
            {
                if (String.Equals(directoryChange,
                                  FtpFileSystemObject.ParentChangeCommand))
                {
                    var ftpReply = await this.ExecuteWithoutMutexAsync(controlComplexSocket,
                                                                       cancellationToken,
                                                                       "CDUP");
                    if (ftpReply.Success)
                    {
                        this._currentFtpDirectory = this._currentFtpDirectory.GetParentFtpDirectory();
                    }
                }
                else
                {
                    var success = await this.ChangeWorkingDirectoryAsync(controlComplexSocket,
                                                                         directoryChange,
                                                                         cancellationToken,
                                                                         createDirectoryIfNotExists);
                    if (!success)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private async Task<bool> ChangeWorkingDirectoryAsync(ComplexSocket controlComplexSocket,
                                                             string directory,
                                                             CancellationToken cancellationToken,
                                                             bool createDirectoryIfNotExists = false)
        {
            var ftpReply = await this.ExecuteWithoutMutexAsync(controlComplexSocket,
                                                               cancellationToken,
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
                    ftpReply = await this.ExecuteWithoutMutexAsync(controlComplexSocket,
                                                                   cancellationToken,
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

        private async Task<FtpFeatures> GetFtpFeaturesAsync(CancellationToken cancellationToken)
        {
            var ftpReply = await this.ExecuteWithoutMutexAsync(this._controlComplexSocket,
                                                               cancellationToken,
                                                               "FEAT");
            if (!ftpReply.Success)
            {
                return FtpFeatures.Unknown;
            }

            var messages = ftpReply.Messages;
            var ftpFeatures = FtpClientHelper.ParseFtpFeatures(messages);

            return ftpFeatures;
        }

        #endregion

        #region events

        public event EventHandler<SocketRequestEventArg> SocketRequest;
        public event EventHandler<SocketResponseEventArg> SocketResponse;
        public event EventHandler<UploadProgressEventArgs> UploadProgress;
        public event EventHandler<DownloadProgressEventArgs> DownloadProgress;

        private void OnSocketRequestAsync(SocketRequestEventArg e)
        {
            var handler = this.SocketRequest;
            if (handler != null)
            {
                handler.BeginInvoke(this,
                                    e,
                                    handler.EndInvoke,
                                    null);
            }
        }

        private void OnSocketResponseAsync(SocketResponseEventArg e)
        {
            var handler = this.SocketResponse;
            if (handler != null)
            {
                handler.BeginInvoke(this,
                                    e,
                                    handler.EndInvoke,
                                    null);
            }
        }

        private void OnUploadProgressAsync(UploadProgressEventArgs e)
        {
            var handler = this.UploadProgress;
            if (handler != null)
            {
                handler.BeginInvoke(this,
                                    e,
                                    handler.EndInvoke,
                                    null);
            }
        }

        private void OnDownloadProgressAsync(DownloadProgressEventArgs e)
        {
            var handler = this.DownloadProgress;
            if (handler != null)
            {
                handler.BeginInvoke(this,
                                    e,
                                    handler.EndInvoke,
                                    null);
            }
        }

        #endregion

        #region communcation helpers

        /// <remarks>This code does sending specifically for the <paramref name="controlComplexSocket" /> and does some logging</remarks>
        private async Task<bool> SendAndLogAsync(ComplexSocket controlComplexSocket,
                                                 CancellationToken cancellationToken,
                                                 string command,
                                                 params object[] args)
        {
            command = String.Format(command,
                                    args);
            if (!command.EndsWith(Environment.NewLine))
            {
                command = String.Concat(command,
                                        Environment.NewLine);
            }

            {
                var socketRequestEventArg = new SocketRequestEventArg(command);
                this.OnSocketRequestAsync(socketRequestEventArg);
            }

            {
                var buffer = this.Encoding.GetBytes(command);
                using (var memoryStream = new MemoryStream(buffer))
                {
                    var success = await controlComplexSocket.Socket.SendAsync(this.ChunkSendBufferSize,
                                                                              controlComplexSocket.GetSocketAsyncEventArgs,
                                                                              memoryStream,
                                                                              cancellationToken);
                    return success;
                }
            }
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

        /// <remarks>Sometimes <paramref name="ftpReply" /> has 2 lines or more with different <type name="FtpResponseType" />
        /// </remarks>
        private async Task<bool> ReceiveAndLogSafeAsync(ComplexSocket controlComplexSocket,
                                                        FtpReply ftpReply,
                                                        CancellationToken cancellationToken)
        {
            if (ftpReply.Completed)
            {
                return true;
            }

            ftpReply = await this.ReceiveAndLogAsync(controlComplexSocket,
                                                     cancellationToken);
            var success = ftpReply.Success;
            if (!success)
            {
                return false;
            }

            return true;
        }

        /// <remarks>This code does receiving specifically for the <paramref name="controlComplexSocket" /> and does some logging</remarks>
        private async Task<FtpReply> ReceiveAndLogAsync(ComplexSocket controlComplexSocket,
                                                        CancellationToken cancellationToken)
        {
            this.WaitBeforeReceive();

            var ftpReply = await controlComplexSocket.Socket.GetFtpReplyAsync(this.ChunkReceiveBufferSize,
                                                                              controlComplexSocket.GetSocketAsyncEventArgs,
                                                                              this.Encoding,
                                                                              cancellationToken);

            {
                var socketResponseEventArg = new SocketResponseEventArg(ftpReply.Data);
                this.OnSocketResponseAsync(socketResponseEventArg);
            }

            return ftpReply;
        }

        #endregion
    }
}
