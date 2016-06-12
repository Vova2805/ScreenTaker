using System.Web.Mvc;

namespace ScreenTaker.Controllers
{
    public class FileController : GeneralController
    {
        // GET: File
        public FileResult Image()
        {
            ViewBag.Localize = getLocale();
            return File(Request.RawUrl, "image/png");
        }
    }
}