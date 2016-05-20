using System;
using System.Collections.Generic;
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

        [HttpPost]
        public ActionResult ChangeFoldersAttr(Folder folder)
        {
            // modify dbc
            folders[folder.BookId] = folder;

            return RedirectToAction("Library");
        }
    }
}