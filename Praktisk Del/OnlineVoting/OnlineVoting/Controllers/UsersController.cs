using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using OnlineVoting.Models;
using System.IO;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Data.SqlClient;
using System.Configuration;
using System.Xml.Linq;
using System.Xml.Serialization;


namespace OnlineVoting.Controllers
{

    public class UsersController : Controller
    {

        private OnlineVotingContext db = new OnlineVotingContext();


        [Authorize(Roles = "Admin")]
        public ActionResult XML()// skapar XML fil 
        {

        }


        private List<User> GenerateUserList()// anbänds till att lad hem alla användare så att man kan skapa en XML fil
        {


        }



        [Authorize(Roles = "User")]

        public ActionResult MySettings()// visar view med användare info för att kunna ändra den
        {
            //this.User.Identity.Name= name of user login
            var user = db.Users
                .Where(u => u.UserName == this.User.Identity.Name)
                .FirstOrDefault();

            var view = new UserSettingsView
            {
                Adress = user.Adress,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.Phone,
                Photo = user.Photo,
                UserId = user.UserId,
                UserName = user.UserName,
            };

            return View(view);
        }

        [HttpPost]
        public ActionResult MySettings(UserSettingsView view)// postar ändringarna man gjort i sin användar info 
        {
            if (ModelState.IsValid)
            {
                //Upload image
                string path = string.Empty;
                string pic = string.Empty;

                if (view.NewPhoto != null)
                {
                   
                    pic = Path.GetFileName(view.NewPhoto.FileName);//hämtar anmnet på bilden  
                  
                    path = Path.Combine(Server.MapPath("~/Content/Photos"), pic);   //sökvägen(~= relative rute)

                    view.NewPhoto.SaveAs(path);// sparar vägen 

                    using (MemoryStream ms = new MemoryStream())// laddar up bilden 
                    {
                        view.NewPhoto.InputStream.CopyTo(ms);
                        byte[] array = ms.GetBuffer();
                    }
                }

                var user = db.Users.Find(view.UserId);

                user.Adress = view.Adress;
                user.FirstName = view.FirstName;
                user.LastName = view.LastName;
                user.Phone = view.Phone;

                if (!string.IsNullOrEmpty(pic))
                {
                    user.Photo = string.Format("~/Content/Photos/{0}", pic);
                }

                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("Index", "Home");
            }

            return View(view);
        }




        [Authorize(Roles = "Admin")]

        public ActionResult OnOffAdmin(int id)// funktion som kan göra användare till admin, On/Off Admin 
        {
            var user = db.Users.Find(id);

            if (user != null)
            {
                var userContext = new ApplicationDbContext();
                var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(userContext));
                var userASP = userManager.FindByEmail(user.UserName);

                if (userASP != null)
                {
                    if (userManager.IsInRole(userASP.Id, "Admin"))
                    {
                        userManager.RemoveFromRole(userASP.Id, "Admin");
                    }
                    else
                    {
                        userManager.AddToRole(userASP.Id, "Admin");
                    }
                }
            }

            return RedirectToAction("Index");
        }


    

        [Authorize(Roles = "Admin")]
        public ActionResult Index()// hämtar lista på alla användare som finns på DB
        {
            var userContext = new ApplicationDbContext();
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(userContext));
            var users = db.Users.ToList();
            var usersView = new List<UserIndexView>();

