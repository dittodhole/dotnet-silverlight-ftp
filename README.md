# sharpLightFtp
sharpLightFtp is a Silverlight 5 assembly, written in C#. It enables rudimental ftp-access within an in-browser scenario.

## Download

If you have [NuGet](http://nuget.org) installed, run the following command in the [Package Manager Console](http://docs.nuget.org/docs/start-here/using-the-package-manager-console), to install sharpLightFtp

    PM> Install-Package sharpLightFtp

## Notes

I am using [System.Net.FtpClient](http://netftp.codeplex.com/) as a guideline, as this project contains many helpful things (eg already implemented parsing of directory-listing, ...).

### Configure your Silverlight-Application properly

You need to follow [this awesome tutorial](http://www.pitorque.de/MisterGoodcat/post/Silverlight-5-Tidbits-Trusted-applications.aspx) to enable elevated trust in-browser, which is needed for socket-programming.

## License

            DO WHAT THE FUCK YOU WANT TO PUBLIC LICENSE
                    Version 2, December 2004

 Copyright (C) 2004 Sam Hocevar <sam@hocevar.net>

 Everyone is permitted to copy and distribute verbatim or modified
 copies of this license document, and changing it is allowed as long
 as the name is changed.

            DO WHAT THE FUCK YOU WANT TO PUBLIC LICENSE
   TERMS AND CONDITIONS FOR COPYING, DISTRIBUTION AND MODIFICATION

  0. You just DO WHAT THE FUCK YOU WANT TO.