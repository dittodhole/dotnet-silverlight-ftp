sharpLightFtp is a Silverlight 5 assembly, written in C#. It enables rudimental ftp-access within an in-browser scenario.

## Download

If you have [NuGet](http://nuget.org) installed, run the following command in the [Package Manager Console](http://docs.nuget.org/docs/start-here/using-the-package-manager-console), to install sharpLightFtp

    PM> Install-Package sharpLightFtp

## Things to know ...

I am using System.Net.FtpClient (http://netftp.codeplex.com/) as a guideline, as this project contains many helpful things (eg already implemented parsing of directory-listing, ...).

### Configure your Silverlight-Application properly

You need to follow [this awesome tutorial](http://www.pitorque.de/MisterGoodcat/post/Silverlight-5-Tidbits-Trusted-applications.aspx) to enable elevated trust in-browser, which is needed for socket-programming.

### Licence

sharpLightFtp is licenced under [WTFPL](http://www.wtfpl.net/), but it would be nice if you link my project-site and inform me about any issues/bugfixes/... found/made, so that I can enhance my project.

### Unit Tests

In order to run unit tests you'll need to install [AgUnit](https://agunit.codeplex.com/) and [Silverlight 5 Toolkit](https://silverlight.codeplex.com/releases/view/78435)

### ReSharper

By 2013-05-24 JetBrains issued an open-source licence for ReSharper to this project. This is a very valuable support!