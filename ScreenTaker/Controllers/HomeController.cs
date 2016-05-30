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
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Image = ScreenTaker.Models.Image;
namespace ScreenTaker.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private ScreenTakerEntities _entities = new ScreenTakerEntities();
        private ImageCompressor _imageCompressor = new ImageCompressor();
        private RandomStringGenerator _stringGenerator = new RandomStringGenerator()
        {
            Chars = "1234567890qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM",
            Length = 15
        };
        public ActionResult Index(string lang = "en")
        {
            return RedirectToAction("Welcome", "Home");
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult Welcome(string lang = "en")
        {
            return View();
        }

        [HttpPost]
        public ActionResult Welcome(HttpPostedFileBase file, string lang = "en")
        {
            if (file != null)
            {
                using (var transaction = _entities.Database.BeginTransaction())
                {
                    try
                    {
                        var sharedCode = _stringGenerator.Next();
                        var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                        var image = new Image();
                        //if (_entities.Image.ToList().Count > 0)
                        //    image.id = _entities.Image.Max(s => s.id) + 1;
                        //else image.id = 1;

                        image.IsPublic = false;
                        image.FolderId = _entities.Folders.Where(f=>f.Name.Equals("General")).Select(fol=>fol.Id).FirstOrDefault();
                        image.SharedCode = sharedCode;
                        image.Name = fileName;
                        image.PublicationDate = DateTime.Now;
                        _entities.Images.Add(image);
                        _entities.SaveChanges();

                        transaction.Commit();

                        var bitmap = new Bitmap(file.InputStream);

                        var path = Path.Combine(Server.MapPath("~/img/"), sharedCode + ".png");
                        bitmap.Save(path, ImageFormat.Png);

                        var compressedBitmap = _imageCompressor.Compress(bitmap, new Size(128, 128));
                        path = Path.Combine(Server.MapPath("~/img/"), sharedCode + "_compressed.png");
                        compressedBitmap.Save(path, ImageFormat.Png);
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                    }
                }

            }
            return View();
        }

        [AllowAnonymous]
        public ActionResult About(string lang = "en")
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        [AllowAnonymous]
        public ActionResult Contact(string lang = "en")
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        #region Library
        public ActionResult Library(string lang = "en")
        {
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

            ViewBag.CurrentFolderId = (folder == null )? (
               ViewBag.Folders.Count > 0 ? _entities.Folders.Where(f => f.OwnerId == user.Id).ToList().First().Id : -1
               ) : folder.Id;
            return View();
        }

        [HttpGet]
        public ActionResult ChangeFoldersAttr(Folder folder, string lang = "en")
        {
            ViewBag.Folders = _entities.Folders.ToList();

            return RedirectToAction("Library");
        }
        #endregion

        private void FillImagesViewBag(int folderId)
        {
            var list = _entities.Images.Where(i => i.FolderId == folderId).ToList();

            ViewBag.IsEmpty = !list.Any();
            ViewBag.Images = list;            
            var pathsList = _entities.Images.ToList()
                .Select(i => GetBaseUrl() + "img/" + i.SharedCode).ToList();
            ViewBag.Paths = pathsList;
            ViewBag.BASE_URL = GetBaseUrl();
            ViewBag.SharedLinks = _entities.Images.ToList()
                .Select(i => GetBaseUrl() + "Home/SharedImage?i=" + i.SharedCode).ToList();

            ViewBag.FolderId = folderId;
        }

        public ActionResult Images(string id, string lang = "en")
        {
            if (id == null)
            {
                return RedirectToAction("Welcome");
            }

            FillImagesViewBag(Int32.Parse(id));

            return View();
        }

        [HttpPost]
        public ActionResult Images(HttpPostedFileBase file, string folderId, string lang = "en")
        {
            if (file != null)
            {
                using (var transaction = _entities.Database.BeginTransaction())
                {
                    try
                    {
                        var sharedCode = _stringGenerator.Next();
                        var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                        var image = new Image();                
                        image.IsPublic = false;
                        image.FolderId = Int32.Parse(folderId);
                        image.SharedCode = sharedCode;
                        image.Name = fileName;
                        image.PublicationDate = DateTime.Now;
                        _entities.Images.Add(image);
                        _entities.SaveChanges();

                        transaction.Commit();

                        var bitmap = new Bitmap(file.InputStream);

                        var path = Path.Combine(Server.MapPath("~/img/"), sharedCode + ".png");
                        bitmap.Save(path, ImageFormat.Png);

                        var compressedBitmap = _imageCompressor.Compress(bitmap, new Size(128, 128));
                        path = Path.Combine(Server.MapPath("~/img/"), sharedCode + "_compressed.png");
                        compressedBitmap.Save(path, ImageFormat.Png);
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                    }
                }

            }
            FillImagesViewBag(Int32.Parse(folderId));

            return View();
        }

        public ActionResult SharedFolder(string id, string lang = "en")
        {
            //if (id == null)
            //{
            //    return RedirectToAction("Welcome");
            //}

            int folderId = Int32.Parse(id);
            var list = _entities.Images.Where(i => i.FolderId == folderId).ToList();

            ViewBag.IsEmpty = !list.Any();
            ViewBag.Images = list;
            var pathsList = _entities.Images.ToList()
                .Select(i => GetBaseUrl() + "img/" + i.SharedCode).ToList();
            ViewBag.Paths = pathsList;
            ViewBag.BASE_URL = GetBaseUrl();
            ViewBag.SharedLinks = _entities.Images.ToList()
                .Select(i => GetBaseUrl() + "Home/SharedImage?i=" + i.SharedCode).ToList();

            return View();
        }

        public string GetBaseUrl()
        {
            var request = HttpContext.Request;
            var appUrl = HttpRuntime.AppDomainAppVirtualPath;
            var baseUrl = string.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, appUrl);
            return baseUrl;
        }

        [HttpGet]
        public ActionResult SingleImage(string image, string lang = "en")
        {
            ViewBag.Image =  _entities.Images.FirstOrDefault(im => im.SharedCode.Equals(image));
            if(ViewBag.Image==null && _entities.Images.ToList().Count>0)
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

            if (ViewBag.Image != null)
            {
                ViewBag.SharedLink = GetBaseUrl() + "Home/SharedImage?i=" + ViewBag.Image.SharedCode;
            }
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult SharedImage(string i)
        {
            var image = _entities.Images.FirstOrDefault(im => im.SharedCode.Equals(i));
            bool accesGranted = false;
            if (image != null)
            {
                accesGranted = true;
                if (accesGranted)
                {
                    ViewBag.ImageName = image.Name;
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
            return View();
        }
        public ActionResult DeleteImage(string path, string lang = "en")
        {
            int folderId = 0;
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {                    
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
                    transaction.Rollback();
                }
            }
            return RedirectToAction("Images", new { id = folderId.ToString() });
        }

        public ActionResult RenameImage(string path, string newName, string lang = "en")
        {
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    ViewBag.ImageTitle = newName;
                    var sharedDode = Path.GetFileNameWithoutExtension(path);
                    var obj=_entities.Images.FirstOrDefault(w => w.SharedCode == sharedDode);
                    obj.Name = newName;
                    _entities.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                }
            }
            return RedirectToAction("SingleImage", new { image = Path.GetFileNameWithoutExtension(path) });
        }

        public ActionResult AddFolder(string path, string title, string lang = "en")
        {
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
            return RedirectToAction("Library","Home");
        }

        public ActionResult RenameImageOutside(string path, string newName, string lang = "en")
        {
            int folderId = 0;
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    ViewBag.ImageTitle = newName;
                    var sharedDode = Path.GetFileNameWithoutExtension(path);
                    var obj = _entities.Images.FirstOrDefault(w => w.SharedCode == sharedDode);
                    obj.Name = newName;
                    folderId = _entities.Images.FirstOrDefault(w => w.SharedCode == sharedDode).FolderId;
                    _entities.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                }
            }
            return RedirectToAction("Images", new { id=folderId.ToString() });
        }

        public ActionResult DeleteFolder(string path, string lang = "en")
        {
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    var sharedDode = Path.GetFileNameWithoutExtension(path);
                    var obj = _entities.Images.FirstOrDefault(w => w.SharedCode == sharedDode);
                    _entities.Images.Remove(obj);
                    _entities.SaveChanges();
                    System.IO.File.Delete(Server.MapPath("~/img/") + Path.GetFileName(path));
                    System.IO.File.Delete(Server.MapPath("~/img/") + Path.GetFileNameWithoutExtension(path) + "_compressed.png");
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                }
            }
            return RedirectToAction("Images");
        }

        public ActionResult RenameFolder(string path, string newName, string lang = "en")
        {
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
            return RedirectToAction("SingleImage", new { image = Path.GetFileNameWithoutExtension(path) });
        }
    }
}
