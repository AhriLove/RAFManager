using System;
using System.IO;
using System.IO.Compression;

namespace RAFManager
{
    public static class RafDat
    {
        public static string ApplicationDirectoryPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        public static void Save(byte[] buffer, string pathString)
        {
            try
            {
                if (buffer == null)
                {
                    throw new NullReferenceException();
                }

                string filePath = Path.GetFullPath(Path.Combine(ApplicationDirectoryPath, "export", pathString));
                string directoryFilePath = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directoryFilePath))
                {
                    Directory.CreateDirectory(directoryFilePath);
                }
                File.WriteAllBytes(filePath, buffer);
            }
            catch (Exception x)
            {
                Console.WriteLine(pathString + " - " + x.Message);
            }
        }

        public static byte[] Deflate(byte[] buffer, long position = 2)
        {
            try
            {
                using (var compressedMemoryStream = new MemoryStream(buffer))
                {
                    compressedMemoryStream.Position = position; // Skip bytes
                    using (var deflateStream = new DeflateStream(compressedMemoryStream, CompressionMode.Decompress))
                    {
                        using (var uncompressedMemoryStream = new MemoryStream())
                        {
                            deflateStream.CopyTo(uncompressedMemoryStream);
                            return uncompressedMemoryStream.ToArray();
                        }
                    }
                }
            }
            catch
            {
                return position == 2 ? Deflate(buffer, 0) : null;
            }
        }
    }
}
