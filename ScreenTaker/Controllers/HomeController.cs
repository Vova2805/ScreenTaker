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

            ViewBag.Folders = folders;

            return View();
        }

        // todo: database context
        public struct Folder
        {
            public int BookId { get; set; }
            public string Title { get; set; }
            public bool IsPrivate { get; set; }

            public Folder(int bookId, string title, bool isPrivate)
            {
                BookId = bookId;
                Title = title;
                IsPrivate = isPrivate;
            }
        }
        // represent db context
        List<Folder> folders = new List<Folder>()
            {
                new Folder(0, "My Photo", false),
                new Folder(1, "Fishing", true),
                new Folder(2, "Job", false),
                new Folder(3, "Friends", false),
                new Folder(4, "Kids", true),
                new Folder(5, "Summer-2015", false),
                new Folder(6, "Africa", true),
                new Folder(7, "Encapsulation", true)
            };

        [HttpGet]
        public ActionResult ChangeFoldersAttr(Folder folder)
        {
            // modify dbc
            folders[folder.BookId] = folder;

            return RedirectToAction("Library");
        }
        #endregion

        public class MyImage
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Source { get; set; }

            public MyImage(int id, string name, string source)
            {
                Id = id;
                Name = name;
                Source = source;
            }
        }

        public static List<string> GetImageList()
        {
            var l = Directory.GetFiles(System.Web.HttpContext.Current.Server.MapPath("~/img/")).ToList<string>();
            l = l.Select(s => "~/img/" + Path.GetFileName(s)).ToList<string>();
            return l;
        }

        private List<string> images = GetImageList(); 

        public ActionResult Images()
        {
            ViewBag.ImagePath = images;

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
        public ActionResult SingleImage()
        {
            ViewBag.Image = GetBaseUrl();
            return View();
        }
    }
}