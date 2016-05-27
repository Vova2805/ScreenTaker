
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
            return View();
        }

        public void ChangeLocalization(string request,string lang = "en")
        {
            if(request.Contains("?"))
            {
                if(request.Contains("lang"))
                {
                    int index = request.IndexOf("lang=");
                    string sub = request.Substring(index, 7);
                    request = request.Replace(sub,"lang="+lang);
                }
                else
                {
                    request += "&lang=" + lang;
                }
            }
            else
            {
                request += "?lang=" + lang;
            }
             Response.Redirect(request);
        }
    }
}