            foreach (var user in users)
            {
                var userASP = userManager.FindByEmail(user.UserName);

                usersView.Add(new UserIndexView
                {
                    Adress = user.Adress,
                    Candidates = user.Candidates,
                    FirstName = user.FirstName,
                    //GroupMember = user.GroupMember,
                    IsAdmin = userASP != null && userManager.IsInRole(userASP.Id, "Admin"),
                    LastName = user.LastName,
                    Phone = user.Phone,
                    Photo = user.Photo,
                    UserId = user.UserId,
                    UserName = user.UserName,
                });
            }
            return View(usersView);
        }


        [Authorize(Roles = "Admin")]
        public ActionResult Details(int? id)// hämtar specifik användares info
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }


        [Authorize(Roles = "Admin")]
        public ActionResult Create()// användas av admin för att skapa ny anändare 
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(UserView userView)// postar den nya anändaren som admin skapat 
        {
            if (!ModelState.IsValid)
            {

                return View(userView);
            }


            //ladar upp bild 

            String path = string.Empty;
            String pic = string.Empty;

            if (userView.Photo != null)
            {

                pic = Path.GetFileName(userView.Photo.FileName);
                path = Path.Combine(Server.MapPath("~/Content/Photos"), pic);
                userView.Photo.SaveAs(path);
                using (MemoryStream ms = new MemoryStream())
                {
                    userView.Photo.InputStream.CopyTo(ms);
                    byte[] array = ms.GetBuffer();
                }
            }

            //sparar användar info

            var user = new User
            {

                Adress = userView.Adress,
                FirstName = userView.FirstName,
                LastName = userView.LastName,
                Phone = userView.Phone,
                Photo = pic == string.Empty ? string.Empty : string.Format("~/Content/Photos/{0}", pic),
                UserName = userView.UserName,


            };

            db.Users.Add(user);

            try
            {
                db.SaveChanges();
                this.CreateASPUser(userView);
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null && ex.InnerException.InnerException != null && ex.InnerException.InnerException.Message.Contains("UserNameIndex"))
                {

                    ViewBag.Error = "The email has already been used by another user";

                }
                else
                {
                    ViewBag.Error = ex.Message;

                }

                return View(userView);
            }

            return RedirectToAction("Index");
        }

        private void CreateASPUser(UserView userView)// skapar en ny användare i ASP.net DB 
        {

            //user managment
            var userContext = new ApplicationDbContext();
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(userContext));
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(userContext));


            string roleName = "User";// användarens rol i systemet  
            // string roleName = "Admin"; // skapar en admin 

            if (!roleManager.RoleExists(roleName))// kontrolerar om rolen existerar om den inte gör det så skapas den 
            {

                roleManager.Create(new IdentityRole(roleName));

            }

            

            var userASP = new ApplicationUser // skapar ASPNetUser

            {

                UserName = userView.UserName,
                Email = userView.UserName,
                PhoneNumber = userView.Phone,

            };

            userManager.Create(userASP, userASP.UserName);

            userASP = userManager.FindByName(userView.UserName);
            userManager.AddToRole(userASP.Id, "User");// läger till role i DB, byt till Admin för att skapa en admin User 



        }



        [Authorize(Roles = "Admin")]
        public ActionResult Edit(int? id)// används för att kuna ändra användares info
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var user = db.Users.Find(id);

            if (user == null)
            {
                return HttpNotFound();
            }

            var userView = new UserView
            {

                Adress = user.Adress,
                FirstName = user.Adress,
                LastName = user.LastName,
                Phone = user.Phone,
                UserId = user.UserId,
                UserName = user.UserName,

            };

            return View(userView);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(UserView userView)// postar det ändrade infot om användaren 
        {
            if (!ModelState.IsValid)
            {
                return View(userView);
            }

            //Upload image

            String path = string.Empty;
            String pic = string.Empty;

            if (userView.Photo != null)
            {

                pic = Path.GetFileName(userView.Photo.FileName);
                path = Path.Combine(Server.MapPath("~/Content/Photos"), pic);
                userView.Photo.SaveAs(path);
                using (MemoryStream ms = new MemoryStream())
                {
                    userView.Photo.InputStream.CopyTo(ms);
                    byte[] array = ms.GetBuffer();
                }
            }

            var user = db.Users.Find(userView.UserId);

            user.Adress = userView.Adress;
            user.FirstName = userView.FirstName;
            user.LastName = userView.LastName;
            user.Phone = userView.Phone;

            if (!string.IsNullOrEmpty(pic))
            {

                user.Photo = string.Format("~/Content/Photos/{0}", pic);

            }


            db.Entry(user).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
        }



        [Authorize(Roles = "Admin")]
        public ActionResult Delete(int? id)// hämtar anvädnare som ska tas bort
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)// postar användare som ska tas bort 
        {
            User user = db.Users.Find(id);

            db.Users.Remove(user);

            try
            {
                db.SaveChanges();
            }

            catch (Exception ex)
            {
                if (ex.InnerException != null &&
                      ex.InnerException.InnerException != null &&
                      ex.InnerException.InnerException.Message.Contains("REFERENCE"))
                {
                    ModelState.AddModelError(string.Empty, "Can't delete the register because it has related records to it");

                }
                else
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }

                return View(user);
            }
            return RedirectToAction("Index");

        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
