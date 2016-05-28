
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

namespace ScreenTaker.Controllers
{
    public class EditController : Controller
    {
        private ScreenTakerEntities _entities = new ScreenTakerEntities();
        public ActionResult Index(string lang = "en")
        {
            return View("UserGroups");
        }

        public ActionResult UserGroups(string lang = "en",int selectedId=0)
        {
            using (var transaction = _entities.Database.BeginTransaction())
            {
                ViewBag.selectedId = selectedId;
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

            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    var group = new PersonGroup();
                    group.Name = name;
                    group.PersonId = null;
                    _entities.PersonGroups.Add(group);
                    _entities.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                }
            }
            return RedirectToAction("UserGroups");
        }


        public ActionResult RemoveGroup(int selectedId = 0)
        {
            using (var transaction = _entities.Database.BeginTransaction())
            {
                try
                {
                    var groupId = selectedId;
                    var group = _entities.PersonGroups.Where(w => w.Id == groupId).FirstOrDefault();
                    _entities.PersonGroups.Remove(group);
                    _entities.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                }
            }
            return RedirectToAction("UserGroups");
        }

    }
}