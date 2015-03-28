using System.Net.Sockets;

namespace sharpLightFtp.Extensions
{
    public static class SocketAsyncEventArgsExtensions
    {
        public static bool GetSuccess(this SocketAsyncEventArgs socketAsyncEventArgs)
        {
            // TODO check against .ConnectByNameError
            // TODO check against .SocketError
            // TODO check against ((SocketAsyncEventArgsUserToken) .UserToken).TimeoutExpired

            var socketError = socketAsyncEventArgs.SocketError;
            if (socketError != SocketError.Success)
            {
                return false;
            }

            return true;
        }
    }
}
