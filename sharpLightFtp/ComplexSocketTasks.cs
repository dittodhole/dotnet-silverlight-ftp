using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sharpLightFtp.Extensions;

namespace sharpLightFtp
{
	internal static class ComplexSocketTasks
	{
		internal static bool SendFeatureToComplexSocketTask(ComplexSocket controlComplexSocket,
		                                                    Encoding encoding,
		                                                    TimeSpan sendTimeout)
		{
			return controlComplexSocket.Send("FEAT",
			                                 encoding,
			                                 sendTimeout);
		}

		internal static bool GetFeaturesTask(Task<bool> sendFeatureCommandTask,
		                                     ComplexSocket controlComplexSocket,
		                                     Encoding encoding,
		                                     TimeSpan receiveTimeout,
		                                     ref FtpFeatures features)
		{
			if (!sendFeatureCommandTask.Result)
			{
				return false;
			}

			var complexResult = controlComplexSocket.Receive(encoding,
			                                                 receiveTimeout);
			if (!complexResult.Success)
			{
				return false;
			}

			features = FtpFeatures.NONE;

			var complexEnums = (from name in Enum.GetNames(typeof (FtpFeatures))
			                    let enumName = name.ToUpper()
			                    let enumValue = Enum.Parse(typeof (FtpFeatures),
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
						features |= enumValue;
					}
				}
			}

			return true;
		}
	}
}
