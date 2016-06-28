using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ScreenTaker.Models;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net.Mail;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Image = ScreenTaker.Models.Image;
using System.Globalization;
using System.Threading;

namespace ScreenTaker.Controllers
{
    [Authorize]
    public class HomeController : GeneralController
    {
        private ScreenTakerEntities _entities = new ScreenTakerEntities();
        private ImageCompressor _imageCompressor = new ImageCompressor();
        private RandomStringGenerator _stringGenerator = new RandomStringGenerator()
        {
            Chars = "1234567890qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM",
            Length = 15
        };
        int SingleImageId;
        private SecurityHelper _securityHelper = new SecurityHelper();

        [AllowAnonymous]
        public ActionResult Index(string lang = "en")
        {
            ViewBag.Localize = getLocale();
            return RedirectToAction("Welcome", "Home", new { lang = getLocale() });
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult Welcome(string lang = "en")
        {
            try
            {
                ViewBag.Localize = getLocale();                
            }
            catch
            {
                return RedirectToAction("Message", "Home", new { lang = getLocale() });
            }
            return View("Welcome", new { lang = getLocale() });
        }

        [HttpPost]
        public ActionResult Welcome(HttpPostedFileBase file, string lang = "en")
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(getLocale());
            int fId = -1;
            ViewBag.Localize = getLocale();
            if (file != null)
            {
                using (var transaction = _entities.Database.BeginTransaction())
                {
                    try
                    {                       
                        ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext()
                           .GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());
                        if (user == null)
                            return RedirectToAction("Account/Register", new { lang = getLocale() });
                        var folder = _entities.Folders.Where(w=>w.OwnerId==user.Id).FirstOrDefault();
                        bool accesGranted = false;               
                        if (folder != null)                
                            accesGranted = SecurityHelper.IsFolderEditable(user, folder.Person, folder, _entities);                
                        else
                            return RedirectToAction("Account/Register", new { lang = getLocale() });
                        if (!accesGranted)                
                            return RedirectToAction("Welcome", new { lang = getLocale() });
                        fId = folder.Id;
                        if (!_imageCompressor.IsValid(file))
                            throw new Exception(Resources.Resource.ERR_IMAGE_NOT_VALID);                    
                        var sharedCode = _stringGenerator.Next();
                        var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                        var serverFolderId = 0;
                        var last = _entities.ServerFolder.Where(w => w.Count < 1000).ToList().LastOrDefault();
                        if (last == null || !System.IO.Directory.Exists(Server.MapPath("/img/") + last.SharedCode))
                        {
                            var serverFolder = new ServerFolder() { Count = 2, SharedCode = _stringGenerator.Next() };
                            _entities.ServerFolder.Add(serverFolder);
                            System.IO.Directory.CreateDirectory(Server.MapPath("/img/") + serverFolder.SharedCode);
                            serverFolderId = serverFolder.Id;
                        }
                        else
                        {
                            serverFolderId = last.Id;
                            last.Count += 2;
                        }
                        var image = new Image();
                        image.IsPublic = false;
                        image.FolderId = folder.Id;
                        image.SharedCode = sharedCode;
                        image.Name = fileName;
                        image.ServerFolderId = serverFolderId;
                        image.PublicationDate = DateTime.Now;
                        _entities.Images.Add(image);
                        _entities.SaveChanges();
                        var bitmap = new Bitmap(file.InputStream);
                        var path = Path.Combine(Server.MapPath("~/img/"), image.ServerFolder.SharedCode + "/"+ sharedCode + ".png");
                        bitmap.Save(path, ImageFormat.Png);
                        var compressedBitmap = _imageCompressor.Compress(bitmap, new Size(128, 128));
                        path = Path.Combine(Server.MapPath("~/img/"), image.ServerFolder.SharedCode + "/"+ sharedCode + "_compressed.png");
                        compressedBitmap.Save(path, ImageFormat.Png);
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        ViewBag.MessageContent= ex.Message;
                        ViewBag.MessageTitle = Resources.Resource.ERR_TITLE;
                        ViewBag.Localize = getLocale();
                        return View("Welcome", new { lang = getLocale() });
                    }
                }
            }
            return RedirectToAction("Images", new { id = fId, lang = getLocale() });
        }

        // GET: /Account/ConfirmEmail
        [AllowAnonymous]
        public ActionResult Message(string lang = "en")
        {
            ViewBag.Localize = getLocale();
            ViewBag.Email = "";
            return View();
        }

        [AllowAnonymous]
        public ActionResult About(string lang = "en")
        {
            ViewBag.Localize = getLocale();
            ViewBag.Message = "Your application description page.";
            return View("About", new { lang = getLocale() });
        }

