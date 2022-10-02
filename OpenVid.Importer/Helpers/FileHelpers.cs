using System;
using System.IO;
using System.Security.Cryptography;

namespace OpenVid.Importer.Helpers
{
    public static class FileHelpers
    {
        public static void TouchDirectory(string directory)
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }
        public static string GenerateHash(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var ms = File.OpenRead(filename))
                {
                    byte[] hash = md5.ComputeHash(ms);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        public static void CopyDirectory(string source, string destination)
        {
            foreach (var directory in Directory.GetDirectories(source))
            {
                string dirName = Path.GetFileName(directory);
                if (!Directory.Exists(Path.Combine(destination, dirName)))
                {
                    Directory.CreateDirectory(Path.Combine(destination, dirName));
                }
                CopyDirectory(directory, Path.Combine(destination, dirName));
            }

            foreach (var file in Directory.GetFiles(source))
            {
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
            }

        }
    }
}
