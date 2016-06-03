using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace ScreenTaker.Models
{
    public static class SecurityHelper
    {
        public static string GetImagePath(string folderPath, string imageCode)
        {
            return folderPath + "/" + imageCode + ".png";
        }

        public static bool IsImageAccessible(ApplicationUser user, Person owner, Image image, ScreenTakerEntities context)
        {
            if (image.IsPublic)
                return true;
            if (user == null)
                return false;
            return image.Folder.OwnerId == user.Id
                   || context.UserShares.Any(us => us.PersonId == user.Id && us.ImageId == image.Id)
                   || (from gm in context.GroupMembers
                       join pg in context.PersonGroups
                           on new {pid = gm.PersonId, gid = gm.GroupId} equals new {pid = user.Id, gid = pg.Id}
                       join gs in context.GroupShares
                           on pg.Id equals gs.GroupId
                       where gs.ImageId == image.Id
                       select gm.PersonId).Any();
        }

        public static bool IsFolderAccessible(ApplicationUser user, Person owner, Folder folder, ScreenTakerEntities context)
        {
            return folder.IsPublic || folder.OwnerId == user?.Id;
        }

        public static bool IsImageEditable(ApplicationUser user, Person owner, Image image, ScreenTakerEntities context)
        {
            return image.Folder.OwnerId == user?.Id;
        }

        public static bool IsFolderEditable(ApplicationUser user, Person owner, Folder folder, ScreenTakerEntities context)
        {
            return folder.OwnerId == user?.Id;
        }

    }
}