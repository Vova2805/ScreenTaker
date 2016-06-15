using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using ScreenTaker.Models;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Globalization;

namespace ScreenTaker.Controllers
{
    [Authorize]
    public class AccountController : GeneralController
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        private ScreenTakerEntities _entities = new ScreenTakerEntities();
        private ImageCompressor _imageCompressor = new ImageCompressor();


        private RandomStringGenerator _stringGenerator = new RandomStringGenerator()
        {
            Chars = "1234567890qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM",
            Length = 15
        };

        public AccountController()
        {
        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.Localize = getLocale();
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(getLocale());
            ViewBag.Localize = getLocale();
            ViewBag.ErrorTitle = null;
            if (!ModelState.IsValid)
            {
                ViewBag.ErrorTitle = Resources.Resource.INVALID_LOGIN_ATTEMPT;

                return View(model);
            }

            // Require the user to have a confirmed email before they can log on.
            var user = await UserManager.FindByNameAsync(model.Email);
            if (user != null)
            {
                if (!await UserManager.IsEmailConfirmedAsync(user.Id))
                {
                    string callbackUrl = await SendEmailConfirmationTokenAsync(user, "Confirm your account");

                    ViewBag.ErrorTitle = Resources.Resource.INVALID_LOGIN_ATTEMPT;

                    return View(model);
                }
            }

            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, change to shouldLockout: true

            var result = await SignInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, shouldLockout: false);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                case SignInStatus.Failure:
                default:
                { }

                ViewBag.ErrorTitle = Resources.Resource.INVALID_LOGIN_ATTEMPT;
                return View(model);
            }
        }

        //
        // GET: /Account/VerifyCode
        [AllowAnonymous]
        public async Task<ActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)
        {
            ViewBag.Localize = getLocale();
            // Require that the user has already logged in via username/password or external login
            if (!await SignInManager.HasBeenVerifiedAsync())
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            ViewBag.Localize = getLocale();
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes. 
            // If a user enters incorrect codes for a specified amount of time then the user account 
            // will be locked out for a specified amount of time. 
            // You can configure the account lockout settings in IdentityConfig
            var result = await SignInManager.TwoFactorSignInAsync(model.Provider, model.Code, isPersistent: model.RememberMe, rememberBrowser: model.RememberBrowser);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(model.ReturnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid code.");
                    return View(model);
            }
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            ViewBag.Localize = getLocale();
            return View();
        }
        string EMAIL = "";
        [AllowAnonymous]
        public async Task<ActionResult> ResendConfirmation(string email)
        {
            ViewBag.Localize = getLocale();

            var user = UserManager.FindByEmail(email);
            if (user != null)
            {
                ViewBag.Email = email;
                EMAIL = ViewBag.Email;
                string callbackUrl = await SendEmailConfirmationTokenAsync(user, "Confirm your account");
            }
            return View("ConfirmEmailInfo");
        }
        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            ViewBag.Localize = getLocale();
            ViewBag.ErrorTitle = null;

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email, };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    //await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);

                    string callbackUrl = await SendEmailConfirmationTokenAsync(user, "Confirm your account");

                    ViewBag.Email = user.Email;
                    EMAIL = ViewBag.Email;
                    //return RedirectToAction("Index", "Home");

                    return View("ConfirmEmailInfo");
                    // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                    // Send an email with this link
                    // string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    // var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                    // await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

                }
                if (result.Errors.Any())
                    ViewBag.ErrorTitle = result.Errors.FirstOrDefault();
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ConfirmEmail
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(int userId, string code, string lang = "en")
        {
            ViewBag.Localize = getLocale();

            if (userId == default(int) || code == null)
            {
                return View("Error");
            }
            var result = await UserManager.ConfirmEmailAsync(userId, code);
            if (result.Succeeded)
            {
                using (var entities = new ScreenTakerEntities())
                {
                    var defaultFolder = new Folder()
                    {
                        IsPublic = true,
                        OwnerId = userId,
                        SharedCode = _stringGenerator.Next(),
                        Name = "Public",
                        CreationDate = DateTime.Now
                    };
                    entities.Folders.Add(defaultFolder);
                    var user = entities.People.Find(userId);
                    if (user != null)
                    {
                        var userShares = entities.UserShares.Where(w => w.Email == user.Email);
                        foreach (var u in userShares)
                            u.PersonId = user.Id;
                    }                    
                    entities.SaveChanges();

                }
                return View("ConfirmEmail");
            }
            AddErrors(result);
            ViewBag.Email = UserManager.FindById(userId).Email;
            EMAIL = ViewBag.Email;
            return View();
        }

        //
        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            ViewBag.Localize = getLocale();
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            ViewBag.Localize = getLocale();
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByNameAsync(model.Email);
                if (user == null || !(await UserManager.IsEmailConfirmedAsync(user.Id)))
                {
//                    Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                // Send an email with this link
                string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);

                await UserManager.SendEmailAsync(user.Id, "Password Reset",
                    String.Format(System.IO.File.ReadAllText(HttpContext.Server.MapPath("~/Emails/ResetPassword.html")),
                            callbackUrl, user.Email));

