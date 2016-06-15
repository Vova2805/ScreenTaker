
using ScreenTaker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Globalization;
using System.Threading;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;

namespace ScreenTaker.Controllers
{
    [Authorize]
    public class EditController : GeneralController
    {
        private ScreenTakerEntities _entities = new ScreenTakerEntities();
        public ActionResult Index(string lang = "en")
        {
            ViewBag.Localize = getLocale();
            return View("UserGroups",new { lang = getLocale() });
        }

        public ActionResult EditImage(string lang = "en")
        {
            ViewBag.Localize = getLocale();
            return View("EditImage", new { lang = getLocale() });
        }

        public ActionResult UserGroups(int selectedId=-1)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(getLocale());
            ViewBag.Localize = getLocale();
            using (var transaction = _entities.Database.BeginTransaction())
            {                                   
                try
                {
                    ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());
                    if (user != null)
                    {
                        var email = user.Email;                        
                        ViewBag.Groups = _entities.PersonGroups.Where(w=>w.Person.Email==email).Select(s => s).ToList();
                        ViewBag.GroupMemberCounts = _entities.PersonGroups.Where(w => w.Person.Email == email).Select(s => s.GroupMembers.Count).ToList();
                        if (selectedId == -1)
                            selectedId = _entities.PersonGroups.Where(w => w.Person.Email == email).Select(s => s.Id).FirstOrDefault();
                        ViewBag.selectedId = selectedId;
                    }

                    var emails = from p in _entities.People
                                 join m in _entities.GroupMembers
                                 on p.Id equals m.PersonId
                                 where m.GroupId == selectedId
                                 select new { ID = m.GroupId, Email = p.Email, p.AvatarFile };              
                    if (emails.Any())
                    {
                        ViewBag.Emails = emails.Select(s => s.Email).ToList();
                        var baseUrl = GetBaseUrl();                        
                        IList<string> avatars = new List<string>();
                        foreach (var e in emails)
                        {
                            if (e.AvatarFile != null && System.IO.File.Exists(getUserAvatar(e.AvatarFile + "_50")))
                                avatars.Add(getUserAvatar(e.AvatarFile+"_50"));
                            else
                                avatars.Add(getUserAvatar("user_50"));
                        }
                        ViewBag.Avatars = avatars;
                    }                                                  
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
	            ViewBag.MessageContent = ex.Message;
                    return View("Message", new { lang = getLocale() });
                }
            }
            return View("UserGroups", new { lang = getLocale() });
        }

        [AllowAnonymous]
        public void ChangeLocalization(string request, string lang = "en")
        {
            
            if (request.Equals("/") || request.Equals(""))
            {
                request = GetBaseUrl() +"Home/Welcome";
            }
            if (request.Contains("?"))
            {
                if (request.Contains("lang"))
                {
                    int index = request.IndexOf("lang=");
                    string sub = request.Substring(index, 7);
                    request = request.Replace(sub, "lang=" + lang);
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
            Session["Locale"] = lang;
            ViewBag.Localize = getLocale();
            Response.Redirect(request);
        }

        public ActionResult CreateGroup(string name)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(getLocale());
            ViewBag.Localize = getLocale();
            int idToRedirect = 0;
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {                   
                    if (name==null||name.Length==0)
                        throw new Exception(Resources.Resource.ERR_EMPTY_FIELD);
                    var group = new PersonGroup();
                    group.Name = name;                    

                    ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());
                    if (user != null)
                    {
                        if (_entities.PersonGroups.Where(w => w.Name == name&&w.PersonId==user.Id).Any())
                            throw new Exception(Resources.Resource.ERR_GROUP_ALREDY);
                        var email = user.Email;
                        group.PersonId = _entities.People.Where(w=>w.Email==email).Select(s=>s.Id).FirstOrDefault();
                    }
                    else
                        group.PersonId = null;
                    _entities.PersonGroups.Add(group);
                    _entities.SaveChanges();
                    idToRedirect = group.Id;
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["MessageContent"] = ex.Message;
                }
            }
            return RedirectToAction("Partial_GroupsAndEmails", new {selectedId=idToRedirect, lang = getLocale()  });
        }


        public ActionResult RemoveGroup(int selectedId = 0)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(getLocale());
            ViewBag.Localize = getLocale();
            int idToRedirect = 0;
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    var groupId = selectedId;

                    if (_entities.GroupMembers.Where(w => w.GroupId == groupId).Any())
            {
                var groupMembers = _entities.GroupMembers.Where(w => w.GroupId == groupId).ToList();
                for (var i = 0; i < groupMembers.Count; i++)
                    if (groupMembers[i] != null)
                        _entities.GroupMembers.Remove(groupMembers[i]);
            }
            if (_entities.GroupShares.Where(w => w.GroupId == groupId).Any())
            {
                var groupShares= _entities.GroupShares.Where(w => w.GroupId == groupId).ToList();
                for (var i = 0; i < groupShares.Count; i++)
                    if (groupShares[i] != null)
                        _entities.GroupShares.Remove(groupShares[i]);
            }
                var group = _entities.PersonGroups.Where(w => w.Id == groupId).FirstOrDefault();
            if (group != null)
                _entities.PersonGroups.Remove(group);
            ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());
                    if (user != null)
                    {
                        var email = user.Email;
                idToRedirect = _entities.PersonGroups.Where(w => w.Person.Email == email).Select(s => s.Id).FirstOrDefault();
            }
                    _entities.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["MessageContent"] = ex.Message;
                }
            }
            return RedirectToAction("Partial_GroupsAndEmails", new { selectedId = idToRedirect, lang = getLocale()  });
        }

        public ActionResult AddUser(int selectedId,string email)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(getLocale());
            ViewBag.Localize = getLocale();
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    if (email == null || email.Length == 0)
                        throw new Exception(Resources.Resource.ERR_EMPTY_FIELD);
                    if (!_entities.People.Where(s => s.Email.Equals(email)).Any())
                        throw new Exception(Resources.Resource.ERR_EMAIL_NOT_EXIST);
                    if (_entities.People.Where(w => w.Email == email && w.GroupMembers.Where(w2 => w2.GroupId == selectedId).Any()).Any())
                        throw new Exception(Resources.Resource.ERR_USER_ALREDY);                    
                    ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());
                    if (user != null)
                    {
                        
                        if (user != null && email==user.Email)
                                throw new Exception(Resources.Resource.ERR_ADD_YOURSELF);
                        var friend = _entities.People.Where(w => w.Email == email).FirstOrDefault();
                        if(friend!=null&&!_entities.PersonFriends.Where(w=>w.PersonId==user.Id&&w.FriendId==friend.Id).Any())
                        {
                            var personFriend = new PersonFriend() {PersonId=user.Id,FriendId=friend.Id };
                            _entities.PersonFriends.Add(personFriend);
                        }
                    }
                    var groupMember = new GroupMember();
                    groupMember.GroupId = selectedId;
                    groupMember.PersonId = _entities.People.Where(s => s.Email.Equals(email)).Select(s => s.Id).FirstOrDefault();
                    _entities.GroupMembers.Add(groupMember);
                    _entities.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();                    
                    TempData["MessageContent"]= ex.Message;
                }
            }
            return RedirectToAction("Partial_GroupsAndEmails", new { selectedId = selectedId, lang = getLocale()  });
        }

        public ActionResult RemoveUser(int selectedId,string email)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(getLocale());
            ViewBag.Localize = getLocale();
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    var personId = _entities.People.Where(w => w.Email == email).Select(s => s.Id).FirstOrDefault();
                    _entities.GroupMembers.Remove(_entities.GroupMembers.Where(w => w.GroupId == selectedId && w.PersonId == personId).FirstOrDefault());
                    _entities.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["MessageContent"] = ex.Message;
                }
            }
            return RedirectToAction("Partial_GroupsAndEmails", new { selectedId = selectedId, lang = getLocale()  });
        }

        public ActionResult Partial_GroupsAndEmails(int selectedId, string lang = "en")
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(getLocale());
            ViewBag.Localize = getLocale();
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());
                    if (user != null)
                    {
                        var email = user.Email;
                        ViewBag.Groups = _entities.PersonGroups.Where(w => w.Person.Email == email).Select(s => s).ToList();
                        ViewBag.GroupMemberCounts = _entities.PersonGroups.Where(w => w.Person.Email == email).Select(s => s.GroupMembers.Count).ToList();
                        if (selectedId == -1)
                            selectedId = _entities.PersonGroups.Where(w => w.Person.Email == email).Select(s => s.Id).FirstOrDefault();
                        ViewBag.selectedId = selectedId;
                    }

                    var emails = from p in _entities.People
                                 join m in _entities.GroupMembers
                                 on p.Id equals m.PersonId
                                 where m.GroupId == selectedId
                                 select new { ID = m.GroupId, Email = p.Email, p.AvatarFile };
                    if (emails.Any())
                    {
                        ViewBag.Emails = emails.Select(s => s.Email).ToList();
                        var baseUrl = GetBaseUrl();                        
                        IList<string> avatars = new List<string>();
                        foreach (var e in emails)
                        {
                            if (e.AvatarFile == null || !System.IO.File.Exists(Server.MapPath("/avatars/") + e.AvatarFile + "_50.png"))
                                avatars.Add(getUserAvatar("user_50"));
                            else
                                avatars.Add(getUserAvatar(e.AvatarFile + "_50"));
                        }
                        ViewBag.Avatars = avatars;                        
                    }
                    if (TempData["MessageContent"] != null)
                        ViewBag.MessageContent = TempData["MessageContent"];
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ViewBag.MessageContent = ex.Message;
                }
                return PartialView("Partial_GroupsAndEmails", new { lang = getLocale() });

            }
        }
        public ActionResult AutocompleteSearchEmails(string term)
        {
            try
            {
                ViewBag.Localize = getLocale();
                ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());
                if (user != null)
                {
                    var emails = _entities.People.Where(w => w.Email.StartsWith(term) && _entities.PersonFriends.Where(ww => ww.PersonId == user.Id).Select(s => s.FriendId).Contains(w.Id)).Select(s => new { value = s.Email }).ToList();                                       
                    return Json(emails, JsonRequestBehavior.AllowGet);
                }
                else return null;
            }
            catch
            {
                return RedirectToAction("Message", "Home", new { lang = getLocale() });
            }
        }
    }
}
