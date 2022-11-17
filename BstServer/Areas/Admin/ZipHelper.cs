using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BstServer.Areas.Admin;

public static class ZipHelper
{
    public static List<string> ExtractZipFile(string archiveFilenameIn, string password, string outFolder)
    {
        List<string> decompressed = new List<string>();

        ZipFile zipFile = null;
        try
        {
            FileStream fs = File.OpenRead(archiveFilenameIn);
            zipFile = new ZipFile(fs);
            if (!string.IsNullOrEmpty(password))
            {
                zipFile.Password = password; // AES encrypted entries are handled automatically
            }

            foreach (ZipEntry zipEntry in zipFile)
            {
                if (!zipEntry.IsFile)
                {
                    continue; // Ignore directories
                }

                string entryFileName = zipEntry.Name;
                // to remove the folder from the entry:- entryFileName = Path.GetFileName(entryFileName);
                // Optionally match entrynames against a selection list here to skip as desired.
                // The unpacked length is available in the zipEntry.Size property.

                string extension = zipEntry.Name.Split('.').Last().ToLower();
                if (extension != "vpk")
                    continue;

                byte[] buffer = new byte[4096]; // 4K is optimum
                Stream zipStream = zipFile.GetInputStream(zipEntry);

                // Manipulate the output filename here as desired.
                string outPath = Path.Combine(outFolder, entryFileName);
                if (!Directory.Exists(outFolder))
                    Directory.CreateDirectory(outFolder);

                // Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
                // of the file, but does not waste memory.
                // The "using" will close the stream even if an exception occurs.
                using (FileStream streamWriter = File.Create(outPath))
                {
                    StreamUtils.Copy(zipStream, streamWriter, buffer);
                }

                decompressed.Add(outPath);
            }
        }
        finally
        {
            if (zipFile != null)
            {
                zipFile.IsStreamOwner = true; // Makes close also shut the underlying stream
                zipFile.Close(); // Ensure we release resources
            }
        }

        return decompressed;
    }
}