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
            try
            {
                ViewBag.PeopleForMaster = _entities.People.Select(s => s).ToList();
            }
            catch (Exception e) { }  
                       
        }
        protected string GetBaseUrl()
        {
            var request = HttpContext.Request;
            var appUrl = HttpRuntime.AppDomainAppVirtualPath;
            var baseUrl = string.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, appUrl);
            return baseUrl;
        }

        protected string GetFolderImageLink(Folder folder, bool isShared = false)
        {
            bool isFull = false;
            bool isPublic = false;
            if (folder != null)
            {
                isFull = folder.Images != null ? folder.Images.Count > 0 : false;
                isPublic = folder.Images != null ? folder.IsPublic : false;
            }

            string imageLink = GetBaseUrl() + "Resources/" + (isPublic ? "public" : "private");
            if (isShared)
            {
                imageLink += "_shared";
            }
            else
            if (isFull)
            {
                imageLink += "_full";
            }
            imageLink += ".png";
            return imageLink;
        }

        protected string getUserAvatar(string code)
        {
           return getUserAvatarBASE() + code + ".png";
        }
        protected string getUserAvatarBASE()
        {
            return GetBaseUrl() + "avatars/";
        }
        protected string GetImagePath(string code)
        {
            return GetBaseUrl() + "img/" + code + ".png";
        }
        protected string GetImagePathBASE()
        {
            return GetBaseUrl() + "img/";
        }

        protected string GetImageLink(Image image)
        {
            return GetImageLink(image.SharedCode);
        }

        protected string GetImageLink(string code)
        {
            return GetBaseUrl() + "img/" + code + ".png";
        }

        protected string GetFolderLink(string code)
        {
            return GetBaseUrl() + "Home/Images?id=" + code;
        }

        protected string GetSharedImageLink(Image image)
        {
            return GetSharedImageLink(image.SharedCode);
        }

        protected string GetSharedImageLink(string code)
        {
            return GetBaseUrl() + "Home/SharedImage?i=" + code;
        }

        protected string GetSharedFolderLink(Folder folder)
        {
            return GetSharedFolderLink(folder.SharedCode);
        }

        protected string GetSharedFolderLink(string code)
        {
            return GetBaseUrl() + "Home/SharedFolder?f=" + code;
        }

    }
}
