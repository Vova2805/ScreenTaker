using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ScreenTaker.Controllers
{
    public class FileController : GeneralController
    {
        // GET: File
        public FileResult Image()
        {
            ViewBag.Localize = locale;
            return File(Request.RawUrl, "image/png");
        }
    }
}