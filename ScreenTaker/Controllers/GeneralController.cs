using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using ScreenTaker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ScreenTaker.Controllers
{
    public abstract class GeneralController : Controller
    {
        public static string locale = "en";
        public GeneralController()
        {
            var _entities = new ScreenTakerEntities();            
            ViewBag.PeopleForMaster = _entities.People.Select(s => s).ToList();            
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
