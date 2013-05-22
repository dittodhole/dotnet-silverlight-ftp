sharpLightFtp
=============

sharpLightFtp is a Silverlight 5 assembly, written in C#. It enables rudimental ftp-access within an in-browser scenario.
All you need is the assembly "sharpLightFtp.dll", enable elevated trust in your Silverlight-project and a website running at port 943 at your ftp-server, which serves the supplied "ClientAccessPolicy.xml".

I am using System.Net.FtpClient (http://netftp.codeplex.com/) as a guideline, as this project contains many helpful things (eg already implemented parsing of directory-listing, ...).

You need to follow this (http://www.pitorque.de/MisterGoodcat/post/Silverlight-5-Tidbits-Trusted-applications.aspx) awesome tutorial to enable elevated trust in-browser, which is needed for socket-programming.

Licence
=============
sharpLightFtp is licenced under WTFPL (http://www.wtfpl.net/), but it would be nice if you link my project-site and inform me about any issues/bugfixes/... found/made, so that I can enhance my project.

Unit Tests
=============
In order to run unit tests you'll need to install AgUnit (https://agunit.codeplex.com/) and Silverlight 5 Toolkit (https://silverlight.codeplex.com/releases/view/78435)