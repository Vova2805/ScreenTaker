using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace ScreenTaker.Models
{
    public class SecurityHelper
    {
        public string GetImagePath(string folderPath, string imageCode)
        {
            return folderPath + "/" + imageCode + ".png";
        }
    }
}