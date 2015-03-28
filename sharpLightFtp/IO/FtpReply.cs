using System.Collections.Generic;
using System.Linq;

namespace sharpLightFtp.IO
{
    public sealed class FtpReply
    {
        internal static readonly FtpReply Failed = new FtpReply(FtpResponseType.None,
                                                                0,
                                                                null,
                                                                Enumerable.Empty<string>(),
                                                                null);

        public static string RegularExpressionForParsing = @"^(\d{3})[\s\-](.*)$";
        private readonly string _data;
        private readonly FtpResponseType _ftpResponseType;
        private readonly List<string> _messages = new List<string>();
        private readonly int _responseCode;
        private readonly string _responseMessage;

        internal FtpReply(FtpResponseType ftpResponseType,
                          int responseCode,
                          string responseMessage,
                          IEnumerable<string> messages,
                          string data)
        {
            this._ftpResponseType = ftpResponseType;
            this._responseCode = responseCode;
            this._responseMessage = responseMessage;
            this._data = data;
            this._messages.AddRange(messages);
        }

        public int ResponseCode
        {
            get
            {
                return this._responseCode;
            }
        }

        public string ResponseMessage
        {
            get
            {
                return this._responseMessage;
            }
        }

        public IEnumerable<string> Messages
        {
            get
            {
                return this._messages;
            }
        }

        public FtpResponseType FtpResponseType
        {
            get
            {
                return this._ftpResponseType;
            }
        }

        public bool Success
        {
            get
            {
                switch (this.FtpResponseType)
                {
                    case FtpResponseType.PositiveIntermediate:
                    case FtpResponseType.PositiveCompletion:
                    case FtpResponseType.PositivePreliminary:
                        return true;
                }
                return false;
            }
        }

        public bool Completed
        {
            get
            {
                switch (this.FtpResponseType)
                {
                    case FtpResponseType.PermanentNegativeCompletion:
                    case FtpResponseType.PositiveCompletion:
                    case FtpResponseType.TransientNegativeCompletion:
                        return true;
                }
                return false;
            }
        }

        public string Data
        {
            get
            {
                return this._data;
            }
        }
    }
}
