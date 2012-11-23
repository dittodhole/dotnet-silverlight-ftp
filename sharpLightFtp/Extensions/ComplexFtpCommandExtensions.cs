using System;
using System.Diagnostics.Contracts;

namespace sharpLightFtp.Extensions
{
	internal static class ComplexFtpCommandExtensions
	{
		internal static bool Send(this ComplexFtpCommand complexFtpCommand)
		{
			Contract.Requires(complexFtpCommand != null);
			Contract.Requires(!string.IsNullOrWhiteSpace(complexFtpCommand.Command));

			var command = string.Concat(complexFtpCommand.Command, Environment.NewLine);
			var encoding = complexFtpCommand.Encoding;
			var sendBuffer = encoding.GetBytes(command);

			var complexSocket = complexFtpCommand.ComplexSocket;
			var endPoint = complexSocket.EndPoint;

			using (var sendSocketEventArgs = endPoint.GetSocketEventArgs())
			{
				return sendSocketEventArgs.Send(sendBuffer, 0, sendBuffer.Length);
			}
		}

		internal static bool SendAndReceiveIsSuccess(this ComplexFtpCommand complexFtpCommand)
		{
			Contract.Requires(complexFtpCommand != null);

			ComplexResult complexResult;
			var success = complexFtpCommand.SendAndReceiveIsSuccess(out complexResult);

			return success;
		}

		internal static bool SendAndReceiveIsSuccess(this ComplexFtpCommand complexFtpCommand, out ComplexResult complexResult)
		{
			Contract.Requires(complexFtpCommand != null);

			var success = complexFtpCommand.Send();
			if (!success)
			{
				complexResult = ComplexResult.FailedSendComplexResult;
				return false;
			}

			var complexSocket = complexFtpCommand.ComplexSocket;
			var encoding = complexFtpCommand.Encoding;

			complexResult = complexSocket.Receive(encoding);
			success = complexResult.Success;

			return success;
		}
	}
}
