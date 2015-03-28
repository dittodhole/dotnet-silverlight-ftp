using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

// originally designed by http://netftp.codeplex.com/

namespace sharpLightFtp
{
    /// <summary>Map's regex group index's to the appropriate fields in the parser results.</summary>
    internal sealed class FtpListFormatParser : IDisposable
    {
        internal static readonly Lazy<List<FtpListFormatParser>> Parsers = new Lazy<List<FtpListFormatParser>>(() => new List<FtpListFormatParser>
        {
            new FtpListFormatParser(@"(\d+-\d+-\d+\s+\d+:\d+\w+)\s+<DIR>\s+(.*)",
                                    2,
                                    -1,
                                    1,
                                    -1,
                                    -1,
                                    -1,
                                    -1,
                                    FtpObjectType.Directory),
            new FtpListFormatParser(@"(\d+-\d+-\d+\s+\d+:\d+\w+)\s+(\d+)\s+(.*)",
                                    3,
                                    2,
                                    1,
                                    -1,
                                    -1,
                                    -1,
                                    -1,
                                    FtpObjectType.File), // DOS format file
            new FtpListFormatParser(@"(d[\w-]{9})\s+\d+\s+([\w\d]+)\s+([\w\d]+)\s+\d+\s+(\w+\s+\d+\s+\d+:?\d+)\s+(.*)",
                                    5,
                                    -1,
                                    4,
                                    1,
                                    2,
                                    3,
                                    -1,
                                    FtpObjectType.Directory), // UNIX format directory
            new FtpListFormatParser(@"(-[\w-]{9})\s+\d+\s+([\w\d]+)\s+([\w\d]+)\s+(\d+)\s+(\w+\s+\d+\s+\d+:?\d+)\s+(.*)",
                                    6,
                                    4,
                                    5,
                                    1,
                                    2,
                                    3,
                                    -1,
                                    FtpObjectType.File), // UNIX format file
            new FtpListFormatParser(@"(l[\w-]{9})\s+\d+\s+([\w\d]+)\s+([\w\d]+)\s+(\d+)\s+(\w+\s+\d+\s+\d+:?\d+)\s+(.*) ->\s+(.*)",
                                    6,
                                    4,
                                    5,
                                    1,
                                    2,
                                    3,
                                    7,
                                    FtpObjectType.Link), // UNIX format link
            new FtpListFormatParser(@"(c[\w-]{9})\s+\d+\s+([\w\d]+)\s+([\w\d]+)\s+(\d+)\s+(\w+\s+\d+\s+\d+:?\d+)\s+(.*)",
                                    6,
                                    4,
                                    5,
                                    1,
                                    2,
                                    3,
                                    -1,
                                    FtpObjectType.Device), // UNIX format device
            new FtpListFormatParser(@"(b[\w-]{9})\s+\d+\s+([\w\d]+)\s+([\w\d]+)\s+(\d+)\s+(\w+\s+\d+\s+\d+:?\d+)\s+(.*)",
                                    6,
                                    4,
                                    5,
                                    1,
                                    2,
                                    3,
                                    -1,
                                    FtpObjectType.Device),
            new FtpListFormatParser(@"d[\w-]+\s\d+\s[\d\w]+\s\d+\s+\w+\s+\d+\s+\d+:?\d+\s+(.*)",
                                    1,
                                    0,
                                    0,
                                    0,
                                    0,
                                    0,
                                    0,
                                    FtpObjectType.Directory), // other format directory
            new FtpListFormatParser(@"-[\w-]+\s+\d+\s+[\w\d]+\s+(\d+)\s+\w+\s+\d+\s+\d+:?\d+\s+(.*)",
                                    2,
                                    1,
                                    0,
                                    0,
                                    0,
                                    0,
                                    0,
                                    FtpObjectType.File), // other format file
            new FtpListFormatParser(@"(\d+)\s+(\w+-\d{1,2}-\d{4}\s+\d{1,2}:\d{1,2}:\d{1,2})\s+(.*?)\s+\<\DIR\>",
                                    3,
                                    -1,
                                    2,
                                    -1,
                                    -1,
                                    -1,
                                    -1,
                                    FtpObjectType.Directory), //VxWorks format directory
            new FtpListFormatParser(@"(\d+)\s+(\w+-\d{1,2}-\d{4}\s+\d{1,2}:\d{1,2}:\d{1,2})\s+(.*?)\s+",
                                    3,
                                    1,
                                    2,
                                    -1,
                                    -1,
                                    -1,
                                    -1,
                                    FtpObjectType.File) ////VxWorks format file
        });

