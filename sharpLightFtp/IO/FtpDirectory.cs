using System.IO;

namespace sharpLightFtp.IO
{
    public sealed class FtpDirectory : FtpFileSystemObject
    {
        public static readonly FtpDirectory Root = new FtpDirectory("/");

        private FtpDirectory(string path)
            : base(path) {}

        public string DirectoryName
        {
            get
            {
                var directoryName = this.GetFileName();

                return directoryName;
            }
        }

        public static FtpDirectory Create(string path)
        {
            var ftpDirectory = new FtpDirectory(path);

            return ftpDirectory;
        }

        public static FtpDirectory Create(FtpDirectory baseFtpDirectory,
                                          string directory)
        {
            var fullName = baseFtpDirectory.FullName;
            var path = Path.Combine(fullName,
                                    directory);

            var ftpDirectory = Create(path);

            return ftpDirectory;
        }
    }
}
