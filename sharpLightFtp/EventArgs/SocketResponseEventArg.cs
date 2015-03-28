namespace sharpLightFtp.EventArgs
{
    public sealed class SocketResponseEventArg : System.EventArgs
    {
        private readonly string _text;

        internal SocketResponseEventArg(string text)
        {
            this._text = text;
        }

        public string Text
        {
            get
            {
                return this._text;
            }
        }
    }
}
