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
        public ScreenTakerDBEntities connection = new ScreenTakerDBEntities();
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(HttpPostedFileBase file)
        {
            if (file != null)
            {
                var fileName = Path.GetFileName(file.FileName);
                var path = Path.Combine(Server.MapPath("~/img/"), fileName);
                file.SaveAs(path);
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

            ViewBag.Folders = connection.folder.ToList();

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
            var list = connection.image.ToList().Select(i=>i.name).ToList();
            ViewBag.Images = list;

            return View();
        }

        [HttpGet]
        public ActionResult SingleImage(int id)
        {
            string path = GetBaseUrl() + "img/" + connection.image.ToList().ElementAt(id).name;
            ViewBag.CurrentImagePath = path;
            ViewBag.CurrentImageTitle = connection.image.ToList().ElementAt(id).name;
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