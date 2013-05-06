using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

// originally designed by http://netftp.codeplex.com/

namespace sharpLightFtp
{
	[DebuggerDisplay("Type: {Type} Name: {Name} Size: {Size}: Modify: {Modify}")]
	public sealed class FtpListItem
	{
		/// <summary>Initializes an empty parser</summary>
		private FtpListItem()
		{
			this.Size = -1;
			this.Mode = "0000";
			this.Modify = DateTime.MinValue;
		}

		/*
		/// <summary>
		/// 	Initializes a new FtpListItem object from a parser's results.
		/// </summary>
		/// <param name="parser"> </param>
		private FtpListItem(FtpListFormatParser parser)
			: this()
		{
			this.Type = parser.ObjectType;
			this.Name = parser.Name;
			this.Size = parser.Size;
			this.Modify = parser.Modify;
			this.Mode = parser.Mode;
			this.Owner = parser.Owner;
			this.Group = parser.Group;
		}
		*/

		/// <summary>Parses a given listing</summary>
		/// <param name="listing"> The single line that needs to be parsed </param>
		/// <param name="type"> The command that generated the line to be parsed </param>
		private FtpListItem(string listing,
		                    FtpListType type)
			: this()
		{
			this.Parse(listing,
			           type);
		}

		/*
		/// <summary>
		/// 	Parses a given listing
		/// </summary>
		/// <param name="listing"> </param>
		/// <param name="type"> </param>
		public FtpListItem(string[] listing, FtpListType type)
			: this()
		{
			foreach (var s in listing)
			{
				this.Parse(s, type);
			}
		}
		*/

		/// <summary>Gets the type of object (File/Directory/Unknown)</summary>
		public FtpObjectType Type { get; private set; }

		/// <summary>The file/directory name from the listing</summary>
		public string Name { get; private set; }

		/// <summary>The file size from the listing, default -1</summary>
		public long Size { get; set; }

		/// <summary>The file mode from the listing, default 0000</summary>
		public string Mode { get; set; }

		/// <summary>The last write time from the listing</summary>
		public DateTime Modify { get; set; }

		/// <summary>The file's owner from the listing</summary>
		public string Owner { get; set; }

		/// <summary>The file's group from the listing</summary>
		public string Group { get; set; }

		/// <summary>The file's link path, if it is a symlink.</summary>
		public string LinkPath { get; set; }

		#region LIST parsing

		/// <summary>Parses DOS and UNIX LIST style listings</summary>
		/// <param name="listing"> </param>
		private void ParseListListing(string listing)
		{
			foreach (var p in FtpListFormatParser.Parsers.Value)
			{
				if (!p.Parse(listing))
				{
					continue;
				}

				this.Type = p.ObjectType;
				this.Name = p.Name;
				this.Size = p.Size;
				this.Modify = p.Modify;
				this.Mode = p.Mode;
				this.Owner = p.Owner;
				this.LinkPath = p.LinkPath;
				this.Group = p.Group;

				break;
			}
		}

		#endregion

		#region MLS* Parsing

		private void ParseMachineListing(string listing)
		{
			var matches = new List<string>();
			var regularExpression = new Regex(@"(.+?)=(.*?);|  ?(.+?)$");
			Match match;

			if (Regex.Match(listing,
			                "^[0-9]+")
			         .Success)
			{
				// this is probably info messages, don't try to parse it
				return;
			}

			if ((match = regularExpression.Match(listing)).Success)
			{
				do
				{
					matches.Clear();

					for (var i = 1; i < match.Groups.Count; i++)
					{
						var group = match.Groups[i];
						if (group.Success)
						{
							matches.Add(group.Value);
						}
					}

					var key = matches[0];
					if (matches.Count != 2)
					{
						if (matches.Count == 1
						    && this.Name == null)
						{
							// filename
							this.Name = key;
						}
						continue;
					}

					// key=value pair
					var value = matches[1];
					switch (key.Trim()
					           .ToLower())
					{
						case "type":
							switch (this.Type)
							{
								case FtpObjectType.Unknown:
									var lower = value.ToLower();
									if (lower == "file")
									{
										this.Type = FtpObjectType.File;
									}
									else if (lower.Contains("os.unix=slink"))
									{
										this.Type = FtpObjectType.Link;
										this.LinkPath = value.Substring(value.LastIndexOf(':'));
									}
									else if (lower == "dir"
									         || lower == "cdir"
									         || lower == "pdir")
									{
										this.Type = FtpObjectType.Directory;
									}
									break;
							}
							break;
						case "size":
							if (this.Size == -1)
							{
								this.Size = long.Parse(value);
							}
							break;
						case "modify":
							if (this.Modify == DateTime.MinValue)
							{
								DateTime tmodify;
								var formats = new[]
								{
									"yyyyMMddHHmmss", "yyyyMMddHHmmss.fff"
								};
								if (DateTime.TryParseExact(value,
								                           formats,
								                           CultureInfo.InvariantCulture,
								                           DateTimeStyles.AssumeLocal,
								                           out tmodify))
								{
									this.Modify = tmodify;
								}
							}
							break;
						case "unix.mode":
							if (this.Mode == "0000")
							{
								this.Mode = value;
							}
							break;
						case "unix.owner":
							if (this.Owner == null)
							{
								this.Owner = value;
							}
							break;
						case "unix.group":
							if (this.Group == null)
							{
								this.Group = value;
							}
							break;
					}
				} while ((match = match.NextMatch()).Success);
			}
		}

		#endregion

		/// <summary>Parses a given listing</summary>
		/// <param name="listing"> The single line that needs to be parsed </param>
		/// <param name="type"> The command that generated the line to be parsed </param>
		private bool Parse(string listing,
		                   FtpListType type)
		{
			if (type == FtpListType.MLSD
			    || type == FtpListType.MLST)
			{
				this.ParseMachineListing(listing);
			}
			else if (type == FtpListType.LIST)
			{
				this.ParseListListing(listing);
			}
			else
			{
				var message = string.Format("{0} style formats are not supported.",
				                            type);
				throw new NotImplementedException(message);
			}

			var success = this.Type != FtpObjectType.Unknown;

			return success;
		}

		/// <summary>Parses an array of list results</summary>
		/// <param name="sequence"> Array of list results </param>
		/// <param name="ftpListType"> The command that generated the list being parsed </param>
		/// <returns> </returns>
		public static IEnumerable<FtpListItem> ParseList(IEnumerable<string> sequence,
		                                                 FtpListType ftpListType)
		{
			return from item in sequence
			       let ftpListItem = new FtpListItem(item,
			                                         ftpListType)
			       where ftpListItem.Type != FtpObjectType.Unknown
			       select ftpListItem;
		}
	}
}
