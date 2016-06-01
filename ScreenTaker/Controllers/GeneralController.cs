using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ScreenTaker.Controllers
{
    public abstract class GeneralController: Controller
    {
        public string locale = "en";
    }
}