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