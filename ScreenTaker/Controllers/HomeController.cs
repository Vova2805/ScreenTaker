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
                var image = new image();
                image.id = _entities.image.Max(s => s.id)+1;
                image.folderId = 0;
                image.isPublic = false;
                image.sharedCode = sharedCode;
                image.name = fileName;                
                image.publicationDate = new DateTime(2016, 1, 1);
                _entities.image.Add(image);
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
            ViewBag.Folders = _entities.folder.ToList();
            return View();
        }
       
        [HttpGet]
        public ActionResult ChangeFoldersAttr(folder folder)
        {
            return RedirectToAction("Library");
        }
        #endregion

        public ActionResult Images()
        {
            var list = _entities.image.ToList().Select(i=>i.name).ToList();
            ViewBag.Images = list;

            return View();
        }

        [HttpGet]
        public ActionResult SingleImage(int id)
        {
            string path = GetBaseUrl() + "img/" + _entities.image.ToList().ElementAt(id).sharedCode+".png";
            ViewBag.CurrentImagePath = path;
            ViewBag.CurrentImageTitle = _entities.image.ToList().ElementAt(id).name;
            return View();
        }

        public string GetBaseUrl()
        {
            var request = HttpContext.Request;
            var appUrl = HttpRuntime.AppDomainAppVirtualPath;
            var baseUrl = string.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, appUrl);
            return baseUrl;
        }
    }
}
