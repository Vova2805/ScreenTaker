using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ScreenTaker.Models;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Ajax.Utilities;

namespace ScreenTaker.Controllers
{
    public class HomeController : Controller
    {
        private ScreenTakerDBEntities _entities = new ScreenTakerDBEntities();
        private ImageCompressor _imageCompressor = new ImageCompressor();
        private RandomStringGenerator _stringGenerator = new RandomStringGenerator()
        {
            Chars = "1234567890qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM",
            Length = 10
        };
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(HttpPostedFileBase file)
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
                        image.isPublic = false;
                        image.folderId = _entities.Folder.Where(f=>f.name.Equals("General")).Select(fol=>fol.id).FirstOrDefault();
                        image.sharedCode = sharedCode;
                        image.name = fileName;
                        image.publicationDate = DateTime.Now;
                        _entities.Image.Add(image);
                        _entities.SaveChanges();

                        transaction.Commit();

                        var bitmap = new Bitmap(file.InputStream);

                        var path = Path.Combine(Server.MapPath("~/img/"), sharedCode + ".png");
                        bitmap.Save(path, ImageFormat.Png);

                        var compressedBitmap = _imageCompressor.Compress(bitmap, new Size(128, 128));
                        path = Path.Combine(Server.MapPath("~/img/"), sharedCode + "_compressed.png");
                        compressedBitmap.Save(path, ImageFormat.Png);
                    }
                    catch(Exception e)
                    {
                        transaction.Rollback();
                    }
                }
                 
            }
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        #region Library
        public ActionResult Library()
        {
            ViewBag.Message = "Library page";
            ViewBag.Folders = _entities.Folder.ToList();
            ViewBag.FolderLink = GetBaseUrl() + _entities.Folder.ToList().ElementAt(0).sharedCode;
            return View();
        }

        [HttpGet]
        public ActionResult ChangeFoldersAttr(Folder folder)
        {
            ViewBag.Folders = _entities.Folder.ToList();

            return RedirectToAction("Library");
        }
        #endregion

        public ActionResult Images()
        {
            var list = _entities.Image.ToList();
            ViewBag.Images = list;
            var pathsList = _entities.Image.ToList().Select(i => GetBaseUrl() + "img/" + i.sharedCode ).ToList();
            ViewBag.Paths = pathsList;
            ViewBag.BASE_URL = GetBaseUrl() + "img/";
            return View();
        }

        public string GetBaseUrl()
        {
            var request = HttpContext.Request;
            var appUrl = HttpRuntime.AppDomainAppVirtualPath;
            var baseUrl = string.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, appUrl);
            return baseUrl;
           // return "http://screentaker.azurewebsites.net/";
        }

        [HttpGet]
        public ActionResult SingleImage(string image)
        {
            ViewBag.Image =  _entities.Image.FirstOrDefault(im => im.sharedCode.Equals(image));
            if(ViewBag.Image==null && _entities.Image.ToList().Count>0)
            {
                ViewBag.Image = _entities.Image.ToList().First();
            }
            ViewBag.OriginalPath = "";
            if (ViewBag.Image != null)
            {
                ViewBag.OriginalPath = GetBaseUrl()+"img/"+ViewBag.Image.sharedCode + ".png";
            }
            ViewBag.OriginalName = "";
            if (ViewBag.Image != null)
            {
                ViewBag.OriginalName = ViewBag.Image.name + ".png";
            }

            ViewBag.Date = "";
            if (ViewBag.Image != null)
            {
                ViewBag.Date = ViewBag.Image.publicationDate;
            }

            if (ViewBag.Image != null)
            {
                ViewBag.SharedLink = GetBaseUrl() + "Home/SharedImage?i=" + ViewBag.Image.sharedCode;
            }
            return View();
        }

        [HttpGet]
        public ActionResult SharedImage(string i)
        {
            var image = _entities.Image.FirstOrDefault(im => im.sharedCode.Equals(i));
            bool accesGranted = false;
            if (image != null)
            {
                accesGranted = true;
                if (accesGranted)
                {
                    ViewBag.ImageName = image.name;
                    ViewBag.ImagePath = GetBaseUrl() + "img/" + image.sharedCode + ".png";
                }
            }
            ViewBag.AccessGranted = accesGranted;
            return View();
        }

    }
}
