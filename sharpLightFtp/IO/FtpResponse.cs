namespace sharpLightFtp.IO
{
    public sealed class FtpResponse
    {
        public static readonly FtpResponse Failed = new FtpResponse(false,
                                                                    null);

        private readonly string _data;
        private readonly bool _success;

        public FtpResponse(string data)
            : this(true,
                   data) {}

        public FtpResponse(bool success,
                           string data)
        {
            this._success = success;
            this._data = data;
        }

        public bool Success
        {
            get
            {
                return this._success;
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
