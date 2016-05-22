using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ScreenTaker.Models;
using System.Drawing;
using System.Drawing.Imaging;

namespace ScreenTaker.Controllers
{
    public class HomeController : Controller
    {
        private ScreenTakerDBEntities _entities = new ScreenTakerDBEntities();

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
                var sharedCode = _stringGenerator.Next();
                var bitmap = new Bitmap(file.InputStream);
                var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                var path = Path.Combine(Server.MapPath("~/img/"), sharedCode + ".png");
                bitmap.Save(path, ImageFormat.Png);
                var image = new Image();
//                image.id = _entities.Image.Max(s => s.id)+1;
                image.folderId = 1;
                image.isPublic = false;
                image.sharedCode = sharedCode;
                image.name = fileName;                
                image.publicationDate = new DateTime(2016, 1, 1);
                _entities.Image.Add(image);
                _entities.SaveChanges();
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
            ViewBag.Folders = _entities.Folder.ToList(); ;

            return View();
        }

        [HttpGet]
        public ActionResult ChangeFoldersAttr(Folder folder)
        {
            //folders[folder.BookId] = folder;
            ViewBag.Folders = _entities.Folder.ToList();

            return RedirectToAction("Library");
        }
        #endregion

//        public static List<string> GetImageList()
//        {
//<<<<<<< HEAD
//            var l = Directory.GetFiles(System.Web.HttpContext.Current.Server.MapPath("~/img/")).ToList<string>();
//            l = l.Select(s => "~/img/" + Path.GetFileName(s)).ToList<string>();
//            return l;
//=======
//            var pathsList = _entities.image.ToList().Select(i => GetBaseUrl() + "img/" + i.sharedCode + "_compressed.png").ToList();
//            ViewBag.Paths = pathsList;
//
//        }

        public ActionResult Images()
        {
            var list = _entities.Image.ToList().Select(i => i.name).ToList();
            ViewBag.Images = list;
            var pathsList = _entities.Image.ToList().Select(i => GetBaseUrl() + "img/" + i.sharedCode + "_compressed.png").ToList();
            ViewBag.Paths = pathsList;
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
        public ActionResult SingleImage(string image)
        {
            ViewBag.Image = image;
            return View();
        }
    }
}
