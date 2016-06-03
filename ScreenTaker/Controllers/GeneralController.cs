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
        public string locale = "en";
        public GeneralController()
        {
            var _entities = new ScreenTakerEntities();
            if (User == null) return;
            ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());
            if (user != null)
            {
                var person = _entities.People.Where(w => w.Email == user.Email).FirstOrDefault();
                if (person.AvatarFile != null && System.IO.File.Exists(Server.MapPath("~/avatars/") + person.AvatarFile + "_50.png"))
                    ViewBag.Avatar_50 = GetBaseUrl() + "/avatars/" + person.AvatarFile + "_50.png";
                else
                    ViewBag.Avatar_50 = GetBaseUrl() + "/Resources/user.png";
            }
            ViewBag.PeopleForMaster = _entities.People.Select(s => s).ToList();
            ViewBag.BaseUrl = GetBaseUrl()+"";
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
