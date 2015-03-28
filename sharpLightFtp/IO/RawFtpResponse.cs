namespace sharpLightFtp.IO
{
    public sealed class RawFtpResponse
    {
        public static readonly RawFtpResponse Failed = new RawFtpResponse(false,
                                                                          new byte[0]);

        private readonly byte[] _buffer;
        private readonly bool _success;

        public RawFtpResponse(bool success,
                              byte[] buffer)
        {
            this._success = success;
            this._buffer = buffer ?? new byte[0];
        }

        public bool Success
        {
            get
            {
                return this._success;
            }
        }

        public byte[] Buffer
        {
            get
            {
                return this._buffer;
            }
        }
    }
}
