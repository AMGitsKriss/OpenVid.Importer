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
    }
}
