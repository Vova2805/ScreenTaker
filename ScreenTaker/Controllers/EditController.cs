
using ScreenTaker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Globalization;
using System.Threading;
using System.Web.WebPages;

namespace ScreenTaker.Controllers
{
    public class EditController : Controller
    {
        public ActionResult Index(string lang = "en")
        {
            return View("UserGroups");
        }

        public ActionResult UserGroups(string lang = "en")
        {
            //if (!Request.QueryString["lang"].IsEmpty())
            //{
            //    Culture = UICulture = Request.QueryString["lang"];
            //}
            //else
            //{
            //    Culture = UICulture = "en";
            //}
            return View();
        }
    }
}