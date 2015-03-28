using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using sharpLightFtp.Extensions;

namespace sharpLightFtp
{
    internal sealed class ComplexSocket : IDisposable
    {
        private readonly EndPoint _endPoint;
        private readonly bool _isControlSocket;
        private readonly Socket _socket;
        private readonly SocketClientAccessPolicyProtocol _socketClientAccessPolicyProtocol;

        private ComplexSocket(EndPoint endPoint,
                              bool isControlSocket,
                              int receiveBufferSize,
                              int sendBufferSize,
                              SocketClientAccessPolicyProtocol socketClientAccessPolicyProtocol)
        {
            this._endPoint = endPoint;
            this._isControlSocket = isControlSocket;
            this._socketClientAccessPolicyProtocol = socketClientAccessPolicyProtocol;
            this._socket = new Socket(AddressFamily.InterNetwork,
                                      SocketType.Stream,
                                      ProtocolType.Tcp)
            {
                ReceiveBufferSize = receiveBufferSize,
                SendBufferSize = sendBufferSize
            };
        }

        public EndPoint EndPoint
        {
            get
            {
                return this._endPoint;
            }
        }

        public Socket Socket
        {
            get
            {
                return this._socket;
            }
        }

        public bool IsControlSocket
        {
            get
            {
                return this._isControlSocket;
            }
        }

        public bool Connected
        {
            get
            {
                return this._socket.Connected;
            }
        }

        public SocketClientAccessPolicyProtocol SocketClientAccessPolicyProtocol
        {
            get
            {
                return this._socketClientAccessPolicyProtocol;
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            this._socket.Dispose();
        }

        #endregion

        public async Task<bool> ConnectAsync(CancellationToken cancellationToken)
        {
            using (var socketAsyncEventArgs = this.GetSocketAsyncEventArgs())
            {
                await this._socket.ExecuteAsync(arg => arg.ConnectAsync,
                                                socketAsyncEventArgs,
                                                cancellationToken);
                var success = socketAsyncEventArgs.GetSuccess();

                return success;
            }
        }

        public SocketAsyncEventArgs GetSocketAsyncEventArgs()
        {
            var socketAsyncEventArgs = new SocketAsyncEventArgs
            {
                RemoteEndPoint = this.EndPoint,
                SocketClientAccessPolicyProtocol = this.SocketClientAccessPolicyProtocol
            };

            return socketAsyncEventArgs;
        }

        public static ComplexSocket CreateForTransfer(FtpClient ftpClient,
                                                      IPEndPoint ipEndPoint)
        {
            // TODO this method should be moved to a factory
            // TODO add check for ftpClient.Port 0 - 0xffff

            var complexSocket = new ComplexSocket(ipEndPoint,
                                                  false,
                                                  ftpClient.SocketReceiveBufferSize,
                                                  ftpClient.SocketSendBufferSize,
                                                  ftpClient.SocketClientAccessPolicyProtocol);

            return complexSocket;
        }

        public static ComplexSocket CreateForControl(FtpClient ftpClient)
        {
            // TODO this method should be moved to a factory
            // TODO add check for ftpClient.Port 0 - 0xffff

            var endPoint = new DnsEndPoint(ftpClient.Server,
                                           ftpClient.Port);

            var complexSocket = new ComplexSocket(endPoint,
                                                  true,
                                                  ftpClient.SocketReceiveBufferSize,
                                                  ftpClient.SocketSendBufferSize,
                                                  ftpClient.SocketClientAccessPolicyProtocol);

            return complexSocket;
        }
    }
}
