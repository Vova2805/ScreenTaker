using System.Collections.Generic;
using System.Linq;

namespace ScreenTaker.Models
{
    public class SecurityHelper
    {
        public static string GetImagePath(string folderPath, string imageCode)
        {
            return folderPath + "/" + imageCode + ".png";
        }

        private static List<Image> GetAccessibleImagesInFolder(ApplicationUser user, Folder folder,
            ScreenTakerEntities context)
        {
            return ((
                    from image in folder.Images
                    where image.IsPublic
                    select image)
                .Union(
                    from image in context.Images
                    join us in context.UserShares
                        on new { iid = (int?)image.Id, fid = image.FolderId } equals new { iid = us.ImageId, fid = folder.Id }
                    select image)
                .Union(
                    from im in context.Images
                    where im.FolderId == folder.Id
                    join gs in context.GroupShares
                        on im.Id equals gs.ImageId
                    join pg in context.PersonGroups
                        on gs.GroupId equals pg.Id
                    join gm in context.GroupMembers
                        on pg.Id equals gm.GroupId
                    where gm.PersonId == user.Id
                    select im)
            ).ToList();
        }

        public static List<Image> GetAccessibleImages(ApplicationUser user, Folder folder, ScreenTakerEntities context)
        {
            if (user == null)
            {                
                // show only public images to unauthorized user
                return folder.IsPublic ? folder.Images.Where(i => i.IsPublic).ToList() : null;
            }
            // show all images to authorized user who has access
            if (IsFolderSharedWithUser(user, folder.Person, folder, context))
            {
                return folder.Images.ToList();
            }
            return GetAccessibleImagesInFolder(user, folder, context);
        } 

        public static bool IsImageAccessible(ApplicationUser user, Person owner, Image image, ScreenTakerEntities context)
        {
            if (image.IsPublic)
                return true;
            if (user == null)
                return false;
            return image.Folder.OwnerId == user.Id
                   || context.UserShares.Any(
                       us => us.PersonId == user.Id && (us.ImageId == image.Id || us.FolderId == image.FolderId))
                   || (from gm in context.GroupMembers
                       join pg in context.PersonGroups
                           on new {pid = gm.PersonId, gid = gm.GroupId} equals new {pid = user.Id, gid = pg.Id}
                       join gs in context.GroupShares
                           on pg.Id equals gs.GroupId
                       where gs.ImageId == image.Id || gs.FolderId == image.FolderId
                       select gm.PersonId).Any()
                   || context.UserShares.Any(us => us.PersonId == user.Id && us.ImageId == image.Id);
        }

        public static bool IsFolderAccessible(ApplicationUser user, Person owner, Folder folder, ScreenTakerEntities context)
        {
            if (folder.IsPublic)
                return true;
            if (user == null)
                return false;
            return IsFolderSharedWithUser(user, owner, folder, context);
        }

        private static bool IsFolderSharedWithUser(ApplicationUser user, Person owner, Folder folder,
            ScreenTakerEntities context)
        {
            return folder.OwnerId == user.Id
                   || context.UserShares.Any(us => us.PersonId == user.Id && us.FolderId == folder.Id)
                   || (from gm in context.GroupMembers
                       join pg in context.PersonGroups
                           on new {pid = gm.PersonId, gid = gm.GroupId} equals new {pid = user.Id, gid = pg.Id}
                       join gs in context.GroupShares
                           on pg.Id equals gs.GroupId
                       where gs.FolderId == folder.Id
                       select gm.PersonId).Any();
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