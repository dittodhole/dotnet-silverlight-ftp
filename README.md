sharpLightFtp
=============

sharpLightFtp is a Silverlight 5 assembly, written in C#. It enables rudimental ftp-access within a in-browser scenario.
All you need is the assembly "sharpLightFtp.dll", enable elevated trust in your Silverlight-project and a website running at port 943 at your ftp-server, which serves the supplied "ClientAccessPolicy.xml".

I am using System.Net.FtpClient (http://netftp.codeplex.com/) as a guideline, as this project contains many helpful things (eg already implemented parsing of directory-listing, ...).

You need to follow this (http://www.pitorque.de/MisterGoodcat/post/Silverlight-5-Tidbits-Trusted-applications.aspx) awesome tutorial to enable elevated trust in-browser, which is need for socket-programming.