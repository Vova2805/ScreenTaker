using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ScreenTaker.Models;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq.Expressions;
using System.Net.Mail;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Image = ScreenTaker.Models.Image;
using System.Text.RegularExpressions;

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
            ViewBag.Localize = locale;
            return RedirectToAction("Welcome", "Home", new { lang = locale });
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult Welcome(string lang = "en")
        {
            ViewBag.Localize = locale;
            return View("Welcome", new { lang = locale });
        }

        [HttpPost]
        public ActionResult Welcome(HttpPostedFileBase file, string lang = "en")
        {
            int fId = -1;
            ViewBag.Localize = locale;
            if (file != null)
            {
                using (var transaction = _entities.Database.BeginTransaction())
                {
                    try
                    {
                        ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext()
                           .GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());
                        if (user == null)
                            return RedirectToAction("Account/Register", new { lang = locale });
                        var folder = _entities.Folders.Where(w=>w.OwnerId==user.Id).FirstOrDefault();
                        bool accesGranted = false;               
                        if (folder != null)                
                            accesGranted = SecurityHelper.IsFolderEditable(user, folder.Person, folder, _entities);                
                        else
                            return RedirectToAction("Account/Register", new { lang = locale });
                        if (!accesGranted)                
                            return RedirectToAction("Welcome", new { lang = locale });
                        fId = folder.Id;               
                        var sharedCode = _stringGenerator.Next();
                        var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                        var image = new Image();
                        image.IsPublic = false;
                        image.FolderId = folder.Id;
                        image.SharedCode = sharedCode;
                        image.Name = fileName;
                        image.PublicationDate = DateTime.Now;
                        _entities.Images.Add(image);
                        _entities.SaveChanges();
                        var bitmap = new Bitmap(file.InputStream);
                        var path = Path.Combine(Server.MapPath("~/img/"), sharedCode + ".png");
                        bitmap.Save(path, ImageFormat.Png);
                        var compressedBitmap = _imageCompressor.Compress(bitmap, new Size(128, 128));
                        path = Path.Combine(Server.MapPath("~/img/"), sharedCode + "_compressed.png");
                        compressedBitmap.Save(path, ImageFormat.Png);
                        transaction.Commit();
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                    }
                }
            }
            return RedirectToAction("Images", new { id = fId, lang = locale });
        }

        // GET: /Account/ConfirmEmail
        [AllowAnonymous]
        public ActionResult Message(string lang = "en")
        {
            ViewBag.Localize = locale;

            ViewBag.Email = "";
            return View();
        }

        [AllowAnonymous]
        public ActionResult About(string lang = "en")
        {
            ViewBag.Localize = locale;
            ViewBag.Message = "Your application description page.";

            return View("About", new { lang = locale });
        }

        [AllowAnonymous]
        public ActionResult Contact(string lang = "en")
        {
            ViewBag.Message = "Your contact page.";
            ViewBag.Localize = locale;
            return View();
        }

        #region Library
        public ActionResult Library(string lang = "en")
        {
            ViewBag.Localize = locale;
            ViewBag.Message = "Library page";

            ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext()
                .GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());
            ViewBag.UserId = user.Id;
            ViewBag.BaseURL = GetBaseUrl() + "";
            ViewBag.Folders = _entities.Folders.ToList().Where(f => f.OwnerId == user.Id).ToList();
           
            Folder folder = _entities.Folders.ToList().Where(f=> f.OwnerId == user.Id).ToList().FirstOrDefault(); 
            ViewBag.FolderLink = GetBaseUrl() + "Home/SharedFolder?f=" + folder.SharedCode;
            ViewBag.CurrentFolderShCode = folder == null ? (
                ViewBag.Folders.Count > 0 ? _entities.Folders.Where(f => f.OwnerId == user.Id).ToList().First().SharedCode : null
                ) : folder.SharedCode;
            ViewBag.ImageSrc = GetBaseUrl() + "Resources/" + (folder.IsPublic?"public.png":"private.png");
            ViewBag.FolderTitle = folder.Name;

            ViewBag.CurrentFolderId = (folder == null) ? (
               ViewBag.Folders.Count > 0 ? _entities.Folders.Where(f => f.OwnerId == user.Id).ToList().First().Id : -1
               ) : folder.Id;
            int currentFolderId = ViewBag.CurrentFolderId;
            ViewBag.IsFolderPublic = _entities.Folders.Where(f => f.OwnerId == user.Id).Select(w => w.IsPublic).FirstOrDefault();
            ViewBag.ButtonPrivateORPublic = "";
            ViewBag.ButtonPrivateORPublicMain = "";
            if (ViewBag.IsFolderPublic)
            {
                ViewBag.ButtonPrivateORPublic = "Turn Off";
                ViewBag.ButtonPrivateORPublicMain = "Make private";
            }
            else
            {
                ViewBag.ButtonPrivateORPublicMain = @Resources.Resource.MAKE_PUBLIC;
                ViewBag.ButtonPrivateORPublic = "Torn On";
            }                           
            return View();
        }

        public ActionResult PartialLibraryAccess(int folderId)
        {
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
                            ViewBag.FolderLink = GetBaseUrl() + "Home/SharedFolder?f=" + folder.SharedCode;
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
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    if (!IsValidEmail(email))
                        throw new Exception("Email is not valid");
                    if (email.Length == 0)
                        throw new Exception("Email shouldn't be empty");
                    var personID = _entities.People.Where(w => w.Email == email).Select(s => s.Id).FirstOrDefault();
                    if (personID != 0)
                    {
                        UserShare us = new UserShare { PersonId = personID, FolderId = folderId };
                        _entities.UserShares.Add(us);
                    }
                    else
                    {
                        UserShare us = new UserShare { FolderId = folderId, Email = email };
                        _entities.UserShares.Add(us);
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
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    if (!IsValidEmail(email))
                        throw new Exception("Email is not valid");
                    UserShare record;
                    if (_entities.People.Where(w => w.Email == email).Any())
                        record = _entities.UserShares.Where(w => w.Person.Email == email && w.FolderId == folderId).FirstOrDefault();
                    else
                        record = _entities.UserShares.Where(w => w.Email == email && w.FolderId == folderId).FirstOrDefault();
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
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    var result = _entities.GroupShares.Where(w => w.GroupId == groupId && w.FolderId == folderId).FirstOrDefault();
                    if (result != null)
                        _entities.GroupShares.Remove(result);
                    else
                    {
                        GroupShare us = new GroupShare { GroupId = groupId, FolderId = folderId };
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
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());
                    if (user != null)
                    {
                        Image image = _entities.Images.Where(w => w.Id == imageId).FirstOrDefault();
                        if (image != null)
                        {
                            ViewBag.ImageSharedLink = GetBaseUrl() + "Home/SharedImage?i=" + image.SharedCode;
                            ViewBag.AllowedUsers = _entities.UserShares.Where(w => w.ImageId == image.Id).Select(s => (s.PersonId != null ? s.Person.Email : s.Email)).ToList();
                            ViewBag.AllowedUsersIds = _entities.UserShares.Where(w => w.ImageId == image.Id).Select(s => s.PersonId != null ? s.Person.Id : s.Id).ToList();
                            ViewBag.AllGroups = _entities.PersonGroups.Where(w => w.PersonId == user.Id).Select(s => s.Name).ToList();
                            ViewBag.GroupsIDs = _entities.PersonGroups.Where(w => w.PersonId == user.Id).Select(s => s.Id).ToList();
                            ViewBag.GroupsAccess = _entities.PersonGroups.Where(w => w.PersonId == user.Id).Select(s => (_entities.GroupShares.Where(w => w.GroupId == s.Id && w.ImageId == image.Id).Any()) ? true : false).ToList();
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

        public bool IsValidEmail(string e_mail)
        {
            string expr = "[.\\-_a-z0-9]+@([a-z0-9][\\-a-z0-9]+\\.)+[a-z]{2,6}";
            Match isMatch = Regex.Match(e_mail, expr, RegexOptions.IgnoreCase);
            return isMatch.Success;
        }

        public ActionResult ImageAccessAddUser(string email, int imageId)
        {
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    if (!IsValidEmail(email))
                        throw new Exception("Email is not valid");
                    if (email.Length == 0)
                        throw new Exception("Email shouldn't be empty");
                    var personID = _entities.People.Where(w => w.Email == email).Select(s => s.Id).FirstOrDefault();
                    if (personID != 0)
                    {
                        UserShare us = new UserShare { PersonId = personID, ImageId = imageId };
                        _entities.UserShares.Add(us);
                    }
                    else
                    {
                        UserShare us = new UserShare { ImageId = imageId, Email = email };
                        _entities.UserShares.Add(us);
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
            return RedirectToAction("PartialImagesAccess", new { imageId = imageId });
        }
            
        public ActionResult ImageAccessRemoveUser(string email, int imageId)
        {
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    if (!IsValidEmail(email))
                        throw new Exception("Email is not valid");
                    UserShare record=null;
                    if (_entities.People.Where(w => w.Email == email).Any())
                        record = _entities.UserShares.Where(w => w.Person.Email == email&&w.ImageId==imageId).FirstOrDefault();
                    else
                        record = _entities.UserShares.Where(w => w.Email == email && w.ImageId == imageId).FirstOrDefault();
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
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    var result = _entities.GroupShares.Where(w => w.GroupId == groupId && w.ImageId == imageId).FirstOrDefault();
                    if (result != null)
                        _entities.GroupShares.Remove(result);
                    else
                    {
                        GroupShare us = new GroupShare { GroupId = groupId, ImageId = imageId };
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
            ViewBag.Localize = locale;

            // var sharedСode = Path.GetFileNameWithoutExtension(path);
            var result = _entities.Folders.FirstOrDefault(w => w.Id == folderId);

            if (result.IsPublic)

                result.IsPublic = false;

            else
                result.IsPublic = true;

            _entities.SaveChanges();


            return RedirectToAction("Library", new { lang = locale });
        }      

        public ActionResult SharedLibrary(string lang = "en")
        {
            ViewBag.Localize = locale;
            ViewBag.Message = "Library page";

            ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext()
                .GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());
            ViewBag.UserId = user.Id;
            ViewBag.BaseURL = GetBaseUrl() + "";
            ViewBag.Folders = _entities.Folders.Where(f => f.OwnerId == user.Id).ToList();
            ViewBag.FolderLink = GetBaseUrl() + _entities.Folders.ToList().ElementAt(0).SharedCode;
            Folder folder = _entities.Folders.ToList().Where(f =>f.OwnerId == user.Id).FirstOrDefault();
            ViewBag.CurrentFolderShCode = folder == null ? (
                ViewBag.Folders.Count > 0 ? _entities.Folders.Where(f => f.OwnerId == user.Id).ToList().First().SharedCode : null
                ) : folder.SharedCode;
            ViewBag.CurrentFolderId = (folder == null) ? (
               ViewBag.Folders.Count > 0 ? _entities.Folders.Where(f => f.OwnerId == user.Id).ToList().First().Id : -1
               ) : folder.Id;
            return View();
        }

        [HttpGet]
        public ActionResult ChangeFoldersAttr(Folder folder, string lang = "en")
        {
            ViewBag.Folders = _entities.Folders.ToList();
            ViewBag.Localize = locale;
            return RedirectToAction("Library");
        }
        #endregion

        private void FillImagesViewBag(int folderId)
        {
            var list = _entities.Images.Where(i => i.FolderId == folderId).ToList();
            ViewBag.Localize = locale;
            ViewBag.IsEmpty = !list.Any();
            ViewBag.Images = list;
            var pathsList = _entities.Images.ToList()
                .Select(i => GetBaseUrl() + "img/" + i.SharedCode).ToList();
            ViewBag.Paths = pathsList;
            ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext()
                .GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());
            if (user != null)
                ViewBag.UserFolders = _entities.Folders.Where(w => w.OwnerId == user.Id&&w.Id!=folderId).ToList();
            ViewBag.BASE_URL = GetBaseUrl();
            ViewBag.SharedLinks = _entities.Images.ToList()
                .Select(i => GetBaseUrl() + "Home/SharedImage?i=" + i.SharedCode).ToList();
            ViewBag.Count = "0";
            if(list.Count>0)
            ViewBag.Count = list.Count+"";
            ViewBag.FolderId = folderId;
            ViewBag.FirstId = list.Count > 0 ? (list.First().Id+"") : "-1";
            ViewBag.FirstImageName = list.Count > 0 ? (list.First().Name + ".png"): "";
            ViewBag.FirstWithoutEx = list.Count > 0 ? (list.First().Name+"") : "";
            int length = ViewBag.FirstImageName.Length;
            int size = length <= 15 ? length : 15;
            string name = ViewBag.FirstImageName.Substring(0, size);
            ViewBag.FirstImageName = name;

            ViewBag.FirstImageId = list.Count > 0 ? list.First().Id:-1;
            ViewBag.FirstImageShCode = list.Count > 0 ? list.First().SharedCode : "";
            ViewBag.FirstImageSrc = list.Count > 0 ? GetBaseUrl()+"img/"+list.First().SharedCode+"_compressed.png" : "";
            ViewBag.FirstImageShLink =  (list.Count > 0 ? GetBaseUrl() + "Home/SharedImage?i=" + list.First().SharedCode: "");
            
        }
            
        public ActionResult Images(string id = "-1", string lang = "en")
        {
            ViewBag.Localize = locale;
            ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext()
                .GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());

            int folderId = Int32.Parse(id);

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

            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    var groups = from p in _entities.PersonGroups
                                 where p.PersonId == user.Id
                                 select new { ID = p.Id, Groups = p.Name };
                    //var flags = from p in _entities.GroupShares
                    //            where p.FolderId == /*current folderid*/
                    //            select p;

                    if (groups.Any())
                    {
                        ViewBag.Groups = groups.Select(s => s.Groups).ToList();
                        ViewBag.GroupsIDs = groups.Select(s => s.ID).ToList();

                    }
                    //if (flags.Any())
                    //    ViewBag.Flags = groups.Select(w=> flags.Select(s=>s.GroupId).Contains(w.ID)?"Allowed": "Denied").ToList();

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                }
            }


           

            ViewBag.FolderName = folder.Name;
            FillImagesViewBag(folderId);
            return View("Images", new { lang = locale });
        }

        [HttpPost]
        public ActionResult Images(HttpPostedFileBase file, string folderId, string lang = "en")
        {
            ViewBag.Localize = locale;
            if (file != null)
            {
                ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext()
                    .GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());

                var folder = _entities.Folders.Find(Int32.Parse(folderId));

                bool accesGranted = false;

                if (folder != null)
                {
                    accesGranted = SecurityHelper.IsFolderEditable(user, folder.Person, folder, _entities);
                }

                if (!accesGranted)
                {
                    return RedirectToAction("Welcome", new { lang = locale });
                }

                using (var transaction = _entities.Database.BeginTransaction())
                {
                    try
                    {
                        var sharedCode = _stringGenerator.Next();
                        var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                        var image = new Image
                        {
                            IsPublic = false,
                            FolderId = Int32.Parse(folderId),
                            SharedCode = sharedCode,
                            Name = fileName,
                            PublicationDate = DateTime.Now
                        };
                        _entities.Images.Add(image);
                        _entities.SaveChanges();
                        var bitmap = new Bitmap(file.InputStream);
                        var path = Path.Combine(Server.MapPath("~/img/"), sharedCode + ".png");
                        bitmap.Save(path, ImageFormat.Png);
                        var compressedBitmap = _imageCompressor.Compress(bitmap, new Size(128, 128));
                        path = Path.Combine(Server.MapPath("~/img/"), sharedCode + "_compressed.png");
                        compressedBitmap.Save(path, ImageFormat.Png);
                        transaction.Commit();
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                    }
                }


            }
            FillImagesViewBag(Int32.Parse(folderId));

            return RedirectToAction("Images", new { id = Int32.Parse(folderId), lang = locale });
        }


       


        public ActionResult MakeImagePublicOrPrivate(int imageId, string lang = "en")
        {
            ViewBag.Localize = locale;

            // var sharedСode = Path.GetFileNameWithoutExtension(path);
            var result = _entities.Images.FirstOrDefault(w => w.Id == imageId);

            if (result.IsPublic)

                result.IsPublic = false;

            else
                result.IsPublic = true;

            _entities.SaveChanges();


            return RedirectToAction("Images", new { lang = locale });
        }

      

        [AllowAnonymous]
        public ActionResult SharedFolder(string f, string lang = "en")
        {
            ViewBag.Localize = locale;
            ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext()
                .GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());

            var folder = _entities.Folders.FirstOrDefault(fold => fold.SharedCode == f);
            
            bool accessGranted = false;

            if (folder != null)
            {
                int folderId = folder.Id;
                accessGranted = SecurityHelper.IsFolderAccessible(user, folder.Person, folder, _entities);

                if (accessGranted)
                {
                    List<Image> images = SecurityHelper.GetAccessibleImages(user, folder, _entities);


                    ViewBag.IsEmpty = !images.Any();
                    ViewBag.Images = images;
                    var pathsList = _entities.Images
                        .Select(GetImageLink).ToList();

                    ViewBag.Paths = pathsList;
                    ViewBag.BASE_URL = GetBaseUrl();
                    ViewBag.SharedLinks = _entities.Images
                        .Select(GetSharedImageLink).ToList();

                }
            }
            ViewBag.AccessGranted = accessGranted;
            if (accessGranted)
            {
                return View("SharedFolder", new { lang = locale });
            }
            return View("Message", new { lang = locale });
        }

        public string GetBaseUrl()
        {
            var request = HttpContext.Request;
            var appUrl = HttpRuntime.AppDomainAppVirtualPath;
            var baseUrl = string.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, appUrl);
            return baseUrl;
        }

        [HttpGet]
        public ActionResult SingleImage(string image, string lang = "en", int selectedId = -1)
        {
            ViewBag.Localize = locale;
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
                ViewBag.FolderLink = GetBaseUrl() + "Home/Images?id=" + im.FolderId;
            }

            if (!accesGranted)
            {
                return View("Message", new { lang = locale });
            }

            if (ViewBag.Image == null && _entities.Images.ToList().Count > 0)
            {
                ViewBag.Image = _entities.Images.ToList().First();
            }

            ViewBag.OriginalPath = "";
            if (ViewBag.Image != null)
            {
                ViewBag.OriginalPath = GetBaseUrl() + "img/" + ViewBag.Image.SharedCode + ".png";
            }

            ViewBag.OriginalName = "";
            if (ViewBag.Image != null)
            {
                ViewBag.OriginalName = ViewBag.Image.Name + ".png";
            }
            ViewBag.OriginalNameWithoutEx = "";
            if (ViewBag.Image != null)
            {
                int length = ViewBag.Image.Name.Length;
                int size = length <= 15 ? length : 15;
                string name = ViewBag.Image.Name.Substring(0, size);
                ViewBag.OriginalNameWithoutEx = name;
            }
            ViewBag.ImageTitle = "";
            if (ViewBag.Image != null)
            {
                ViewBag.ImageTitle = ViewBag.Image.Name;
            }

            ViewBag.Date = "";
            if (ViewBag.Image != null)
            {
                ViewBag.Date = ViewBag.Image.PublicationDate;
            }

            ViewBag.IsPublic = "";
            if (ViewBag.Image != null)
            {
                ViewBag.IsPublic = ViewBag.Image.IsPublic;
            }

            ViewBag.ImageId = "";
            if (ViewBag.Image != null)
            {
                ViewBag.ImageId = ViewBag.Image.Id;
                SingleImageId = ViewBag.Image.Id;
            }

            ViewBag.ButtonPrivateORPublic = "";
            if (ViewBag.Image != null)
            {
                if (ViewBag.Image.IsPublic)
                {
                    ViewBag.ButtonPrivateORPublicMain = "Make private";
                    ViewBag.ButtonPrivateORPublic = "Turn Off";
                }
                else
                {
                    ViewBag.ButtonPrivateORPublicMain = Resources.Resource.MAKE_PUBLIC;
                    ViewBag.ButtonPrivateORPublic = "Turn On";
                }
            }

            if (ViewBag.Image != null)
            {
                ViewBag.SharedLink = GetBaseUrl() + "Home/SharedImage?i=" + ViewBag.Image.SharedCode;
            }


            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
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

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                }
            }

            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
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
                    //if (flags.Any())
                    //    ViewBag.Flags = groups.Select(w => flags.Select(s => s.GroupId).Contains(w.ID) ? "Allowed" : "Denied").ToList();

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                }
            }
            return View("SingleImage", new { lang = locale });
        }                

        [AllowAnonymous]
        [HttpGet]
        public ActionResult SharedImage(string i, string lang = "en")
        {
            ViewBag.Localize = locale;
            ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext()
                .GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());


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
                return View("Message", new { lang = locale });

            ViewBag.AccessGranted = accessGranted;

            ViewBag.Image = image;
            if (ViewBag.Image == null && _entities.Images.ToList().Count > 0)
            {
                ViewBag.Image = _entities.Images.ToList().First();
            }
            ViewBag.OriginalPath = "";
            if (ViewBag.Image != null)
            {
                ViewBag.OriginalPath = GetBaseUrl() + "img/" + ViewBag.Image.SharedCode + ".png";
            }
            ViewBag.OriginalName = "";
            if (ViewBag.Image != null)
            {
                ViewBag.OriginalName = ViewBag.Image.Name + ".png";
            }
            return View("SharedImage", new { lang = locale });
        }

        private string GetImagePath(string code)
        {
            return GetBaseUrl() + "img" + code + ".png";
        }

        private string GetImageLink(Image image)
        {
            return GetImageLink(image.SharedCode);
        }

        private string GetImageLink(string code)
        {
            return GetBaseUrl() + "img/" + code + ".png";
        }

        private string GetFolderLink(string code)
        {
            return GetBaseUrl() + "Home/Images?id=" + code;
        }

        private string GetSharedImageLink(Image image)
        {
            return GetSharedImageLink(image.SharedCode);
        }

        private string GetSharedImageLink(string code)
        {
            return GetBaseUrl() + "Home/SharedImage?i=" + code;
        }

        private string GetSharedFolderLink(Folder folder)
        {
            return GetSharedFolderLink(folder.SharedCode);
        }

        private string GetSharedFolderLink(string code)
        {
            return GetBaseUrl() + "Home/SharedFolder?f=" + code;
        }

        public ActionResult DeleteImage(string path, string lang = "en")
        {
            ViewBag.Localize = locale;
            int folderId = 0;
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    if (path == null)
                        throw new Exception("Path is null");
                    var sharedDode = Path.GetFileNameWithoutExtension(path);             
                    folderId = _entities.Images.FirstOrDefault(w => w.SharedCode == sharedDode).FolderId;
                    var obj = _entities.Images.FirstOrDefault(w => w.SharedCode == sharedDode);
                    _entities.Images.Remove(obj);
                    _entities.SaveChanges();
                    System.IO.File.Delete(Server.MapPath("~/img/") + Path.GetFileName(path));
                    System.IO.File.Delete(Server.MapPath("~/img/") + Path.GetFileNameWithoutExtension(path) + "_compressed.png");

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Commit();
                }
            }
            return RedirectToAction("Images", new { id = folderId.ToString(), lang = locale });
        }

        public ActionResult RenameImage(string path, string newName, string lang = "en")
        {
            ViewBag.Localize = locale;
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    ViewBag.ImageTitle = newName;
                    var sharedDode = Path.GetFileNameWithoutExtension(path);
                    var obj = _entities.Images.FirstOrDefault(w => w.SharedCode == sharedDode);
                    obj.Name = newName;
                    _entities.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                }
            }
            return RedirectToAction("SingleImage", new { image = Path.GetFileNameWithoutExtension(path), lang = locale });
        }

        public ActionResult AddFolder(string path, string title, string lang = "en")
        {
            ViewBag.Localize = locale;
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    if (title.Length == 0)
                        throw new Exception("Title should not be empty.");
                    ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());

                    var newolder = new Folder()
                    {
                        IsPublic = true,
                        OwnerId = user.Id,
                        SharedCode = _stringGenerator.Next(),
                        Name = title,
                        CreationDate = DateTime.Now
                    };
                    _entities.Folders.Add(newolder);
                    _entities.SaveChanges();

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();                    
                    TempData["MessageContent"] = ex.Message;
                }
            }
            return RedirectToAction("Library", "Home", new { lang = locale });
        }

        public ActionResult RenameImageOutside(string path, string newName, string lang = "en")
        {
            ViewBag.Localize = locale;
            int folderId = 0;
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    ViewBag.ImageTitle = newName;
                    var sharedCode = Path.GetFileNameWithoutExtension(path);                  
                    var obj = _entities.Images.Where(w=>w.SharedCode == sharedCode).FirstOrDefault();
                    if (obj == null)                   
                        throw new Exception("Path in invalid");
                    obj.Name = newName;
                    folderId = _entities.Images.FirstOrDefault(w => w.SharedCode == sharedCode).FolderId;
                    _entities.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                }
            }
            return RedirectToAction("Images", new { id = folderId.ToString(), lang = locale });
        }

        public ActionResult DeleteFolder(string path, string lang = "en")
        {
            ViewBag.Localize = locale;
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    var sharedСode = Path.GetFileNameWithoutExtension(path);
                    var obj = _entities.Folders.FirstOrDefault(w => w.SharedCode == sharedСode);
                    var images = obj.Images;
                    while (images.Count > 0)
                    {
                        System.IO.File.Delete(Server.MapPath("~/img/") + images.ElementAt(0).SharedCode + ".png");
                        System.IO.File.Delete(Server.MapPath("~/img/") + images.ElementAt(0).SharedCode + "_compressed.png");
                        _entities.Images.Remove(images.ElementAt(0));
                        _entities.SaveChanges();
                    }
                    _entities.Folders.Remove(obj);
                    _entities.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                }
            }
            return RedirectToAction("Library", new { lang = locale });
        }

        public ActionResult RenameFolder(string path, string newName, string lang = "en")
        {
            ViewBag.Localize = locale;
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    var sharedCode = Path.GetFileNameWithoutExtension(path);
                    var obj = _entities.Folders.FirstOrDefault(w => w.SharedCode == sharedCode);
                    obj.Name = newName;
                    _entities.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                }
            }
            return RedirectToAction("Library", new { lang = locale });
        }

        public ActionResult MoveItMoveIt(int folderId,string imageSharedCode)
        {
            ViewBag.Localize = locale;
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
                }
            }
            return RedirectToAction("Images",new {id= folderId });
        }

        public ActionResult ImagesMoveCreateFolder(string name,int folderId)
        {
            ViewBag.Localize = locale;
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
            ViewBag.Localize = locale;
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
            return File(Request.RawUrl, "image/png");
        }

    }
}
