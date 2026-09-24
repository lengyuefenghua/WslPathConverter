using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace WslPathConverter
{
    internal sealed class ClipboardImageService
    {
        public bool TrySave(string targetPath, out string actualPath, out string error)
        {
            actualPath = string.Empty;
            error = string.Empty;
            try
            {
                var directory = Path.GetDirectoryName(targetPath);
                if (string.IsNullOrEmpty(directory))
                {
                    error = "The image save directory is invalid.";
                    return false;
                }
                Directory.CreateDirectory(directory);

                using (var source = Clipboard.GetImage())
                {
                    if (source == null)
                    {
                        error = "The clipboard image could not be read.";
                        return false;
                    }

                    using (var image = new Bitmap(source))
                    {
                        if (TrySave(image, targetPath))
                        {
                            actualPath = targetPath;
                            return true;
                        }

                        var fallback = BuildFallbackPath(targetPath);
                        if (TrySave(image, fallback))
                        {
                            actualPath = fallback;
                            return true;
                        }
                    }
                }
                error = "Could not save the clipboard image. The target file may be locked or inaccessible.";
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static bool TrySave(Image image, string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
                image.Save(path, ImageFormat.Png);
                return File.Exists(path);
            }
            catch
            {
                return false;
            }
        }

        private static string BuildFallbackPath(string targetPath)
        {
            var directory = Path.GetDirectoryName(targetPath);
            var name = Path.GetFileNameWithoutExtension(targetPath);
            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var candidate = Path.Combine(directory, name + "_" + stamp + ".png");
            var index = 1;
            while (File.Exists(candidate))
                candidate = Path.Combine(directory, name + "_" + stamp + "_" + index++ + ".png");
            return candidate;
        }
    }
}