//                await UserManager.SendEmailAsync(user.Id, "Password Reset", String.Format(Resources.Resource.RESET_PASSWORD_EMAIL, callbackUrl));
                return RedirectToAction("ForgotPasswordConfirmation", "Account");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            ViewBag.Localize = getLocale();
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            ViewBag.Localize = getLocale();
            return code == null ? View("Error") : View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            try {
                ViewBag.Localize = getLocale();
                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                var user = await UserManager.FindByNameAsync(model.Email);
                if (user == null)
                {
                    // Don't reveal that the user does not exist
                    return RedirectToAction("ResetPasswordConfirmation", "Account");
                }
                var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
                if (result.Succeeded)
                {
                    return RedirectToAction("ResetPasswordConfirmation", "Account");
                }
                AddErrors(result);
                return View();
            }
            catch
            {
                return RedirectToAction("Message", "Home", new { lang = getLocale() });
            }
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            ViewBag.Localize = getLocale();
            return View();
        }

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            ViewBag.Localize = getLocale();
            // Request a redirect to the external login provider
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/SendCode
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl, bool rememberMe)
        {
            try
            {
                ViewBag.Localize = getLocale();
                var userId = await SignInManager.GetVerifiedUserIdAsync();
                if (userId == default(int))
                {
                    return View("Error");
                }
                var userFactors = await UserManager.GetValidTwoFactorProvidersAsync(userId);
                var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
                return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
            }
            catch
            {
                return RedirectToAction("Message", "Home", new { lang = getLocale() });
            }
        }

        //
        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendCode(SendCodeViewModel model)
        {
            try
            {
                ViewBag.Localize = getLocale();
                if (!ModelState.IsValid)
                {
                    return View();
                }

                // Generate the token and send it
                if (!await SignInManager.SendTwoFactorCodeAsync(model.SelectedProvider))
                {
                    return View("Error");
                }
                return RedirectToAction("VerifyCode", new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
            }
            catch
            {
                return RedirectToAction("Message", "Home", new { lang = getLocale() });
            }
        }

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            try
            {
                ViewBag.Localize = getLocale();
                var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (loginInfo == null)
                {
                    return RedirectToAction("Login");
                }

                // Sign in the user with this external login provider if the user already has a login
                var result = await SignInManager.ExternalSignInAsync(loginInfo, isPersistent: false);
                switch (result)
                {
                    case SignInStatus.Success:
                        return RedirectToLocal(returnUrl);
                    case SignInStatus.LockedOut:
                        return View("Lockout");
                    case SignInStatus.RequiresVerification:
                        return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });
                    case SignInStatus.Failure:
                    default:
                        // If the user does not have an account, then prompt the user to create an account
                        ViewBag.ReturnUrl = returnUrl;
                        ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                        return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });
                }
            }
            catch
            {
                return RedirectToAction("Message", "Home", new { lang = getLocale() });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            try
            {
                ViewBag.Localize = getLocale();
                if (User.Identity.IsAuthenticated)
                {
                    return RedirectToAction("Index", "Manage");
                }

                if (ModelState.IsValid)
                {
                    // Get the information about the user from the external login provider
                    var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                    if (info == null)
                    {
                        return View("ExternalLoginFailure");
                    }
                    var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                    var result = await UserManager.CreateAsync(user);
                    if (result.Succeeded)
                    {
                        result = await UserManager.AddLoginAsync(user.Id, info.Login);
                        if (result.Succeeded)
                        {
                            await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                            return RedirectToLocal(returnUrl);
                        }
                    }
                    AddErrors(result);
                }

                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }
            catch
            {
                return RedirectToAction("Message", "Home", new { lang = getLocale() });
            }
            }

        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            ViewBag.Localize = getLocale();
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            ViewBag.Localize = getLocale();
            return View();
        }
        
        public ActionResult UserProfile()
        {
            ViewBag.Localize = getLocale();
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    ViewBag.Email = User.Identity.GetUserName();
                    ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());
                    if (user != null)
                    {
                        var person = _entities.People.Where(w => w.Email == user.Email).FirstOrDefault();
                        if (person.AvatarFile != null && System.IO.File.Exists(Server.MapPath("~/avatars/") + person.AvatarFile + "_128.png"))
                            ViewBag.Avatar_128 = getUserAvatar(person.AvatarFile + "_128");
                        else
                            ViewBag.Avatar_128 = getUserAvatar("user_128");
                    }
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    return View("Message", new { lang = getLocale() });
                }
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            try
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(getLocale());
                ViewBag.Localize = getLocale();
                ViewBag.ErrorTitle = null;

                if (!ModelState.IsValid)
                {
                    ViewBag.ErrorTitle = Resources.Resource.INCORRECT_PASSWORD;
                    return RedirectToAction("UserProfile");
                }
                var result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId<int>(), model.OldPassword, model.NewPassword);
                if (result.Succeeded)
                {
                    var user = await UserManager.FindByIdAsync(User.Identity.GetUserId<int>());
                    if (user != null)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                        ViewBag.Title = Resources.Resource.PASSWORD_CHANGE;
                        ViewBag.Message = Resources.Resource.PASSWORD_CHANGE_SUCCESS;
                        return View("Info");
                    }
                }
                if (result.Errors.Any())
                {
                    ViewBag.ErrorTitle = Resources.Resource.INCORRECT_PASSWORD;
                }
                AddErrors(result);
                ViewBag.Email = EMAIL;
                ViewBag.Avatar_128 = AVATAR;
                return RedirectToAction("UserProfile");
            }
            catch
            {
                return RedirectToAction("Message", "Home", new { lang = getLocale() });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
            }

            base.Dispose(disposing);
        }
        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            ViewBag.Localize = getLocale();
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        private async Task<string> SendEmailConfirmationTokenAsync(ApplicationUser user, string subject)
        {
            string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
            var callbackUrl = Url.Action("ConfirmEmail", "Account",
               new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
            await UserManager.SendEmailAsync(user.Id, subject,
               String.Format(System.IO.File.ReadAllText(HttpContext.Server.MapPath("~/Emails/Confirmation.html")), 
                            callbackUrl, user.Email));

            return callbackUrl;
        }
        #endregion


        string AVATAR = "/avatars/user_128.png";
        public ActionResult SetAvatar(HttpPostedFileBase file)
        {
            ViewBag.Localize = getLocale();
            if (file != null)
            {
                using (var transaction = _entities.Database.BeginTransaction())
                {
                    try
                    {
                        var avatarFile = "";
                        ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());
                        if (user != null)
                        {
                            var email = user.Email;
                            var person = _entities.People.Where(w => w.Email == email).FirstOrDefault();
                            if (person.AvatarFile != null)
                            {
                                try
                                {
                                    System.IO.File.Delete(Server.MapPath("/avatars/") + avatarFile + "_128");
                                    System.IO.File.Delete(Server.MapPath("/avatars/") + avatarFile + "_50");
                                }
                                catch { }
                            }
                            person.AvatarFile = _stringGenerator.Next();
                            avatarFile = person.AvatarFile;
                            _entities.SaveChanges();
                        }                        
                        var bitmap = new Bitmap(file.InputStream);

                        var bitmap128 = _imageCompressor.Compress(bitmap, new Size(128, 128));
                        var path = Server.MapPath("/avatars/")+avatarFile + "_128.png";
                        bitmap128.Save(path, ImageFormat.Png);
                        var bitmap25 = _imageCompressor.Compress(bitmap, new Size(50, 50));
                        path = Server.MapPath("/avatars/") + avatarFile + "_50.png";
                        bitmap25.Save(path, ImageFormat.Png);
                        ViewBag.Avatar_128 = getUserAvatar(avatarFile + "_128");
                        ViewBag.PeopleForMaster = _entities.People.Select(s => s).ToList();
                transaction.Commit();
            }
                    catch (Exception)
            {
                transaction.Rollback();
                return RedirectToAction("Message", "Home", new { lang = getLocale() });
            }
        }
    }
            ViewBag.Email = EMAIL;
            AVATAR = ViewBag.Avatar_128;
            return RedirectToAction("UserProfile");
        }
    }
}
