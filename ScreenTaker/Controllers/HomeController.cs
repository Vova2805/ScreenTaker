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
            ViewBag.Folders = _entities.Folders.Where(f => f.OwnerId == user.Id).ToList();
            ViewBag.FolderLink = GetBaseUrl() + _entities.Folders.ToList().ElementAt(0).SharedCode;
            Folder folder = _entities.Folders.ToList().Where(f => f.Name.Equals("General") && f.OwnerId == user.Id).FirstOrDefault();
            ViewBag.CurrentFolderShCode = folder == null ? (
                ViewBag.Folders.Count > 0 ? _entities.Folders.Where(f => f.OwnerId == user.Id).ToList().First().SharedCode : null
                ) : folder.SharedCode;

            ViewBag.CurrentFolderId = (folder == null) ? (
               ViewBag.Folders.Count > 0 ? _entities.Folders.Where(f => f.OwnerId == user.Id).ToList().First().Id : -1
               ) : folder.Id;
            return View();
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
            Folder folder = _entities.Folders.ToList().Where(f => f.Name.Equals("General") && f.OwnerId == user.Id).FirstOrDefault();
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

            ViewBag.FolderId = folderId;
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

        [AllowAnonymous]
        public ActionResult SharedFolder(string f, string lang = "en")
        {
            ViewBag.Localize = locale;
            ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext()
                .GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());

            var folder = _entities.Folders.First(fold => fold.SharedCode == f);
            
            bool accessGranted = false;

            if (folder != null)
            {
                int folderId = folder.Id;
                accessGranted = SecurityHelper.IsFolderAccessible(user, folder.Person, folder, _entities);

                if (accessGranted)
                {
                    var images = _entities.Images.Where(i => 
                        i.FolderId == folderId
                        && (folder.OwnerId == user.Id || i.IsPublic)
                    ).ToList();


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
                return RedirectToAction("Welcome", new { lang = locale });
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
                return RedirectToAction("Welcome");
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

            ViewBag.Id = "";
            if (ViewBag.Image != null)
            {
                ViewBag.Id = ViewBag.Image.Id;
            }

            ViewBag.ButtonPrivateORPublic = "";
            if (ViewBag.Image != null)
            {
                if (ViewBag.Image.IsPublic) ViewBag.ButtonPrivateORPublic = "Make private";
                else ViewBag.ButtonPrivateORPublic = "Make public";
            }

            if (ViewBag.Image != null)
            {
                ViewBag.SharedLink = GetBaseUrl() + "Home/SharedImage?i=" + ViewBag.Image.SharedCode;
            }


            return View("SingleImage", new { lang = locale });
        }


        //для одного зображення зміна доступу
        public ActionResult MakeSingleImagePublic(bool imagestatus, int imageId, string lang = "en")
        {
            ViewBag.Localize = locale;
            var result = _entities.Images.FirstOrDefault(b => b.Id == imageId);
            if (result != null)
            {
                if (imagestatus)
                    result.IsPublic = false;
                else
                    result.IsPublic = true;

                _entities.SaveChanges();
            }

            return RedirectToAction("SingleImage", new { lang = locale });
        }


        [AllowAnonymous]
        [HttpGet]
        public ActionResult SharedImage(string i, string lang = "en")
        {
            ViewBag.Localize = locale;
            ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext()
                .GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());


            var image = _entities.Images.FirstOrDefault(im => im.SharedCode.Equals(i));
            bool accesGranted = false;
            if (image != null)
            {
                accesGranted = SecurityHelper.IsImageAccessible(user, image.Folder.Person, image, _entities);

                if (accesGranted)
                {
                    ViewBag.ImageName = image.Name;
                    ViewBag.ImagePath = SecurityHelper.GetImagePath(GetBaseUrl() + "img", i);
                }
            }
            ViewBag.AccessGranted = accesGranted;

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
                catch (Exception)
                {
                    transaction.Rollback();
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
