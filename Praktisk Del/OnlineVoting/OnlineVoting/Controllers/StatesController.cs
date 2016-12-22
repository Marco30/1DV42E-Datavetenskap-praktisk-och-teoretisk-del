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

            if (TempData["Message"] != null)
            {
                ViewBag.Message = TempData["Message"].ToString();// visar medelande som tagit med fron Edit eller delit view
            }
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

            if ("Open" == state.Descripcion | "Closed" == state.Descripcion)// används för att kontrollera att inte Open eller closed state ändras, utan dem så slutar visa funktioner att fungera
            {

                TempData["Message"] = "You tried to Edit (" + state.Descripcion + "), This State can not be edited because it is vital for OnlineVotingSystem!";
                return RedirectToAction("Index", "States");

            }
            else
            {
                return View(state);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(State state)// postar ändringar man gjort på state
        {

            //var S1 = db.States.Find(state.StateId);
            // används för att inte Entity Framework inte ska binda sig till state modelen så att den längre ner kan updateras utan problem
            var S1 = db.States.AsNoTracking().Where(p => p.StateId == state.StateId).FirstOrDefault();
            
            if ("Open" == S1.Descripcion | "Closed" == S1.Descripcion)// används här i den här post funktionen för att hindra post attacker som kan göras genom URL, man postar ändrignar som int ska gå att göras, 
            {
                TempData["Message"] = "You tried to use the URL to Post Edit (" + S1.Descripcion + "), This State can not be edited because it is vital for OnlineVotingSystem!";
                return RedirectToAction("Index", "States");
            }
            else if (!ModelState.IsValid)
            {

                return View(state);

            }
            else
            {
                
                db.Entry(state).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
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

            if ("Open" == state.Descripcion | "Closed" == state.Descripcion)//används för att kontrollera att inte Open eller closed state tas bort, utan dem så slutar visa funktioner att fungera 
            {
                TempData["Message"] = "You tried to Delit (" + state.Descripcion + "), This State can not be Delitet because it is vital for OnlineVotingSystem!";
                return RedirectToAction("Index", "States");

            }
            else
            {
                return View(state);
            }
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult Delete(int id)// posta och tar bort state 
        {


            var state = db.States.Find(id);

            if (state == null)
            {

                return HttpNotFound();
            }


            if ("Open" == state.Descripcion | "Closed" == state.Descripcion)//används för att kontrollera att inte Open eller closed state tas bort, utan dem så slutar visa funktioner att fungera 
            {
                TempData["Message"] = "You tried to use the URL to Post Delit (" + state.Descripcion + "), This State can not be Delitet because it is vital for OnlineVotingSystem!";
                return RedirectToAction("Index", "States");

            }
            else
            {
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