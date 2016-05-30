
using ScreenTaker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Globalization;
using System.Threading;
using System.Web.WebPages;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;

namespace ScreenTaker.Controllers
{
    [Authorize]
    public class EditController : Controller
    {
        private ScreenTakerEntities _entities = new ScreenTakerEntities();
        public ActionResult Index(string lang = "en")
        {
            return View("UserGroups");
        }

        public ActionResult UserGroups(string lang = "en",int selectedId=-1)
        {
            using (var transaction = _entities.Database.BeginTransaction())
            {                                   
                try
                {
                    ViewBag.Groups = _entities.PersonGroups.Select(s => s).ToList();              
                    ViewBag.GroupMemberCounts= _entities.PersonGroups.Select(s => s.GroupMembers.Count).ToList();

                    var emails = from p in _entities.People
                                 join m in _entities.GroupMembers
                                 on p.Id equals m.PersonId
                                 where m.GroupId == selectedId
                                 select new { ID = m.GroupId, Email = p.Email };
                    if (emails.Any())
                        ViewBag.Emails = emails.Select(s => s.Email).ToList();
                    if (selectedId == -1)
                        selectedId= _entities.PersonGroups.Select(s => s.Id).FirstOrDefault();
                    ViewBag.selectedId = selectedId;
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                }
            }
            return View();
        }

        public void ChangeLocalization(string request, string lang = "en")
        {
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
            Response.Redirect(request);
        }

        public ActionResult CreateGroup(string name)
        {
            int idToRedirect = 0;
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    var group = new PersonGroup();
                    group.Name = name;

                    //Microsoft.AspNet.Identity.UserManager.FindById(User.Identity.GetUserId());

                    ApplicationUser user = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(User.Identity.GetUserId<int>());
                    if (user != null)
                    {
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
                }
            }
            return RedirectToAction("UserGroups", new {selectedId=idToRedirect });
        }


        public ActionResult RemoveGroup(int selectedId = 0)
        {
            int idToRedirect = 0;
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    var groupId = selectedId;

                    if (_entities.GroupMembers.Where(w => w.GroupId == groupId).Any())
                    {
                        var groupMembers = _entities.GroupMembers.Where(w => w.GroupId == groupId).ToList();
                        for(var i=0;i<groupMembers.Count;i++)                        
                            _entities.GroupMembers.Remove(groupMembers[i]);                        
                    }
                    var group = _entities.PersonGroups.Where(w => w.Id == groupId).FirstOrDefault();
                    _entities.PersonGroups.Remove(group);
                    idToRedirect = _entities.PersonGroups.Select(s=>s.Id).FirstOrDefault();
                    _entities.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                }
            }
            return RedirectToAction("UserGroups", new { selectedId = idToRedirect});
        }

        public ActionResult AddUser(int selectedId,string email)
        {            
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    if (!_entities.People.Where(s => s.Email.Equals(email)).Any())
                        throw new Exception("There is no user with such e-mail.");
                    if (_entities.People.Where(w => w.Email == email && w.GroupMembers.Where(w2 => w2.GroupId == selectedId).Any()).Any())
                        throw new Exception("This user is alredy here.");
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
                }
            }
            return RedirectToAction("UserGroups", new { selectedId = selectedId });
        }

        public ActionResult RemoveUser(int selectedId,string email)
        {
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
                }
            }
            return RedirectToAction("UserGroups", new { selectedId = selectedId });
        }
    }
}