using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ScreenTaker.Controllers
{
    public class HomeController : Controller
    {
        ScreenTakerDBEntities _entities = new ScreenTakerDBEntities();
        public ActionResult Index()
        {
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

            ViewBag.Folders = _entities.folder.ToList(); ;

            return View();
        }

        [HttpGet]
        public ActionResult ChangeFoldersAttr(folder folder)
        {
            //folders[folder.BookId] = folder;
            ViewBag.Folders = _entities.folder.ToList();

            return RedirectToAction("Library");
        }
        #endregion

        public static List<string> GetImageList()
        {
            var l = Directory.GetFiles(System.Web.HttpContext.Current.Server.MapPath("~/img/")).ToList<string>();
            l = l.Select(s => "~/img/" + Path.GetFileName(s)).ToList<string>();
            return l;
        }

        public ActionResult Images()
        {
            ViewBag.ImagePath = _entities.image.ToList().Select(im=>GetBaseUrl()+"img/"+im.name+".png").ToList();

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