using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace sharpLightFtp.Demo
{
	public partial class MainPage : UserControl
	{
		public MainPage()
		{
			this.InitializeComponent();
		}

		private void DoStuff(object sender, RoutedEventArgs e)
		{
			//this.ConnectAsync();
			//this.GetListingAsync();
			this.UploadAsync();
		}

		private void GetListingAsync()
		{
			var ftpClient = this.GetFtpClient();
			ThreadPool.QueueUserWorkItem(callBack =>
			{
				//var success = ftpClient.GetFeatures();
				var ftpListItems = ftpClient.GetListing("/");
				Dispatcher.BeginInvoke(() =>
				{
					var messageBoxText = string.Format("success: {0}", ftpListItems.Count());
					MessageBox.Show(messageBoxText);
				});
			});
		}

		private void UploadAsync()
		{
			var ftpClient = this.GetFtpClient();
			ThreadPool.QueueUserWorkItem(callBack =>
			{
				var value = "hallo ich bin's ... wer bist'n du??";
				var bytes = Encoding.UTF8.GetBytes(value);
				var memoryStream = new MemoryStream(bytes);
				var remotePath = "hello.txt";
				var success = ftpClient.Upload(memoryStream, remotePath);
				Dispatcher.BeginInvoke(() =>
				{
					var messageBoxText = string.Format("success: {0}", success);
					MessageBox.Show(messageBoxText);
				});
			});
		}

		private FtpClient GetFtpClient()
		{
			var server = this.tbServer.Text;
			var port = Convert.ToInt32(this.tbPort.Text);
			var username = this.tbUsername.Text;
			var password = this.tbPassword.Text;

			var ftpClient = new FtpClient
			{
				Server = server,
				Port = port,
				Username = username,
				Password = password
			};
			return ftpClient;
		}
	}
}
