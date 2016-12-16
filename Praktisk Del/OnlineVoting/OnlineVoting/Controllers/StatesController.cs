using OnlineVoting.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace OnlineVoting.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StatesController : Controller
    {

        public OnlineVotingContext db = new OnlineVotingContext();

        [HttpGet]
        public ActionResult Index()//visar alla states
        {
            return View(db.States.ToList());
        }

        [HttpGet]
        [Authorize]
        public ActionResult Create()//visar view där mans skapar ny state
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(State state)// postar ny state till DB
        {
            if (!ModelState.IsValid)
            {

                return View(state);

            }

            db.States.Add(state);
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        [HttpGet]

        public ActionResult Edit(int? id)// används om man vill ändra existerande state
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var state = db.States.Find(id);

            if (state == null)
            {

                return HttpNotFound();
            }

            return View(state);
        }

        [HttpPost]

        public ActionResult Edit(State state)// postar ändringar man gjort på state
        {
            if (!ModelState.IsValid)
            {

                return View(state);

            }


            db.Entry(state).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
        }


        public ActionResult Details(int? id)// hämtar alla state info
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var state = db.States.Find(id);

            if (state == null)
            {

                return HttpNotFound();
            }

            return View(state);
        }

        [HttpGet]

        public ActionResult Delete(int? id)// visar bekräftelse view för att ta bort 
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var state = db.States.Find(id);

            if (state == null)
            {

                return HttpNotFound();
            }

            return View(state);
        }

        [HttpPost]

        public ActionResult Delete(int id, State state)// posta och tar bort state 
        {


            state = db.States.Find(id);

            if (state == null)
            {

                return HttpNotFound();
            }

            db.States.Remove(state);
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
                    ViewBag.Error = "Can't delete the register because it has related records to it";

                }
                else
                {
                    ViewBag.Error = ex.Message;
                }

                return View(state);

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