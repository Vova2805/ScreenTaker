using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Web;

namespace ScreenTaker.Models
{
    public class ImageCompressor
    {
        public Bitmap Compress(Bitmap originalBM, Size compressedImageSize)
        {                     
            var size = new Size();
            if (originalBM.Size.Height < originalBM.Size.Width)
                size = new Size(Convert.ToInt32(compressedImageSize.Height * 1.0 / originalBM.Size.Height * 1.0 * originalBM.Size.Width * 1.0), compressedImageSize.Height);
            else
                size = new Size(compressedImageSize.Width, Convert.ToInt32(compressedImageSize.Width * 1.0 / originalBM.Size.Width * 1.0 * originalBM.Size.Height * 1.0));            
            Bitmap resizedBM = new Bitmap(originalBM, size);            
            return resizedBM;                        
        }

        private Bitmap CropImage(Bitmap source, Rectangle section)
        {
            Bitmap bmp = new Bitmap(section.Width, section.Height);
            Graphics g = Graphics.FromImage(bmp);
            g.DrawImage(source, 0, 0, section, GraphicsUnit.Pixel);
            return bmp;
        }

        public bool IsValid(object value)
        {
            var file = value as HttpPostedFileBase;
            if (file == null)            
                return false;                        
            try
            {
                var allowedFormats = new[]
                {
                    ImageFormat.Jpeg,
                    ImageFormat.Png,
                    ImageFormat.Gif,
                    ImageFormat.Bmp
                };
                using (var img =  System.Drawing.Image.FromStream(file.InputStream))                
                    return allowedFormats.Contains(img.RawFormat);                
            }
            catch { }
            return false;
        }
    }
}