        private readonly FtpObjectType _objectType;

        /// <summary>The index of the match group collection where the object group can be found after a successful parse. Setting a less than 1 value indicates that this field is not available.</summary>
        private int _groupIndex;

        /// <summary>The index of the match group collection where the object link path can be found afet a successful parse. Setting a less than 1 value indicates that this field is not available.</summary>
        private int _linkPathIndex;

        /// <summary>The index of the match group collection where the object mode can be found after a successful parse. Setting a less than 1 value indicates that this field is not available.</summary>
        private int _modeIndex;

        /// <summary>The index in the match group collection where the object name can be found after a successfull parse. Setting a less than 1 value indicates that this field is not available.</summary>
        private int _modifyIndex;

        /// <summary>The index in the match group collection where the object name can be found after a successfull parse. Setting a less than 1 value indicates that this field is not available.</summary>
        private int _nameIndex;

        /// <summary>The index of the match group collection where the object owner can be found after a successful parse. Setting a less than 1 value indicates that this field is not available.</summary>
        private int _ownerIndex;

        /// <summary>The regex used to parse the input string.</summary>
        private Regex _regex;

        /// <summary>The index in the match group collection where the object name can be found after a successfull parse. Setting a less than 1 value indicates that this field is not available.</summary>
        private int _sizeIndex;

        /// <summary>Creates a new instance of the FtpListParser object and sets the given index locations as specified.</summary>
        /// <param name="re"> </param>
        /// <param name="nameIndex"> </param>
        /// <param name="sizeIndex"> </param>
        /// <param name="modifyIndex"> </param>
        /// <param name="modeIndex"> </param>
        /// <param name="ownerIndex"> </param>
        /// <param name="groupIndex"> </param>
        /// <param name="linkPathIndex"> </param>
        /// <param name="type"> </param>
        private FtpListFormatParser(Regex re,
                                    int nameIndex,
                                    int sizeIndex,
                                    int modifyIndex,
                                    int modeIndex,
                                    int ownerIndex,
                                    int groupIndex,
                                    int linkPathIndex,
                                    FtpObjectType type)
        {
            this._regex = re;
            this._nameIndex = nameIndex;
            this._sizeIndex = sizeIndex;
            this._modifyIndex = modifyIndex;
            this._modeIndex = modeIndex;
            this._ownerIndex = ownerIndex;
            this._groupIndex = groupIndex;
            this._linkPathIndex = linkPathIndex;
            this._objectType = type;
        }

        /// <summary>Creates a new instance of the FtpListParser object and sets the given index locations as sepcified.</summary>
        /// <param name="regex"> </param>
        /// <param name="nameIndex"> </param>
        /// <param name="sizeIndex"> </param>
        /// <param name="modifyIndex"> </param>
        /// <param name="modeIndex"> </param>
        /// <param name="ownerIndex"> </param>
        /// <param name="groupIndex"> </param>
        /// <param name="linkPathIndex"> </param>
        /// <param name="type"> </param>
        private FtpListFormatParser(string regex,
                                    int nameIndex,
                                    int sizeIndex,
                                    int modifyIndex,
                                    int modeIndex,
                                    int ownerIndex,
                                    int groupIndex,
                                    int linkPathIndex,
                                    FtpObjectType type)
            : this(new Regex(regex),
                   nameIndex,
                   sizeIndex,
                   modifyIndex,
                   modeIndex,
                   ownerIndex,
                   groupIndex,
                   linkPathIndex,
                   type) {}

        /// <summary>The type of objec this parser is for (File or Directory)</summary>
        internal FtpObjectType ObjectType
        {
            get
            {
                return this._objectType;
            }
        }

