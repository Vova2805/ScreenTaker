using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ScreenTaker.Controllers
{
    public class API
    {
        public static string GetBaseUrl()
        {
            var request = HttpContext.Request;
            var appUrl = HttpRuntime.AppDomainAppVirtualPath;
            var baseUrl = string.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, appUrl);
            return baseUrl;
        }
    }
}