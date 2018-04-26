
using System;
using System.IO;
using System.Text;

namespace Utility
{
    public static class MD5Ext
    {
        public static string MD5(this string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var hash = md5.ComputeHash(bytes);
                return BitConverter.ToString(hash).ToLowerInvariant();
            }
        }

        public static string MD5(this Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).ToLowerInvariant();
            }
        }


        public static string MD5OfFile(this string filePath)
        {
            using (var stream = File.OpenRead(filePath))
                return stream.MD5();
        }
    }
}