using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
            byte[] bytes;
            using (var memoryStream = new MemoryStream())
            {
                var success = await socket.ReceiveAsync(bufferSize,
                                                        createSocketAsyncEventArgsPredicate,
                                                        memoryStream,
                                                        cancellationToken);
                if (!success)
                {
                    return FtpResponse.Failed;
                }

                bytes = memoryStream.ToArray();
            }

            var data = encoding.GetString(bytes,
                                          0,
                                          bytes.Length);
            var ftpResponse = new FtpResponse(data);

            return ftpResponse;
        }

        internal static async Task<bool> ReceiveAsync(this Socket socket,
                                                      int bufferSize,
                                                      Func<SocketAsyncEventArgs> createSocketAsyncEventArgsPredicate,
                                                      Stream stream,
                                                      CancellationToken cancellationToken,
                                                      long? bytesTotal = null,
                                                      Action<long> progressPredicate = null)
        {
            var bytesTotallyTransferred = 0L;
            int bytesTransferred;

            do
            {
                byte[] buffer;
                int offset;

                using (var socketAsyncEventArgs = createSocketAsyncEventArgsPredicate.Invoke())
                {
                    buffer = new byte[bufferSize];
                    socketAsyncEventArgs.SetBuffer(buffer,
                                                   0,
                                                   bufferSize);
                    await socket.ExecuteAsync(arg => arg.ReceiveAsync,
                                              socketAsyncEventArgs,
                                              cancellationToken);
                    var success = socketAsyncEventArgs.GetSuccess();
                    if (!success)
                    {
                        return false;
                    }

                    buffer = socketAsyncEventArgs.Buffer;
                    bytesTransferred = socketAsyncEventArgs.BytesTransferred;
                    offset = socketAsyncEventArgs.Offset;
                }

                stream.Write(buffer,
                             offset,
                             bytesTransferred);

                bytesTotallyTransferred += bytesTransferred;

                if (progressPredicate != null)
                {
                    progressPredicate.Invoke(bytesTotallyTransferred);
                }
            } while (FtpClientHelper.AreThereRemainingBytes(bytesTransferred,
                                                            bufferSize,
                                                            bytesTotallyTransferred,
                                                            bytesTotal));

            stream.Position = 0L;

            return true;
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
                    await socket.ExecuteAsync(arg => arg.SendAsync,
                                              socketAsyncEventArgs,
                                              cancellationToken);
                    var success = socketAsyncEventArgs.GetSuccess();
                    if (!success)
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

        public static async Task<SocketAsyncEventArgs> ExecuteAsync(this Socket socket,
                                                                    Func<Socket, Func<SocketAsyncEventArgs, bool>> methodPredicate,
                                                                    SocketAsyncEventArgs socketAsyncEventArgs,
                                                                    CancellationToken cancellationToken)
        {
            var taskCompletionSource = new TaskCompletionSource<SocketAsyncEventArgs>();
            socketAsyncEventArgs.Completed += (sender,
                                               args) =>
            {
                taskCompletionSource.SetResult(args);
            };

            var method = methodPredicate.Invoke(socket);
            var async = method.Invoke(socketAsyncEventArgs);
            if (!async)
            {
                taskCompletionSource.SetResult(socketAsyncEventArgs);
            }

            // TODO add cancellationToken!!

            return await taskCompletionSource.Task;
        }
    }
}
