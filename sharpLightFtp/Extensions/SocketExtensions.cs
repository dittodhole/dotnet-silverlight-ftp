using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using sharpLightFtp.IO;

namespace sharpLightFtp.Extensions
{
    public static class SocketExtensions
    {
        public static async Task<FtpReply> GetFtpReplyAsync(this Socket socket,
                                                            int bufferSize,
                                                            Func<SocketAsyncEventArgs> createSocketAsyncEventArgsPredicate,
                                                            Encoding encoding,
                                                            CancellationToken cancellationToken)
        {
            var receiveResult = await socket.ReceiveAsync(bufferSize,
                                                          createSocketAsyncEventArgsPredicate,
                                                          encoding,
                                                          cancellationToken);
            if (!receiveResult.Success)
            {
                return FtpReply.Failed;
            }

            var ftpReply = FtpClientHelper.ParseFtpReply(receiveResult.Data);

            return ftpReply;
        }

        public static async Task<FtpResponse> ReceiveAsync(this Socket socket,
                                                           int bufferSize,
                                                           Func<SocketAsyncEventArgs> createSocketAsyncEventArgsPredicate,
                                                           Encoding encoding,
                                                           CancellationToken cancellationToken)
        {
            var rawFtpResponse = await socket.ReceiveAsync(bufferSize,
                                                           createSocketAsyncEventArgsPredicate,
                                                           cancellationToken);
            if (!rawFtpResponse.Success)
            {
                return FtpResponse.Failed;
            }

            var data = encoding.GetString(rawFtpResponse.Buffer,
                                          0,
                                          rawFtpResponse.Buffer.Length);
            var ftpResponse = new FtpResponse(data);

            return ftpResponse;
        }

        internal static async Task<RawFtpResponse> ReceiveAsync(this Socket socket,
                                                                int bufferSize,
                                                                Func<SocketAsyncEventArgs> createSocketAsyncEventArgsPredicate,
                                                                CancellationToken cancellationToken,
                                                                long? bytesTotal = null,
                                                                Action<long> progressPredicate = null)
        {
            var bytesTotallyTransferred = 0;
            int bytesTransferred;

            var result = new byte[0];
            do
            {
                var buffer = new byte[bufferSize];

                using (var socketAsyncEventArgs = createSocketAsyncEventArgsPredicate.Invoke())
                {
                    socketAsyncEventArgs.SetBuffer(buffer,
                                                   0,
                                                   bufferSize);
                    var socketError = await socket.ExecuteAsync(arg => arg.ReceiveAsync,
                                                                socketAsyncEventArgs,
                                                                cancellationToken);
                    if (socketError != SocketError.Success)
                    {
                        return RawFtpResponse.Failed;
                    }

                    bytesTransferred = socketAsyncEventArgs.BytesTransferred;
                }

                var offset = bytesTotallyTransferred;
                bytesTotallyTransferred += bytesTransferred;

                Array.Resize(ref result,
                             bytesTotallyTransferred);
                Array.Copy(buffer,
                           0,
                           result,
                           offset,
                           bytesTransferred);

                if (progressPredicate != null)
                {
                    progressPredicate.Invoke(bytesTotallyTransferred);
                }
            } while (FtpClientHelper.AreThereRemainingBytes(bytesTransferred,
                                                            bufferSize,
                                                            bytesTotallyTransferred,
                                                            bytesTotal));

            var rawFtpResponse = new RawFtpResponse(true,
                                                    result);

            return rawFtpResponse;
        }

        public static async Task<bool> SendAsync(this Socket socket,
                                                 int bufferSize,
                                                 Func<SocketAsyncEventArgs> createSocketAsyncEventArgsPredicate,
                                                 Stream stream,
                                                 CancellationToken cancellationToken,
                                                 Action<long, long> progressPredicate = null)
        {
            stream.Position = 0L;

            var bytesTotal = stream.Length;
            var bytesTotallyTransferred = 0L;

            int bytesTransferred;
            do
            {
                using (var socketAsyncEventArgs = createSocketAsyncEventArgsPredicate.Invoke())
                {
                    var buffer = new byte[bufferSize];
                    bytesTransferred = stream.Read(buffer,
                                                   0,
                                                   buffer.Length);
                    socketAsyncEventArgs.SetBuffer(buffer,
                                                   0,
                                                   bytesTransferred);
                    var socketError = await socket.ExecuteAsync(arg => arg.SendAsync,
                                                                socketAsyncEventArgs,
                                                                cancellationToken);
                    if (socketError != SocketError.Success)
                    {
                        return false;
                    }
                    bytesTotallyTransferred += bytesTransferred;
                    if (progressPredicate != null)
                    {
                        progressPredicate.Invoke(bytesTotallyTransferred,
                                                 bytesTotal);
                    }
                }
            } while (FtpClientHelper.AreThereRemainingBytes(bytesTransferred,
                                                            bufferSize,
                                                            bytesTotallyTransferred,
                                                            bytesTotal));

            return true;
        }

        public static async Task<SocketError> ExecuteAsync(this Socket socket,
                                                           Func<Socket, Func<SocketAsyncEventArgs, bool>> methodPredicate,
                                                           SocketAsyncEventArgs socketAsyncEventArgs,
                                                           CancellationToken cancellationToken)
        {
            var asyncAutoResetEvent = new AsyncAutoResetEvent(false);
            socketAsyncEventArgs.Completed += (sender,
                                               args) =>
            {
                asyncAutoResetEvent.Set();
            };

            var method = methodPredicate.Invoke(socket);
            var async = method.Invoke(socketAsyncEventArgs);
            if (!async)
            {
                asyncAutoResetEvent.Set();
            }

            await asyncAutoResetEvent.WaitAsync(cancellationToken);

            return socketAsyncEventArgs.SocketError;
        }
    }
}