        /// <summary>The name of the object. A null value is returned when this information is not available.</summary>
        internal string Name
        {
            get
            {
                if (this._nameIndex > 0
                    && this.Match != null
                    && this.Match.Success
                    && this.Match.Groups.Count > this._nameIndex)
                {
                    var value = this.Match.Groups[this._nameIndex].Value;
                    return value;
                }

                return null;
            }
        }

        /// <summary>The size of the object. 0 is returned when this information is not available.</summary>
        internal long Size
        {
            get
            {
                if (this._sizeIndex > 0
                    && this.Match != null
                    && this.Match.Groups.Count > this._sizeIndex)
                {
                    long size;

                    var value = this.Match.Groups[this._sizeIndex].Value;
                    if (long.TryParse(value,
                                      out size))
                    {
                        return size;
                    }
                }

                return 0;
            }
        }

        /// <summary>The last write time of the object. DateTime.MinValue is returned when this information is not available.</summary>
        internal DateTime Modify
        {
            get
            {
                if (this._modifyIndex > 0
                    && this.Match != null
                    && this.Match.Groups.Count > this._modifyIndex)
                {
                    DateTime date;
                    var formats = new[]
                    {
                        "MMM dd HH:mm", "MMM  d HH:mm", "MMM dd  yyyy"
                    };

                    // try to parse an exact format first, if it fails
                    // then just see if TryParse can extract any date
                    // and if it fails, return DateTime.MinValue
                    var value = this.Match.Groups[this._modifyIndex].Value;
                    if (!DateTime.TryParseExact(value,
                                                formats,
                                                CultureInfo.InvariantCulture,
                                                DateTimeStyles.AssumeLocal,
                                                out date))
                    {
                        if (!DateTime.TryParse(value,
                                               CultureInfo.InvariantCulture,
                                               DateTimeStyles.AssumeLocal,
                                               out date))
                        {
                            date = DateTime.MinValue;
                        }
                    }

                    return date;
                }

                return DateTime.MinValue;
            }
        }

        /// <summary>The mode of the object. null is return when this information is not available.</summary>
        internal string Mode
        {
            get
            {
                if (this._modeIndex > 0
                    && this.Match != null
                    && this.Match.Groups.Count > this._modeIndex)
                {
                    var value = this.Match.Groups[this._modeIndex].Value;
                    return value;
                }
                return null;
            }
        }

        /// <summary>The owner of the object. null is return when this information is not available.</summary>
        internal string Owner
        {
            get
            {
                if (this._ownerIndex > 0
                    && this.Match != null
                    && this.Match.Groups.Count > this._ownerIndex)
                {
                    var value = this.Match.Groups[this._ownerIndex].Value;
                    return value;
                }
                return null;
            }
        }

        /// <summary>The group of the object. null is return when this information is not available.</summary>
        internal string Group
        {
            get
            {
                if (this._groupIndex > 0
                    && this.Match != null
                    && this.Match.Groups.Count > this._groupIndex)
                {
                    var value = this.Match.Groups[this._groupIndex].Value;
                    return value;
                }
                return null;
            }
        }

        /// <summary>The link path of the object in case it is a symlink. null is return when this information is not available.</summary>
        internal string LinkPath
        {
            get
            {
                if (this._linkPathIndex > 0
                    && this.Match != null
                    && this.Match.Groups.Count > this._linkPathIndex)
                {
                    var value = this.Match.Groups[this._linkPathIndex].Value;
                    return value;
                }
                return null;
            }
        }

        /// <summary>The match result after calling the Parse() method.</summary>
        private Match Match { get; set; }

        #region IDisposable Members

        public void Dispose()
        {
            this._regex = null;
            this.Match = null;
            this._nameIndex = 0;
            this._sizeIndex = 0;
            this._modifyIndex = 0;
            this._modeIndex = 0;
            this._groupIndex = 0;
            this._ownerIndex = 0;
            this._linkPathIndex = 0;
        }

        #endregion

        /// <summary>Parse the given string</summary>
        /// <param name="input"> </param>
        /// <returns> Returns true on success, false on failure </returns>
        internal bool Parse(string input)
        {
            this.Match = this._regex.Match(input);
            return this.Match.Success;
        }
    }
}
