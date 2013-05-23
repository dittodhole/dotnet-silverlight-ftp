namespace sharpLightFtp.EventArgs
{
	public sealed class SocketRequestEventArg : System.EventArgs
	{
		private readonly string _text;

		public SocketRequestEventArg(string text)
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
