using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;

namespace ninlabs.automark.VisualStudio.Util
{
    class Zip
    {
        public static void ZipFile(string zipFilename, string filenameToAdd)
        {
            using (Package zipPackage = ZipPackage.Open(zipFilename, FileMode.OpenOrCreate))
            {
                string destFilename = ".\\" + Path.GetFileName(filenameToAdd);

                Uri zipPartUri = PackUriHelper.CreatePartUri(new Uri(destFilename, UriKind.Relative));

                if (zipPackage.PartExists(zipPartUri))
                {
                    zipPackage.DeletePart(zipPartUri);
                }

                PackagePart zipPackagePart = zipPackage.CreatePart(zipPartUri, "", CompressionOption.Maximum);

                using (FileStream fileStream = new FileStream(filenameToAdd, FileMode.Open, FileAccess.Read))
                {
                    using (Stream dest = zipPackagePart.GetStream())
                    {
                        fileStream.CopyTo(dest);
                        //CopyStream(fileStream, dest);
                    }
                }
            }
        }
    }
}
