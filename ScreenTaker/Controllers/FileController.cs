using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ScreenTaker.Controllers
{
    public class FileController : Controller
    {
        // GET: File
        public FileResult Image()
        {
            return File(Request.RawUrl, "image/png");
        }
    }
}