        [AllowAnonymous]
        public ActionResult Contact(string lang = "en")
        {
            ViewBag.Message = "Your contact page.";
            ViewBag.Localize = getLocale();
            return View();
        }
        #region Library
        public ActionResult Library(string lang = "en")
        {
            try {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(getLocale());
                ViewBag.Localize = getLocale();
                ViewBag.Message = "Library page";
                ViewBag.FolderLinkBASE = GetFolderLink("");

                ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext()
                    .GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());
                ViewBag.UserId = user.Id;
                ViewBag.BASE_URL = GetBaseUrl() + "";
                var folders = _entities.Folders.ToList().Where(f => f.OwnerId == user.Id).ToList();
                var sharedLinks = folders.ToList().Select(f => GetSharedFolderLink(f.SharedCode)).ToList();
                ViewBag.Count = folders.Count;
                ViewBag.Folders = folders;
                ViewBag.SharedLinks = sharedLinks;
                var folder = folders.ToList().Where(f => f.OwnerId == user.Id).ToList().FirstOrDefault();
                ViewBag.FolderLink = GetFolderLink(folder.SharedCode);
                if (folder == null)
                {
                    ViewBag.CurrentFolderShCode = folders.Where(f => f.OwnerId == user.Id).ToList().First().SharedCode;
                    ViewBag.CurrentFolderId = folders.Where(f => f.OwnerId == user.Id).ToList().First().Id;
                    ViewBag.CurrentFolderSharedLink = GetSharedFolderLink(folders.Where(f => f.OwnerId == user.Id).ToList().First().SharedCode);
                    ViewBag.IsFolderPublic = folders.Where(f => f.OwnerId == user.Id).ToList().First().IsPublic;
                }
                else
                {
                    ViewBag.CurrentFolderShCode = folder.SharedCode;
                    ViewBag.CurrentFolderId = folder.Id;
                    ViewBag.CurrentFolderSharedLink = GetSharedFolderLink(folder.SharedCode);
                    ViewBag.IsFolderPublic = folder.IsPublic + "";
                }
                ViewBag.ImageSrc = GetFolderImageLink(folder);
                ViewBag.FolderTitle = folder.Name;
                if (TempData["MessageContent"] != null)
                {
                    ViewBag.MessageContent = TempData["MessageContent"];
                    ViewBag.MessageTitle = Resources.Resource.ERR_TITLE;
                }
                return View();
            }             
            catch
            {
                return RedirectToAction("Message", "Home", new { lang = getLocale() });
            }
        }

        public ActionResult PartialLibraryAccess(int folderId)
        {
            ViewBag.Localize = getLocale();
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());
                    if (user != null)
                    {
                        Folder folder = _entities.Folders.ToList().Where(f => f.Id == folderId).ToList().FirstOrDefault();
                        if (folder != null)
                        {
                            ViewBag.FolderLink = GetSharedFolderLink(folder.SharedCode);
                            ViewBag.AllowedUsers = _entities.UserShares.Where(w => w.FolderId == folder.Id).Select(s => (s.PersonId != null ? s.Person.Email : s.Email)).ToList();
                            ViewBag.AllowedUsersIds = _entities.UserShares.Where(w => w.FolderId == folder.Id).Select(s => s.PersonId != null ? s.Person.Id : s.Id).ToList();
                            ViewBag.AllGroups = _entities.PersonGroups.Where(w => w.PersonId == user.Id).Select(s => s.Name).ToList();
                            ViewBag.GroupsIDs = _entities.PersonGroups.Where(w => w.PersonId == user.Id).Select(s => s.Id).ToList();
                            ViewBag.GroupsAccess = _entities.PersonGroups.Where(w => w.PersonId == user.Id).Select(s => (_entities.GroupShares.Where(w => w.GroupId == s.Id && w.FolderId == folder.Id).Any()) ? true : false).ToList();
                        }
                    }
                    if (TempData["MessageContent"] != null)
                        ViewBag.MessageContent = TempData["MessageContent"];
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ViewBag.MessageContent = ex.Message;
                }
            }
            return PartialView("PartialLibraryAccess");
        }

        public ActionResult FolderAccessAddUser(string email, int folderId)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(getLocale());
            ViewBag.Localize = getLocale();
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    if (email.Length == 0)
                        throw new Exception(Resources.Resource.ERR_EMPTY_FIELD);
                    if (!IsValidEmail(email))
                        throw new Exception(Resources.Resource.ERR_EMAIL_NOT_VALID);                    
                    var personId = _entities.People.Where(w => w.Email == email).Select(s => s.Id).FirstOrDefault();
                    if (_entities.UserShares.Any(w => (w.Email == email || w.PersonId==personId)&&w.FolderId==folderId))
                        throw new Exception(Resources.Resource.ERR_USER_ALREDY);

                    ApplicationUserManager userManager =
                        System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>();
                    ApplicationUser user = userManager.FindById(User.Identity.GetUserId<int>());

                    if (user != null && user.Email==email)
                        throw new Exception(Resources.Resource.ERR_ADD_YOURSELF);
                    var folder = _entities.Folders.FirstOrDefault(w => w.Id == folderId);
                    var friend = _entities.People.FirstOrDefault(w => w.Email == email);
                    if (user != null && friend != null && !_entities.PersonFriends.Any(w => w.PersonId == user.Id && w.FriendId == friend.Id))
                    {
                        var personFriend = new PersonFriend() { PersonId = user.Id, FriendId = friend.Id };
                        _entities.PersonFriends.Add(personFriend);
                    }
                    if (personId != 0)
                    {
                        UserShare us = new UserShare { PersonId = personId, FolderId = folderId };
                        _entities.UserShares.Add(us);
                    }
                    else
                    {
                        UserShare us = new UserShare { FolderId = folderId, Email = email };
                        _entities.UserShares.Add(us);
                    }
                    userManager.EmailService.SendAsync(new IdentityMessage()
                    {
                        Body = String.Format(System.IO.File.ReadAllText(HttpContext.Server.MapPath("~/Emails/FolderSharing.html")),
                               GetSharedFolderLink(folder), user.Email, folder.Name),
                        Subject = "ScreenTaker shared folder",
                        Destination = email
                    });

                    if (folder != null)
                    {
                        foreach (var i in folder.Images)
                        {
                            var record =
                                _entities.UserShares.FirstOrDefault(
                                    w => w.ImageId == i.Id && (w.Email == email || w.Person.Email == email));
                            if (record != null)
                                _entities.UserShares.Remove(record);
                        }
                    }
                    _entities.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["MessageContent"] = ex.Message;
                }
            }
            return RedirectToAction("PartialLibraryAccess", new { folderId = folderId });
        }

        public ActionResult FolderAccessRemoveUser(string email, int folderId)
        {
            ViewBag.Localize = getLocale();
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {                   
                    UserShare record;
                    if (_entities.People.Any(w => w.Email == email))
                        record = _entities.UserShares.FirstOrDefault(w => w.Person.Email == email && w.FolderId == folderId);
                    else
                        record = _entities.UserShares.FirstOrDefault(w => w.Email == email && w.FolderId == folderId);
                    if (record != null)
                        _entities.UserShares.Remove(record);
                    _entities.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["MessageContent"] = ex.Message;
                }
            }
            return RedirectToAction("PartialLibraryAccess", new { folderId = folderId });
        }

        public ActionResult FolderAccessSwitchGroupsAccess(int groupId, int folderId)
        {
            ViewBag.Localize = getLocale();
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    var folder = _entities.Folders.Find(folderId);
                    var userManager = System.Web.HttpContext.Current.GetOwinContext()
                        .GetUserManager<ApplicationUserManager>();
                    var user = userManager.FindById(User.Identity.GetUserId<int>());
                    var group = _entities.PersonGroups.Find(groupId);
                    if (folder == null || group == null || !SecurityHelper.IsFolderEditable(user, folder.Person, folder, _entities))
                    {
                        throw new Exception("Image is not accessible");
                    }
                    var result = _entities.GroupShares.FirstOrDefault(w => w.GroupId == groupId && w.FolderId == folderId);
                    if (result != null)
                        _entities.GroupShares.Remove(result);
                    else
                    {
                        GroupShare us = new GroupShare { GroupId = groupId, FolderId = folderId };
                        var friends = group.GroupMembers;
                        foreach (var friend in friends)
                        {
                            userManager.SendEmail(friend.PersonId, "ScreenTaker shared folder",
                                String.Format(
                                    System.IO.File.ReadAllText(HttpContext.Server.MapPath("~/Emails/FolderSharing.html")),
                                    GetSharedFolderLink(folder), user.Email, folder.Name));
                        }
                        _entities.GroupShares.Add(us);
                    }
                    _entities.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["MessageContent"] = ex.Message;
                }
            }
            return RedirectToAction("PartialLibraryAccess", new { folderId = folderId });
        }

        public ActionResult PartialImagesAccess(int imageId)
        {
            ViewBag.Localize = getLocale();
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());
                    if (user != null)
                    {
                        Image image = _entities.Images.FirstOrDefault(w => w.Id == imageId);
                        if (image != null)
                        {
                            ViewBag.ImageSharedLink = GetSharedImageLink(image.SharedCode);
                            ViewBag.AllowedUsers = _entities.UserShares.Where(w => w.ImageId == image.Id).Select(s => (s.PersonId != null ? s.Person.Email : s.Email)).ToList().Concat(_entities.UserShares.Where(w => w.FolderId == image.FolderId).Select(s => (s.PersonId != null ? s.Person.Email : s.Email)).ToList()).ToList();
                            ViewBag.AllowedUsersIds = _entities.UserShares.Where(w => w.ImageId == image.Id).Select(s => s.PersonId != null ? s.Person.Id : s.Id).ToList().Concat(_entities.UserShares.Where(w => w.FolderId == image.FolderId).Select(s => (s.PersonId != null ? s.Person.Id : s.Id)).ToList()).ToList();
                            ViewBag.AreUserRightsInherited= _entities.UserShares.Where(w => w.ImageId == image.Id).Select(s => false).ToList().Concat(_entities.UserShares.Where(w => w.FolderId == image.FolderId).Select(s => true)).ToList<bool>();
                            ViewBag.AllGroups = _entities.PersonGroups.Where(w => w.PersonId == user.Id).Select(s => s.Name).ToList();
                            ViewBag.GroupsIDs = _entities.PersonGroups.Where(w => w.PersonId == user.Id).Select(s => s.Id).ToList();
                            ViewBag.GroupsAccess = _entities.PersonGroups.Where(w => w.PersonId == user.Id).Select(s => (_entities.GroupShares.Where(w => w.GroupId == s.Id && w.ImageId == image.Id).Any()) ? true : false).ToList();
                            ViewBag.AreGroupRightsInherited= _entities.PersonGroups.Where(w => w.PersonId == user.Id).Select(s => (_entities.GroupShares.Where(w => w.GroupId == s.Id && w.FolderId == image.FolderId).Any()) ? true : false).ToList<bool>();
                        }
                    }
                    if (TempData["MessageContent"] != null)
                        ViewBag.MessageContent = TempData["MessageContent"];
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ViewBag.MessageContent = ex.Message;
                }
            }
            return PartialView("PartialImagesAccess");            
        }

        public bool IsValidEmail(string email)
        {
            try
            {
                MailAddress m = new MailAddress(email);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public ActionResult ImageAccessAddUser(string email, int imageId)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(getLocale());
            ViewBag.Localize = getLocale();
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    if (email.Length == 0)
                        throw new Exception(Resources.Resource.ERR_EMPTY_FIELD);
                    if (!IsValidEmail(email))
                        throw new Exception(Resources.Resource.ERR_EMAIL_NOT_VALID);                    
                    var person = _entities.People.FirstOrDefault(w => w.Email == email);
                    if (person!=null && _entities.UserShares.Any(w => (w.Email == email || w.PersonId == person.Id) && w.ImageId == imageId))
                        throw new Exception(Resources.Resource.ERR_USER_ALREDY);
                    var image = _entities.Images.FirstOrDefault(w => w.Id == imageId);

                    if (image!=null &&_entities.UserShares.Any(w => w.FolderId == image.FolderId && (w.Email==email || w.Person.Email == email)))
                        throw new Exception(Resources.Resource.ERR_INHERITED);
                    ApplicationUserManager userManager =
                        System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>();
                    ApplicationUser user = userManager.FindById(User.Identity.GetUserId<int>());
                    if (user != null && user.Email == email)
                        throw new Exception(Resources.Resource.ERR_ADD_YOURSELF);
                    var friend = _entities.People.FirstOrDefault(w => w.Email == email);
                    if (user != null && friend != null && !_entities.PersonFriends.Any(w => w.PersonId == user.Id && w.FriendId == friend.Id))
                    {
                        var personFriend = new PersonFriend() { PersonId = user.Id, FriendId = friend.Id };
                        _entities.PersonFriends.Add(personFriend);
                    }
                    if (person != null && person.Id != 0)
                    {
                        UserShare us = new UserShare { PersonId = person.Id, ImageId = imageId };
                        _entities.UserShares.Add(us);
                    }
                    else
                    {
                        UserShare us = new UserShare { ImageId = imageId, Email = email };
                        _entities.UserShares.Add(us);
                    }
                    
                    _entities.SaveChanges();
                    transaction.Commit();

                    userManager.EmailService.SendAsync(new IdentityMessage()
                    {
                        Body = String.Format(System.IO.File.ReadAllText(HttpContext.Server.MapPath("~/Emails/ImageSharing.html")),
                                GetSharedImageLink(image), user.Email, image.Name),
                        Subject = "ScreenTaker shared image",
                        Destination = email
                    });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["MessageContent"] = ex.Message;
                }

            }
            return RedirectToAction("PartialImagesAccess", new { imageId = imageId });
        }
            
        public ActionResult ImageAccessRemoveUser(string email, int imageId)
        {
            ViewBag.Localize = getLocale();
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {                    
                    UserShare record=null;
                    if (_entities.People.Any(w => w.Email == email))
                        record = _entities.UserShares.FirstOrDefault(w => w.Person.Email == email && w.ImageId == imageId);
                    else
                        record = _entities.UserShares.FirstOrDefault(w => w.Email == email && w.ImageId == imageId);
                    if (record != null)
                        _entities.UserShares.Remove(record);
                    _entities.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["MessageContent"] = ex.Message;
                }
            }
            return RedirectToAction("PartialImagesAccess", new { imageId = imageId });
        }

        public ActionResult ImageAccessSwitchGroupsAccess(int groupId, int imageId)
        {
            ViewBag.Localize = getLocale();
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    var result = _entities.GroupShares.FirstOrDefault(w => w.GroupId == groupId && w.ImageId == imageId);
                    if (result != null)
                    {
                        _entities.GroupShares.Remove(result);
                    }
                    else
                    {
                        var image = _entities.Images.Find(imageId);
                        var userManager = System.Web.HttpContext.Current.GetOwinContext()
                            .GetUserManager<ApplicationUserManager>();
                        var user = userManager.FindById(User.Identity.GetUserId<int>());
                        var group = _entities.PersonGroups.Find(groupId);
                        if (image == null || group == null || !SecurityHelper.IsImageEditable(user, image.Folder.Person, image,_entities))
                        {
                            throw new Exception("Image is not accessible");
                        }
                        GroupShare us = new GroupShare {GroupId = groupId, ImageId = imageId};
                        var friends = group.GroupMembers;
                        foreach (var friend in friends)
                        {
                            userManager.SendEmail(friend.PersonId, "ScreenTaker shared image",
                                String.Format(
                                    System.IO.File.ReadAllText(HttpContext.Server.MapPath("~/Emails/ImageSharing.html")),
                                    GetSharedImageLink(image), user.Email, image.Name));
                        }
                        _entities.GroupShares.Add(us);
                    }
                    _entities.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["MessageContent"] = ex.Message;
                }
            }
            return RedirectToAction("PartialImageAccess", new { imageId = imageId});
        }
       
        public ActionResult MakeFolderPublicOrPrivate(int folderId, string lang = "en")
        {
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    ViewBag.Localize = getLocale();
                    var folder = _entities.Folders.FirstOrDefault(w => w.Id == folderId);
                    if (folder != null)
                    {
                        folder.IsPublic = !folder.IsPublic;
                        var images = _entities.Images.Where(i => i.FolderId == folder.Id).ToList();
                        foreach (var i in images)
                            i.IsPublic = folder.IsPublic;
                    }
                    _entities.SaveChanges();
                    ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext()
                    .GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());
                    var folders = _entities.Folders.ToList().Where(f => f.OwnerId == user.Id).ToList();
                    ViewBag.Folders = folders;
                    ViewBag.FolderLinkBASE = GetFolderLink("");
                    var sharedLinks = folders.ToList().Select(f => GetSharedFolderLink(f.SharedCode)).ToList();
                    ViewBag.Count = folders.Count;
                    ViewBag.Folders = folders;
                    ViewBag.SharedLinks = sharedLinks;
                    ViewBag.BASE_URL = GetBaseUrl() + "";
                    ViewBag.FolderID = folderId;
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["MessageContent"] = ex.Message;
                }
            }
            
            return PartialView("PartialFoldersChangeState");
        }      

        public ActionResult SharedLibrary(string lang = "en")
        {
            ViewBag.Localize = getLocale();
            ViewBag.Message = "Library page";
            ViewBag.FolderLinkBASE = GetSharedFolderLink("");
            ViewBag.UserAvatarBASE = getUserAvatarBASE();
            ViewBag.SharedImageBASE = GetSharedImageLink("");
            ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext()
                .GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());
            if (user == null)
            {
                return View("Message");
            }
            var imagesFolder = new Folder()
            {
                Id = 1,
                OwnerId = user.Id,
                SharedCode = "img",
                IsPublic = true,
                Name = "Images"
            };
            List<Folder> sharedFolders =
                (new[] {imagesFolder})
                    .Concat(
                        from userShare in _entities.UserShares
                        where userShare.PersonId == user.Id && userShare.FolderId != null
                        select userShare.Folder
                    ).Union(
                        from folder in _entities.Folders
                        join groupShare in _entities.GroupShares
                            on folder.Id equals groupShare.FolderId
                        join pGroup in _entities.PersonGroups
                            on groupShare.GroupId equals  pGroup.Id
                        join member in _entities.GroupMembers
                            on pGroup.Id equals member.GroupId
                        where member.PersonId == user.Id
                        select folder
                )
                .ToList();
            ViewBag.FolderLinks = sharedFolders.Select(GetSharedFolderLink).ToList();
            ViewBag.FolderImageLinks = sharedFolders.Select(f=>GetFolderImageLink(f)).ToList();
            ViewBag.Folders = sharedFolders;
            ViewBag.Owners = sharedFolders.Select(o => o.Person).ToList();

            ViewBag.UserId = user.Id;
            ViewBag.BASE_URL = GetBaseUrl() + "";
            ViewBag.FolderLinkBASE =  GetSharedFolderLink("");

            return View();
        }
        
        [HttpGet]
        public ActionResult ChangeFoldersAttr(Folder folder, string lang = "en")
        {
            ViewBag.Folders = _entities.Folders.ToList();
            ViewBag.Localize = getLocale();
            return RedirectToAction("Library");
        }
        #endregion
        private void FillImagesViewBag(string folderId)
        {            
            ViewBag.BASE_URL = GetBaseUrl() + "";
            ViewBag.SigleImageBASE = GetSingleImageLink("");            
            ViewBag.Localize = getLocale();                        
            var current = Convert.ToInt32(folderId);           
            var list = _entities.Images.Where(i => i.FolderId == current).ToList();
            ViewBag.Localize = getLocale();
            ViewBag.IsEmpty = !list.Any();
            ViewBag.Images = list;
            var pathsList = _entities.Images.ToList().Select(i => GetImagePath(i.SharedCode)).ToList();
            ViewBag.Paths = pathsList;
            ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext()
                .GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());
            if (user != null)
                ViewBag.UserFolders = _entities.Folders.Where(w => w.OwnerId == user.Id && w.Id.ToString()!=folderId).ToList();
            ViewBag.BASE_URL = GetBaseUrl()+"";
            ViewBag.SharedLinks = _entities.Images.ToList()
                .Select(i => GetSharedImageLink(i.SharedCode)).ToList();
            ViewBag.Count = "0";
            if(list.Count>0)
            ViewBag.Count = list.Count+"";
            ViewBag.FolderId = folderId;
            ViewBag.FirstId = list.Count > 0 ? (list.First().Id+""):"";
            ViewBag.FirstImageName = list.Count > 0 ? list.First().Name.Replace("'","#039"): "";

            ViewBag.FirstImageId = list.Count > 0 ? list.First().Id:-1;
            ViewBag.FirstImageShCode = list.Count > 0 ? list.First().SharedCode : "";
            ViewBag.FirstImageSrc = list.Count > 0 ? GetImagePathBASE()+ (list.First().ServerFolder == null ? "" : list.First().ServerFolder.SharedCode) + "/" + list.First().SharedCode+"_compressed.png" : "";
            ViewBag.FirstImageShLink = (list.Count > 0 ? GetSharedImageLink(list.First().SharedCode) : "");
            ViewBag.ImageIsPublic = (list.Count > 0 ? list.First().IsPublic+"" : "False");
            if(list.Count>0)
            ViewBag.ImageID = list.First().Id;
            else ViewBag.ImageID = 0;
        }
        
        public ActionResult Images(string id , string lang = "en")
        {
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    ViewBag.Localize = getLocale();
                    ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext()
                        .GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());
                    int folderId;
                    Int32.TryParse(id, out folderId);
                    Session["CurrentFolderId"] = folderId;
                    var folder = _entities.Folders.Find(folderId);
                    bool accesGranted = false;
                    if (folder != null)
                    {
                        accesGranted = SecurityHelper.IsFolderEditable(user, folder.Person, folder, _entities);
                    }
                    if (!accesGranted)
                    {
                        return RedirectToAction("Welcome");
                    }
                    var groups = from p in _entities.PersonGroups
                                 where p.PersonId == user.Id
                                 select new { ID = p.Id, Groups = p.Name };   
                    if (groups.Any())
                    {
                        ViewBag.Groups = groups.Select(s => s.Groups).ToList();
                        ViewBag.GroupsIDs = groups.Select(s => s.ID).ToList();
                    }
                    ViewBag.FolderName = folder.Name;
                    FillImagesViewBag(folderId.ToString());
                    if (TempData["MessageContent"] != null)
                    {
                        ViewBag.MessageTitle = Resources.Resource.ERR_TITLE;
                        ViewBag.MessageContent = TempData["MessageContent"];
                    }
                    ViewBag.SharedImageBASE = GetSharedImageLink("");
                    ViewBag.ImageLinkBASE = GetImagePathBASE();
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    return RedirectToAction("Message", "Home", new { lang = getLocale() });
                }
            }           
            return View("Images", new { lang = getLocale() });
        }

        [HttpPost]
        public ActionResult Images(HttpPostedFileBase file, string folderId, string lang = "en")
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(getLocale());
            ViewBag.Localize = getLocale();
            if (file != null)
            {
                using (var transaction = _entities.Database.BeginTransaction())
                {
                    try
                    {
                        ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());
                        var folder = _entities.Folders.Find(Int32.Parse(folderId));
                        bool accesGranted = false;
                        if (folder != null)                        
                            accesGranted = SecurityHelper.IsFolderEditable(user, folder.Person, folder, _entities);                       
                        if (!accesGranted)                        
                            return RedirectToAction("Welcome", new { lang = getLocale() });                                                         
                        if (!_imageCompressor.IsValid(file))
                            throw new Exception(Resources.Resource.ERR_IMAGE_NOT_VALID);                                            
                        var sharedCode = _stringGenerator.Next();
                        var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                        var serverFolderId = 0;               
                        var last = _entities.ServerFolder.Where(w=>w.Count<1000).ToList().LastOrDefault();
                        if (last == null||!System.IO.Directory.Exists(Server.MapPath("/img/")+last.SharedCode))
                        {
                            var serverFolder = new ServerFolder() { Count = 2, SharedCode = _stringGenerator.Next() };
                            _entities.ServerFolder.Add(serverFolder);
                            System.IO.Directory.CreateDirectory(Server.MapPath("/img/")+ serverFolder.SharedCode);
                            serverFolderId = serverFolder.Id;
                        }
                        else
                        {
                            serverFolderId = last.Id;
                            last.Count += 2;
                        }
                        var image = new Image
                        {
                            IsPublic = false,
                            FolderId = Int32.Parse(folderId),
                            SharedCode = sharedCode,
                            Name = fileName,
                            ServerFolderId = serverFolderId,
                            PublicationDate = DateTime.Now
                        };
                        _entities.Images.Add(image);
                        _entities.SaveChanges();
                        var bitmap = new Bitmap(file.InputStream);
                        var path = Path.Combine(Server.MapPath("~/img/"), image.ServerFolder.SharedCode + "/" + sharedCode + ".png");
                        bitmap.Save(path, ImageFormat.Png);
                        var compressedBitmap = _imageCompressor.Compress(bitmap, new Size(128, 128));
			            path = Path.Combine(Server.MapPath("~/img/"), image.ServerFolder.SharedCode + "/" + sharedCode + "_compressed.png");                        
                        compressedBitmap.Save(path, ImageFormat.Png);
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        TempData["MessageContent"] = ex.Message;
                    }
                }
            }
            FillImagesViewBag(folderId);
            return RedirectToAction("Images", new { id = Int32.Parse(folderId), lang = getLocale() });
        }
        public ActionResult MakeImagePublicOrPrivate(int imageId, string lang = "en")
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(getLocale());
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    ViewBag.Localize = getLocale();
                    var image = _entities.Images.FirstOrDefault(w => w.Id == imageId);                    
                    if (image != null)
                    {
                        ViewBag.ImageID = imageId;
                        ViewBag.ImageIsPublic = image.IsPublic + "";
                        ViewBag.ImageIsPublic = image.IsPublic + "";
                        FillImagesViewBag(image.FolderId.ToString());
                        FillSingleImageViewBag(image.SharedCode);
                        ViewBag.ImageLinkBASE = GetImagePathBASE();
                        ViewBag.SharedImageBASE = GetSharedImageLink("");
                        if (!image.Folder.IsPublic)
                            throw new Exception(Resources.Resource.ERR_PUBLIC_IN_PRIVATE);
                        image.IsPublic = !image.IsPublic;
                        ViewBag.ImageIsPublic = image.IsPublic + "";
                        FillImagesViewBag(image.FolderId.ToString());
                        FillSingleImageViewBag(image.SharedCode);
                        ViewBag.ImageLinkBASE = GetImagePathBASE();
                        ViewBag.SharedImageBASE = GetSharedImageLink("");
                    }                                        
                    _entities.SaveChanges();                    
                    transaction.Commit();                    
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ViewBag.MessageContent = ex.Message;
                }
            }
            return PartialView("PartialImagesChangeState");
        }

        public JsonResult MakeSingleImagePublicOrPrivate(int imageId, string lang = "en")
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(getLocale());
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    ViewBag.Localize = getLocale();
                    var image = _entities.Images.FirstOrDefault(w => w.Id == imageId);
                    if (image != null)
                    {
                        if (!image.Folder.IsPublic)
                            throw new Exception(Resources.Resource.ERR_PUBLIC_IN_PRIVATE);
                        image.IsPublic = !image.IsPublic;
                        
                        FillImagesViewBag(image.FolderId.ToString());
                        ViewBag.ImageID = imageId;
                        ViewBag.ImageIsPublic = image.IsPublic + "";
                        ViewBag.Image = image;
                        ViewBag.OriginalPath = GetImagePath(ViewBag.Image.SharedCode);
                    }
                    _entities.SaveChanges();
                    transaction.Commit();
                    return Json("", JsonRequestBehavior.AllowGet);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(ex.Message, JsonRequestBehavior.AllowGet);
                }
            }
        }

        [AllowAnonymous]
        public ActionResult SharedFolder(string f, string lang = "en")
        {
            ViewBag.Localize = getLocale();
            try
            {
                ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext()
                    .GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());

                List<Image> images = null;
                bool accessGranted = false;
                // get all images shared with user
                if (f == "img")
                {
                    if (user != null)
                    {
                        accessGranted = true;
                        images = (
                                from userShare in _entities.UserShares
                                where userShare.PersonId == user.Id && userShare.ImageId != null
                                select userShare.Image
                            ).Union(
                                from image in _entities.Images
                                join groupShare in _entities.GroupShares
                                    on image.Id equals groupShare.ImageId
                                join pGroup in _entities.PersonGroups
                                    on groupShare.GroupId equals pGroup.Id
                                join member in _entities.GroupMembers
                                    on pGroup.Id equals member.GroupId
                                where member.PersonId == user.Id
                                select image
                            ).ToList();

                        ViewBag.FolderName = "Shared images";
                    }
                }
                else
                {
                    var folder = _entities.Folders.FirstOrDefault(fold => fold.SharedCode == f);
                    if (folder != null)
                    {
                        int folderId = folder.Id;
                        ViewBag.FolderName = folder.Name;
                        accessGranted = SecurityHelper.IsFolderAccessible(user, folder.Person, folder, _entities);
                        if (accessGranted)
                        {
                            images = SecurityHelper.GetAccessibleImages(user, folder, _entities);
                        }
                    }
                }
                ViewBag.BASE_URL = GetBaseUrl();
                ViewBag.AccessGranted = accessGranted;
                ViewBag.SharedImageBASE = GetSharedImageLink("");
                ViewBag.ImagePathBASE = GetImagePathBASE();
                ViewBag.UserAvatarBASE = getUserAvatarBASE();
                if (accessGranted)
                {
                    ViewBag.IsEmpty = !images.Any();
                    ViewBag.Images = images;
                    ViewBag.Owners = images.Select(i => i.Folder.Person).ToList();
                    var pathsList = images
                        .Select(GetImageLink).ToList();
                    ViewBag.Paths = pathsList;

                    ViewBag.SharedLinks = images
                        .Select(GetSharedImageLink).ToList();

                    return View("SharedFolder", new { lang = getLocale() });
                }
                return View("Message", new { lang = getLocale() });
            }
            catch
            {
                return View("Message", new { lang = getLocale() });
            }
        }

        [HttpGet]
        public ActionResult SingleImage(string image, string lang = "en", int selectedId = -1)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(getLocale());
            ViewBag.Localize = getLocale();
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    
                    ViewBag.Image = _entities.Images.FirstOrDefault(i => i.SharedCode.Equals(image));
                    ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext()
                        .GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());
                    var im = _entities.Images.FirstOrDefault(i => i.SharedCode.Equals(image));
                    if (im == null)
                        throw new Exception("Null image error.");
                    if (user != null)
                        ViewBag.UserFolders = _entities.Folders.Where(w => w.OwnerId == user.Id && w.Id != im.FolderId).ToList();
                    ViewBag.CurrentFolderId = im.FolderId;
                    bool accesGranted = false;
                    if (im != null)
                    {
                        accesGranted = SecurityHelper.IsImageEditable(user, im.Folder.Person, im, _entities);

                        ViewBag.FolderName = im.Folder.Name;
                        ViewBag.FolderLink = GetFolderLink(im.FolderId.ToString());
                    }
                    if (!accesGranted)
                        return View("Message", new { lang = getLocale() });
                    FillSingleImageViewBag(image);
                    transaction.Commit();
                }                
                catch (Exception)
                {
                    transaction.Rollback();
                    return View("Message", new { lang = getLocale() });
                }
            }            
            return View("SingleImage", new { lang = getLocale() });
        }      
        
        private void FillSingleImageViewBag(string image)
        {
            ViewBag.Image = _entities.Images.FirstOrDefault(i => i.SharedCode.Equals(image));
            ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext()
                       .GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());
            
            if (ViewBag.Image == null && _entities.Images.ToList().Count > 0)
                ViewBag.Image = _entities.Images.ToList().First();
            ViewBag.OriginalPath = "";
            if (ViewBag.Image != null)
            {
                ViewBag.OriginalPath = GetImagePath(ViewBag.Image.SharedCode);
                ViewBag.CompressedPath =    GetImagePathBASE() + (ViewBag.Image.SharedCode == null ? "" : ViewBag.Image.ServerFolder.SharedCode + "/")  + ViewBag.Image.SharedCode + "_compressed.png";
                
            }
            if (ViewBag.Image != null)
                ViewBag.ImageSharedCode = ViewBag.Image.SharedCode;
            ViewBag.OriginalName = "";
            if (ViewBag.Image != null)
                ViewBag.OriginalName = ViewBag.Image.Name;
            ViewBag.ImageIsPublic = "False";
            if (ViewBag.Image != null)
                ViewBag.ImageIsPublic = ViewBag.Image.IsPublic + "";
            ViewBag.ImageTitle = "";
            if (ViewBag.Image != null)
                ViewBag.ImageTitle = ViewBag.Image.Name;
            ViewBag.Date = "";
            if (ViewBag.Image != null)
                ViewBag.Date = ViewBag.Image.PublicationDate;
            ViewBag.ImageId = "";
            if (ViewBag.Image != null)
            {
                ViewBag.ImageId = ViewBag.Image.Id;
                SingleImageId = ViewBag.Image.Id;
            }
            if (ViewBag.Image != null)
                ViewBag.SharedLink = GetSharedImageLink(ViewBag.Image.SharedCode);
            var emails = (from p in _entities.UserShares
                          join m in _entities.People
                              on p.PersonId equals m.Id
                          where p.ImageId == SingleImageId && p.Email == null
                          select new { Email = m.Email })
                .Union
                (from n in _entities.UserShares
                 where n.ImageId == SingleImageId && n.PersonId == null
                 select new { Email = n.Email });
            if (emails.Any())
                ViewBag.Emails = emails.Select(s => s.Email).ToList();
            var groups = from p in _entities.PersonGroups
                         where p.PersonId == user.Id
                         select new { ID = p.Id, Groups = p.Name };
            var flags = from p in _entities.GroupShares
                        where p.ImageId == SingleImageId
                        select p;
            if (groups.Any())
            {
                ViewBag.Groups = groups.Select(s => s.Groups).ToList();
                ViewBag.GroupsIDs = groups.Select(s => s.ID).ToList();
            }
        }          

        [AllowAnonymous]
        [HttpGet]
        public ActionResult SharedImage(string i, string lang = "en")
        {
            ViewBag.Localize = getLocale();
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());
                    var image = _entities.Images.FirstOrDefault(im => im.SharedCode.Equals(i));
                    bool accessGranted = false;
                    if (image != null)
                    {
                        accessGranted = SecurityHelper.IsImageAccessible(user, image.Folder.Person, image, _entities);
                        if (accessGranted)
                        {
                            ViewBag.ImageName = image.Name;
                            ViewBag.ImagePath = SecurityHelper.GetImagePath(GetBaseUrl() + "img", i);
                        }
                    }
                    if (!accessGranted)
                        return View("Message", new { lang = getLocale() });
                    ViewBag.AccessGranted = accessGranted;
                    ViewBag.Image = image;
                    if (ViewBag.Image == null && _entities.Images.ToList().Count > 0)
                    {
                        ViewBag.Image = _entities.Images.ToList().First();
                    }
                    ViewBag.Owner = ViewBag.Image == null ? null : ViewBag.Image.Folder.Person;
                    ViewBag.OriginalPath = "";
                    if (ViewBag.Image != null)
                    {
                        ViewBag.OriginalPath = GetImagePath(ViewBag.Image.SharedCode);
                    }
                    ViewBag.OriginalName = "";
                    if (ViewBag.Image != null)
                    {
                        ViewBag.OriginalName = ViewBag.Image.Name;
                    }
                    if (!accessGranted)
                        return View("Message", new { lang = getLocale() });
                    ViewBag.AccessGranted = accessGranted;
                    ViewBag.Image = image;
                    if (ViewBag.Image == null && _entities.Images.ToList().Count > 0)
                    {
                        ViewBag.Image = _entities.Images.ToList().First();
                    }
                    ViewBag.Owner = ViewBag.Image == null ? null : ViewBag.Image.Folder.Person;
                    ViewBag.OriginalPath = "";
                    if (ViewBag.Image != null)
                    {
                        ViewBag.OriginalPath = GetImagePath(ViewBag.Image.SharedCode);
                    }
                    ViewBag.OriginalName = "";
                    if (ViewBag.Image != null)
                    {
                        ViewBag.OriginalName = ViewBag.Image.Name;
                    }
                    ViewBag.UserAvatarBASE = getUserAvatarBASE();
                    transaction.Commit();                                        
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    return View("Message", new { lang = getLocale() });
                }
            }
            return View("SharedImage", new { lang = getLocale() });
        }

        public ActionResult DeleteImage(string path,string redirect = "false", string lang = "en")
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(getLocale());
            ViewBag.Localize = getLocale();
            int folderId = 0;

            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    if (path == null)
                        throw new Exception("Path is null");
                    var sharedDode = Path.GetFileNameWithoutExtension(path);

                    var serverFolder = _entities.ServerFolder.Where(w => w.Image.Where(ww => ww.SharedCode == sharedDode).Any()).FirstOrDefault();
                    if (serverFolder != null)
                    {
                        serverFolder.Count -= 2;
                    }
                    try
                    {
                        System.IO.File.Delete(Server.MapPath("~/img/") + (serverFolder==null?"":serverFolder.SharedCode) + "/" + Path.GetFileNameWithoutExtension(path) + ".png");
                        System.IO.File.Delete(Server.MapPath("~/img/") + (serverFolder == null ? "" : serverFolder.SharedCode) + "/" + Path.GetFileNameWithoutExtension(path) + "_compressed.png");
                    }
                    catch { }
                    
                    folderId = _entities.Images.FirstOrDefault(w => w.SharedCode == sharedDode).FolderId;
                    var obj = _entities.Images.FirstOrDefault(w => w.SharedCode == sharedDode);                    
                    string fId = obj.FolderId.ToString();
                    var userShare = obj.UserShares;
                    while (userShare.Count > 0)
                        _entities.UserShares.Remove(userShare.ElementAt(0));
                    var groupShare = obj.GroupShares;
                    while (groupShare.Count > 0)
                        _entities.GroupShares.Remove(groupShare.ElementAt(0));
                    _entities.Images.Remove(obj);                    
                    _entities.SaveChanges();
                    transaction.Commit();
                    FillImagesViewBag(fId);
                }
                catch (Exception ex)
                {
                    transaction.Commit();
                    ViewBag.MessageContent = ex.Message;
                }
            }            
            ViewBag.ImageLinkBASE = GetImagePathBASE();
            ViewBag.SharedImageBASE = GetSharedImageLink("");
            if (redirect=="true") return RedirectToAction("Images", new { id = folderId.ToString(), lang = getLocale() });
            else return PartialView("PartialImagesChangeState");
        }

        public ActionResult RenameImage(string path, string newName, string lang = "en")
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(getLocale());
            ViewBag.Localize = getLocale();
            if (ViewBag.Image == null && _entities.Images.ToList().Count > 0)
            {
                ViewBag.Image = _entities.Images.ToList().First();
            }
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {                    
                    ViewBag.ImageTitle = newName;
                    var sharedDode = Path.GetFileNameWithoutExtension(path);
                    var obj = _entities.Images.FirstOrDefault(w => w.SharedCode == sharedDode);                    
                    ViewBag.Image = obj;
                    if (newName.Length == 0)
                        throw new Exception(Resources.Resource.ERR_EMPTY_FIELD);

                    ViewBag.ImageID = obj.Id;
                    obj.Name = newName;
                    ViewBag.Image = obj;
                    
                    FillImagesViewBag(obj.FolderId.ToString());
                    FillSingleImageViewBag(obj.SharedCode);
                    ViewBag.FolderName = obj.Folder.Name;
                    _entities.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ViewBag.MessageContent = ex.Message;
                }
            }
            return PartialView("SingleImageChangeState");
        }

        public ActionResult AddFolder(string path, string title, string lang = "en")
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(getLocale());
            ViewBag.Localize = getLocale();
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {                   
                    ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());
                    var newolder = new Folder()
                    {
                        IsPublic = false,
                        OwnerId = user.Id,
                        SharedCode = _stringGenerator.Next(),
                        Name = title,
                        CreationDate = DateTime.Now
                    };
                    _entities.Folders.Add(newolder);
                                    
                    var folders = _entities.Folders.ToList().Where(f => f.OwnerId == user.Id).ToList();
                    ViewBag.Folders = folders;
                    ViewBag.BASE_URL = GetBaseUrl() + "";
                    ViewBag.FolderID = folders.Last().Id;
                    ViewBag.FolderLinkBASE = GetFolderLink("");
                    var sharedLinks = folders.ToList().Select(f => GetSharedFolderLink(f.SharedCode)).ToList();
                    ViewBag.Count = folders.Count;
                    ViewBag.SharedLinks = sharedLinks;
                    if (title.Length == 0)
                        throw new Exception(Resources.Resource.ERR_EMPTY_FIELD);
                    if (user != null && _entities.Folders.Where(w => w.Name == title && w.OwnerId == user.Id).Any())
                        throw new Exception(Resources.Resource.ERR_FOLDER_ALREDY);
                    _entities.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();                    
                    ViewBag.MessageContent = ex.Message;
                }
            }
            return PartialView("PartialFoldersChangeState");
        }

        public ActionResult RenameImageOutside(string path, string newName, string lang = "en")
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(getLocale());
            int folderId = 0;
            int imageId = 0;
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    if (newName.Length == 0)
                        throw new Exception(Resources.Resource.ERR_EMPTY_FIELD);
                    ViewBag.ImageTitle = newName;
                    var sharedCode = Path.GetFileNameWithoutExtension(path);                  
                    var obj = _entities.Images.Where(w=>w.SharedCode == sharedCode).FirstOrDefault();
                    if (obj == null)                   
                        throw new Exception("Path in invalid");
                    obj.Name = newName;
                    imageId = obj.Id;
                    folderId = _entities.Images.FirstOrDefault(w => w.SharedCode == sharedCode).FolderId;
                    _entities.SaveChanges();
                    FillImagesViewBag(obj.FolderId.ToString());
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ViewBag.MessageContent = ex.Message;
                }
            }
            
            if(imageId!=0)
            ViewBag.ImageID = imageId;
            ViewBag.ImageLinkBASE = GetImagePathBASE();
            ViewBag.SharedImageBASE = GetSharedImageLink("");
            return PartialView("PartialImagesChangeState");
        }

        public ActionResult DeleteFolder(string path, string lang = "en")
        {
            ViewBag.Localize = getLocale();

            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    var sharedСode = Path.GetFileNameWithoutExtension(path);
                    var obj = _entities.Folders.FirstOrDefault(w => w.SharedCode == sharedСode);
                    var images = obj.Images;                    
                    while (images.Count > 0)
                    {
                        try {
                            if(images.ElementAt(0).ServerFolder!= null)
                                images.ElementAt(0).ServerFolder.Count -= 2;
                            System.IO.File.Delete(Server.MapPath("~/img/") + (images.ElementAt(0).ServerFolder == null ? "" : images.ElementAt(0).ServerFolder.SharedCode) + "/" + images.ElementAt(0).SharedCode + ".png");
                            System.IO.File.Delete(Server.MapPath("~/img/") + (images.ElementAt(0).ServerFolder == null ? "" : images.ElementAt(0).ServerFolder.SharedCode) + "/" + images.ElementAt(0).SharedCode + "_compressed.png");
                        }catch { }
                        var im = images.ElementAt(0);
                        var userShareIm = im.UserShares;
                        while (userShareIm.Count > 0)
                            _entities.UserShares.Remove(userShareIm.ElementAt(0));
                        var groupShareIm = im.GroupShares;
                        while (groupShareIm.Count > 0)
                            _entities.GroupShares.Remove(groupShareIm.ElementAt(0));
                        _entities.Images.Remove(im);                        
                    }
                    var userShare = obj.UserShares;
                    while (userShare.Count > 0)
                        _entities.UserShares.Remove(userShare.ElementAt(0));
                    var groupShare = obj.GroupShares;
                    while (groupShare.Count > 0)
                        _entities.GroupShares.Remove(groupShare.ElementAt(0));
                    _entities.Folders.Remove(obj);
                            _entities.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                }
            }
            ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext()
                    .GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());
            var folders = _entities.Folders.ToList().Where(f => f.OwnerId == user.Id).ToList();
            ViewBag.Folders = folders;
            ViewBag.BASE_URL = GetBaseUrl() + "";
            ViewBag.FolderID = folders.First().Id;
            ViewBag.FolderLinkBASE = GetFolderLink("");
            var sharedLinks = folders.ToList().Select(f => GetSharedFolderLink(f.SharedCode)).ToList();
            ViewBag.Count = folders.Count;
            ViewBag.SharedLinks = sharedLinks;
            return PartialView("PartialFoldersChangeState");
        }

        public ActionResult RenameFolder(int folderId, string path, string newName, string lang = "en")
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(getLocale());
            ViewBag.Localize = getLocale();
            ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    string tmp = Resources.Resource.ERR_EMPTY_FIELD;
                    if (newName.Length == 0)
                        throw new Exception(tmp);
                    if (user != null && _entities.Folders.Where(w=>w.Id==folderId).FirstOrDefault().Name!=newName &&_entities.Folders.Where(w => w.Name == newName && w.OwnerId == user.Id).Any())
                        throw new Exception(Resources.Resource.ERR_FOLDER_ALREDY);
                    var sharedCode = Path.GetFileNameWithoutExtension(path);
                    var obj = _entities.Folders.FirstOrDefault(w => w.SharedCode == sharedCode);
                    obj.Name = newName;
                    _entities.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ViewBag.MessageContent = ex.Message;
                }
            }
            var folders = _entities.Folders.ToList().Where(f => f.OwnerId == user.Id).ToList();
            ViewBag.Folders = folders;
            ViewBag.FolderLinkBASE = GetFolderLink("");
            var sharedLinks = folders.ToList().Select(f => GetSharedFolderLink(f.SharedCode)).ToList();
            ViewBag.Count = folders.Count;
            ViewBag.Folders = folders;
            ViewBag.SharedLinks = sharedLinks;
            ViewBag.BASE_URL = GetBaseUrl() + "";
            ViewBag.FolderID = folderId;
            return PartialView("PartialFoldersChangeState");
        }

        public ActionResult MoveItMoveIt(int folderId,string imageSharedCode)
        {
            ViewBag.Localize = getLocale();
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    var imageId = _entities.Images.Where(w => w.SharedCode == imageSharedCode).Select(s => s.Id).FirstOrDefault();
                    var img = _entities.Images.Where(w => w.Id == imageId).FirstOrDefault();
                    img.FolderId = folderId;
                    _entities.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    return View("Message", new { lang = getLocale() });
                }
            }
            return RedirectToAction("Images",new {id= folderId.ToString() });
        }

        public ActionResult ImagesMoveCreateFolder(string name,int folderId)
        {
            ViewBag.Localize = getLocale();
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());
                    if (user != null)
                    {
                        var newolder = new Folder()
                        {
                            IsPublic = true,
                            OwnerId = user.Id,
                            SharedCode = _stringGenerator.Next(),
                            Name = name,
                            CreationDate = DateTime.Now
                        };
                        _entities.Folders.Add(newolder);
                        _entities.SaveChanges();
                    }
                    if (user != null)
                        ViewBag.UserFolders = _entities.Folders.Where(w => w.OwnerId == user.Id && w.Id != folderId).ToList();
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                }
            }
            return PartialView("PartialImagesMoveCreateFolder");
        }

        public ActionResult SingleImageMoveCreateFolder(string name, int folderId,string imageSharedCode)
        {
            ViewBag.Localize = getLocale();
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());
                    if (user != null)
                    {
                        var newolder = new Folder()
                        {
                            IsPublic = true,
                            OwnerId = user.Id,
                            SharedCode = _stringGenerator.Next(),
                            Name = name,
                            CreationDate = DateTime.Now
                        };
                        _entities.Folders.Add(newolder);
                        ViewBag.ImageSharedCode = imageSharedCode;
                        _entities.SaveChanges();
                    }
                    if (user != null)
                        ViewBag.UserFolders = _entities.Folders.Where(w => w.OwnerId == user.Id && w.Id != folderId).ToList();
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                }
           }
            return PartialView("PartialSingleImageMoveCreateFolder");
        }

        public FileResult Image(string i)
        {
            ViewBag.Localize = getLocale();
            return File(Request.RawUrl, "image/png");
        }
    }
}
