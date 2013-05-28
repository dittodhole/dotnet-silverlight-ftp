using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using sharpLightFtp.Extensions;

namespace sharpLightFtp
{
	public static class FtpClientHelper
	{
		private static readonly Type TypeOfFtpFeatures = typeof (FtpFeatures);

		public static FtpFeatures ParseFtpFeatures(IEnumerable<string> messages)
		{
			// Example request & response:
			//Request:	FEAT
			//Response:	211-Features:
			//Response:	 MDTM
			//Response:	 MFMT
			//Response:	 TVFS
			//Response:	 UTF8
			//Response:	 MFF modify;UNIX.group;UNIX.mode;
			//Response:	 MLST modify*;perm*;size*;type*;unique*;UNIX.group*;UNIX.mode*;UNIX.owner*;
			//Response:	 LANG ja-JP;ko-KR;bg-BG;zh-CN;it-IT;zh-TW;ru-RU;en-US*;fr-FR
			//Response:	 REST STREAM
			//Response:	 SIZE
			//Response:	211 End

			var ftpFeatures = FtpFeatures.Unknown;

			var complexEnums = (from name in Enum.GetNames(TypeOfFtpFeatures)
			                    let value = Enum.Parse(TypeOfFtpFeatures,
			                                           name,
			                                           true)
			                    select new
			                    {
				                    Name = name,
				                    Value = (FtpFeatures) value
			                    }).ToList();
			foreach (var message in messages)
			{
				foreach (var complexEnum in complexEnums)
				{
					if (0 <= message.IndexOf(complexEnum.Name,
					                         StringComparison.InvariantCultureIgnoreCase))
					{
						var enumValue = complexEnum.Value;
						ftpFeatures |= enumValue;
						break;
					}
				}
			}

			return ftpFeatures;
		}

		public static IPAddress ParseIPAddress(IEnumerable<string> octets)
		{
			var address = (from octet in octets
			               let b = byte.Parse(octet)
			               select b);
			var ipAddress = new IPAddress(address.ToArray());

			return ipAddress;
		}

		public static int ParsePassivePort(string p1,
		                                   string p2)
		{
			int part1;
			if (!int.TryParse(p1,
			                  out part1))
			{
				return 0;
			}

			int part2;
			if (!int.TryParse(p2,
			                  out part2))
			{
				return 0;
			}

			// part1 * 256 + part2
			var port = (part1 << 8) + part2;

			return port;
		}

		public static FtpReply ParseFtpReply(string data)
		{
			var ftpResponseType = FtpResponseType.None;
			var messages = new List<string>();
			var stringResponseCode = string.Empty;
			var responseCode = 0;
			var responseMessage = string.Empty;

			var lines = data.Split(Environment.NewLine.ToCharArray(),
			                       StringSplitOptions.RemoveEmptyEntries);
			foreach (var line in lines)
			{
				var match = Regex.Match(line,
				                        @"^(\d{3})\s(.*)$");
				if (match.Success)
				{
					if (match.Groups.Count > 1)
					{
						stringResponseCode = match.Groups[1].Value;
					}
					if (match.Groups.Count > 2)
					{
						responseMessage = match.Groups[2].Value;
					}
					if (!string.IsNullOrWhiteSpace(stringResponseCode))
					{
						var firstCharacter = stringResponseCode.First();
						var currentCulture = Thread.CurrentThread.CurrentCulture;
						var character = firstCharacter.ToString(currentCulture);
						var intFtpResponseType = int.Parse(character);
						ftpResponseType = (FtpResponseType) intFtpResponseType;
						responseCode = int.Parse(stringResponseCode);
					}
				}
				messages.Add(line);
			}

			var ftpReply = new FtpReply(ftpResponseType,
			                            responseCode,
			                            responseMessage,
			                            messages,
			                            data);

			return ftpReply;
		}

		public static IPEndPoint ParseIPEndPoint(FtpReply ftpReply)
		{
			if (!ftpReply.Success)
			{
				return null;
			}
			var matches = Regex.Match(ftpReply.ResponseMessage,
			                          "([0-9]+),([0-9]+),([0-9]+),([0-9]+),([0-9]+),([0-9]+)");
			if (!matches.Success)
			{
				return null;
			}
			if (matches.Groups.Count != 7)
			{
				return null;
			}

			var ipAddress = ParseIPAddress(from index in Enumerable.Range(1,
			                                                              4)
			                               let octet = matches.Groups[index].Value
			                               select octet);
			var p1 = matches.Groups[5].Value;
			var p2 = matches.Groups[6].Value;
			var port = ParsePassivePort(p1,
			                            p2);
			var ipEndPoint = new IPEndPoint(ipAddress,
			                                port);

			return ipEndPoint;
		}

		public static IEnumerable<string> DirectoryChanges(FtpDirectory sourceFtpDirectory,
		                                                   FtpDirectory targetFtpDirectory)
		{
			var sourceHierarchy = sourceFtpDirectory.GetHierarchy()
			                                        .ToList();
			var targetHierarchy = targetFtpDirectory.GetHierarchy()
			                                        .ToList();

			var wereEqualBefore = true;
			var i = 0;
			var levelOfEqual = 0;
			for (; i < sourceHierarchy.Count; i++)
			{
				var sourceDirectory = sourceHierarchy.ElementAt(i);
				var targetDirectory = targetHierarchy.ElementAtOrDefault(i);

				if (wereEqualBefore)
				{
					if (!string.Equals(sourceDirectory,
					                   targetDirectory))
					{
						levelOfEqual = i;
						wereEqualBefore = false;
					}
				}
				if (!wereEqualBefore)
				{
					yield return FtpFileSystemObject.ParentChangeCommand;
				}
			}

			foreach (var directory in targetHierarchy.Skip(levelOfEqual))
			{
				yield return directory;
			}
		}
	}
}
