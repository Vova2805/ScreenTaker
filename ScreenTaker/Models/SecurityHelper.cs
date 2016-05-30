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

        public bool IsImageAccessible(ApplicationUser user, Person owner, Image image)
        {
            if (image.IsPublic)
            {
                return true;
            }
            if (user == null)
            {
                return false;
            }
            if (image.Folder.OwnerId == user.Id)
            {
                 return true;
            }

            return false;
        }

        public bool IsFolderAccessible(ApplicationUser user, Person owner, Folder folder)
        {
            if (folder.IsPublic)
            {
                return true;
            }

            if (user == null)
            {
                return false;
            }

            if (folder.OwnerId == user?.Id)
            {
                return true;
            }

            return false;
        }

        public bool IsImageEditable(ApplicationUser user, Person owner, Image image)
        {
            return image.Folder.OwnerId == user?.Id;
        }

        public bool IsFolderEditable(ApplicationUser user, Person owner, Folder folder)
        {
            return folder.OwnerId == user?.Id;
        }

    }
}