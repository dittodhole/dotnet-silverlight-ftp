﻿using System;
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
			this.GetListingAsync();
			//this.UploadAsync();
		}

		private void GetListingAsync()
		{
			var ftpClient = this.GetFtpClient();
			ThreadPool.QueueUserWorkItem(callBack =>
			{
				using (ftpClient)
				{
					var ftpListItems = ftpClient.GetListing("/");
					this.Dispatcher.BeginInvoke(() =>
					{
						var messageBoxText = string.Format("success: {0}", ftpListItems.Count());
						MessageBox.Show(messageBoxText);
					});
				}
			});
		}

		private void UploadAsync()
		{
			var ftpClient = this.GetFtpClient();
			ThreadPool.QueueUserWorkItem(callBack =>
			{
				var value = "hallo ich bin's ... wer bist'n du??";
				var bytes = Encoding.UTF8.GetBytes(value);
				var ftpFile = new FtpFile("/test12/hello.txt");

				using (ftpClient)
				{
					bool success;
					var memoryStream = new MemoryStream(bytes);
					using (memoryStream)
					{
						success = ftpClient.Upload(memoryStream, ftpFile);
					}
					this.Dispatcher.BeginInvoke(() =>
					{
						var messageBoxText = string.Format("success: {0}", success);
						MessageBox.Show(messageBoxText);
					});
				}
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
