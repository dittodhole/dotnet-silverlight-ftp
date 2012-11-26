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

			using (var sendSocketEventArgs = complexSocket.GetSocketEventArgs())
			{
				var success = sendSocketEventArgs.Send(sendBuffer, 0, sendBuffer.Length);

				return success;
			}
		}

		internal static bool SendAndReceiveIsSuccess(this ComplexFtpCommand complexFtpCommand)
		{
			Contract.Requires(complexFtpCommand != null);

			ComplexResult complexResult;
			var success = complexFtpCommand.SendAndReceive(out complexResult);

			return success;
		}

		internal static bool SendAndReceive(this ComplexFtpCommand complexFtpCommand, out ComplexResult complexResult)
		{
			Contract.Requires(complexFtpCommand != null);

			var success = complexFtpCommand.Send();
			if (!success)
			{
				complexResult = ComplexResult.FailedComplexResult;
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
