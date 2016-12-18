﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using OnlineVoting.Models;
using System.Data.SqlClient;
using System.Configuration;

namespace OnlineVoting.Controllers
{

    public class VotingsController : Controller
    {
        private OnlineVotingContext db = new OnlineVotingContext();

        [Authorize(Roles = "Admin")]
        public ActionResult Close(int id)// används för att stänga vallet 
        {
            var voting = db.Votings.Find(id);
            if (voting != null)
            {
                var candidate = db.Candidates
                    .Where(c => c.VotingId == voting.VotingId)
                    .OrderByDescending(c => c.QuantityVotes)
                    .FirstOrDefault();
                if (candidate != null)
                {
                    var state = this.GetState("Closed");
                    voting.StateId = state.StateId;
                    voting.CandidateWinId = candidate.User.UserId;
                    db.Entry(voting).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }
            return RedirectToAction("Index");
        }




        public ActionResult ShowResults(int id)// visar resultat på valet med vinar och lista på alla deltagar och antal röster dem fåt
        {
            var connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;


            var dataTable = new DataTable();
            //@ from not concatenate
            var sql = @"SELECT  Votings.VotingId, Votings.Description AS Voting, States.Descripcion AS State, 
                                Users.FirstName + ' ' + Users.LastName AS Candidate, Candidates.QuantityVotes
                         FROM   Candidates INNER JOIN
                                Users ON Candidates.UserId = Users.UserId INNER JOIN
                                Votings ON Candidates.VotingId = Votings.VotingId INNER JOIN
                                States ON Votings.StateId = States.StateId
                          WHERE Votings.VotingId =" + id + " ORDER BY Candidates.QuantityVotes DESC";

            // Skapar och initierar ett anslutningsobjekt.
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    // Skapar ett List-objekt med 100 platser.
                    var Rank = new List<Rank>(100);

                    // Skapar och initierar ett SqlCommand-objekt som används till att exekveras specifierad lagrad procedur.
                    var command = new SqlCommand(sql, connection);


                    // Öppnar anslutningen till databasen.
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        // Tar reda på vilket index de olika kolumnerna har.
                        var VotingID = reader.GetOrdinal("VotingID");
                        var Electionname = reader.GetOrdinal("Voting");
                        var State = reader.GetOrdinal("State");
                        var Candidate = reader.GetOrdinal("Candidate");
                        var QuantityVotest = reader.GetOrdinal("QuantityVotes");


                        // Så länge som det finns poster att läsa returnerar Read true och läsningen fortsätter.
                        while (reader.Read())
                        {
                            // Hämtar ut datat för en post.
                            Rank.Add(new Rank
                            {
                                VotingID = reader.GetInt32(VotingID),
                                Electionname = reader.GetString(Electionname),
                                State = reader.GetString(State),
                                Candidate = reader.GetString(Candidate),
                                QuantityVotes = reader.GetInt32(QuantityVotest)

                            });
                        }
                    }

                    ViewBag.NameOfElection = Rank[0].Electionname;
                    ViewBag.StateOfElection = Rank[0].State;
                    ViewBag.WinnerOfElection = Rank[0].Candidate;
                    ViewBag.NumberOfVotes = Rank[0].QuantityVotes;
                    // Avallokerar minne som inte används och skickar tillbaks listan med aktiviteter.
                    Rank.TrimExcess();
                    return View(Rank);
                }
                catch
                {
                    throw new ApplicationException("An error occured while getting members from the database.");
                }
            }

        }




        [Authorize(Roles = "User")]
        public ActionResult Results()// listar alla val som hålits
        {
            var state = this.GetState("Closed");
            var votings = db.Votings
                .Where(v => v.StateId == state.StateId)
                .Include(v => v.State);
            var views = new List<VotingIndexView>();
            var db2 = new OnlineVotingContext();

            //Winner
            foreach (var voting in votings)
            {
                User user = null;
                if (voting.CandidateWinId != 0)
                {
                    user = db2.Users.Find(voting.CandidateWinId);
                }

                views.Add(new VotingIndexView
                {
                    CandidateWinId = voting.CandidateWinId,
                    DateTimeEnd = voting.DateTimeEnd,
                    DateTimeStart = voting.DateTimeStart,
                    Description = voting.Description,
                    IsEnableBlankVote = voting.IsEnableBlankVote,
                    IsForAllUsers = voting.IsForAllUsers,
                    QuantityBlankVotes = voting.QuantityBlankVotes,
                    Remarks = voting.Remarks,
                    StateId = voting.StateId,
                    State = voting.State,
                    VotingId = voting.VotingId,
                    Winner = user,

                });
            }
            return View(views);


        }


        [Authorize(Roles = "User")]
        public ActionResult VoteForCandidate(int candidateId, int votingId)// validerign av användare för få möjlighet att rösta 
        {
            // validering av anvädnare 
            var user = db.Users
                .Where(u => u.UserName == this.User.Identity.Name)
                .FirstOrDefault();

            if (user == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var candidate = db.Candidates.Find(candidateId);

            if (candidate == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var voting = db.Votings.Find(votingId);

            if (voting == null)
            {
                return RedirectToAction("Index", "Home");
            }

            //validering om man inte röstat så kommer man hitt 
            if (this.VoteCandidate(user, candidate, voting))
            {
                return RedirectToAction("MyVotings");
            }

            return RedirectToAction("Index", "Home");
        }

        private bool VoteCandidate(Models.User user, Candidate candidate, Voting voting) // röstnings funktion 
        {
            
            using (var transaction = db.Database.BeginTransaction())// kontakt med DB och transaction anbvänds i MVC för att kunna läga till data i flera tabeler 
            {
                var votingDetail = new VotingDetail
                {
                    CandidateId = candidate.CandidateId,
                    DateTime = DateTime.Now,
                    UserId = user.UserId,
                    VotingID = voting.VotingId,
                };

                db.VotingDetails.Add(votingDetail);

            
                candidate.QuantityVotes++;// läger till en röst 

                db.Entry(candidate).State = EntityState.Modified;// läger till röst i DB

                voting.QuantityVotes++;
                db.Entry(voting).State = EntityState.Modified;

                //sparar data i DB
                try
                {
                    db.SaveChanges();
                  
                    transaction.Commit();
                    return true;
                }
                catch (Exception)// om någto går fel så går DB till baks till inna något ändrats 
                {
                    transaction.Rollback();
                }
            }
            return false;
        }



        [Authorize(Roles = "User")]
        public ActionResult Vote(int votingId)// visar röstnigns Viewn
        {
            var voting = db.Votings.Find(votingId);
            var view = new VotingVoteView
            {

                DateTimeEnd = voting.DateTimeEnd,
                DateTimeStart = voting.DateTimeStart,
                Description = voting.Description,
                IsEnableBlankVote = voting.IsEnableBlankVote,
                IsForAllUsers = voting.IsForAllUsers,
                MyCandidate = voting.Candidates.ToList(),
                Remarks = voting.Remarks,
                VotingId = voting.VotingId,
            };

            return View(view);
        }


        [Authorize(Roles = "User")]
        public ActionResult MyVotings()// visar valen 
        {
            // söker login
            var user = db.Users
                .Where(u => u.UserName == this.User.Identity.Name)
                .FirstOrDefault();

            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "There an error with the current user, call the support");
                return View();
            }


            var state = this.GetState("Open");// hämtar valen som är öpan/ där röstnings tiden gäller 

            var votings = db.Votings
                .Where(v => v.StateId == state.StateId &&
                v.DateTimeStart <= DateTime.Now &&
                v.DateTimeEnd >= DateTime.Now)
                .Include(v => v.Candidates)
                //.Include(v => v.VotingGroups)
                .Include(v => v.State)
                .ToList();


            //tar bort val där användaren redan röstat  
            foreach (var voting in votings.ToList())
            {


                var votingDetail = db.VotingDetails.
                    Where(vd => vd.VotingID == voting.VotingId &&
                    vd.UserId == user.UserId).FirstOrDefault();

                if (votingDetail != null)
                {
                    votings.Remove(voting);
                }

            }

            return View(votings);
        }



        private State GetState(string stateName)// konrolerar State om valet är öppet eller stängt 
        {
            var state = db.States.Where(s => s.Descripcion == stateName)
                .FirstOrDefault();
            if (state == null)
            {
                state = new State
                {
                    Descripcion = stateName,
                };

                db.States.Add(state);
                db.SaveChanges();
            }

            return state;
        }



        [Authorize(Roles = "Admin")]
        public ActionResult AddCandidate(int id)// visar view med en drop down lista där man kan läga till användare till valet 
        {
            var view = new AddCandidateView
            {
                VotingId = id,
            };

            ViewBag.UserId = new SelectList(
                db.Users.OrderBy(u => u.FirstName)
               .ThenBy(u => u.LastName),
               "UserId",
               "FullName");

            return View(view);
        }

        //Post AddCandidate
        [HttpPost]
        public ActionResult AddCandidate(AddCandidateView view)// postar användare man valt till kandidat
        {
            if (ModelState.IsValid)
            {
                //no meter 2 veces un candidato
                var candidate = db.Candidates
                    .Where(c => c.VotingId == view.VotingId &&
                                c.UserId == view.UserId)
                                .FirstOrDefault();

                if (candidate != null)
                {
                    //another way error
                    ModelState.AddModelError(string.Empty, "The candidate already belongs to voting");
                    //ViewBag.Error = "The group already belongs to voting";
                    ViewBag.UserId = new SelectList(
                                    db.Users.OrderBy(u => u.FirstName)
                                    .ThenBy(u => u.LastName),
                                    "UserId",
                                    "FullName");
                    return View(view);
                }

                candidate = new Candidate
                {
                    UserId = view.UserId,
                    VotingId = view.VotingId,
                };


                db.Candidates.Add(candidate);
                db.SaveChanges();
                return RedirectToAction(string.Format("Details/{0}", view.VotingId));
            }

            ViewBag.UserId = new SelectList(
                db.Users.OrderBy(u => u.FirstName)
               .ThenBy(u => u.LastName),
               "UserId",
               "FullName");
            return View(view);

        }


        [Authorize(Roles = "Admin")]
        public ActionResult DeleteCandidate(int id)// tar bort kandidat från valet 
        {
            var candidate = db.Candidates.Find(id);

            if (candidate != null)
            {
                db.Candidates.Remove(candidate);
                db.SaveChanges();
            }

            return RedirectToAction(string.Format("Details/{0}", candidate.VotingId));
        }





        [Authorize(Roles = "Admin")]
        public ActionResult Index()// visar index view meda alla valen för admin och om den är avslutad så visas vinare
        {
            var votings = db.Votings.Include(v => v.State);
            var views = new List<VotingIndexView>();
            var db2 = new OnlineVotingContext();

            //visar info om valet och visar också vinare om vallet är slut förd 
            foreach (var voting in votings)
            {
                User user = null;
                if (voting.CandidateWinId != 0)
                {
                    user = db2.Users.Find(voting.CandidateWinId);
                }

                views.Add(new VotingIndexView
                {
                    CandidateWinId = voting.CandidateWinId,
                    DateTimeEnd = voting.DateTimeEnd,
                    DateTimeStart = voting.DateTimeStart,
                    Description = voting.Description,
                    IsEnableBlankVote = voting.IsEnableBlankVote,
                    IsForAllUsers = voting.IsForAllUsers,
                    QuantityBlankVotes = voting.QuantityBlankVotes,
                    Remarks = voting.Remarks,
                    StateId = voting.StateId,
                    State = voting.State,
                    VotingId = voting.VotingId,
                    Winner = user,

                });
            }
            return View(views);
        }




        [Authorize(Roles = "Admin")]
        public ActionResult Details(int? id)// visar detaljerad info om valet
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Voting voting = db.Votings.Find(id);
            if (voting == null)
            {
                return HttpNotFound();
            }

            var view = new DetailsVotingView
            {
                Candidates = voting.Candidates.ToList(),
                CandidateWinId = voting.CandidateWinId,
                DateTimeEnd = voting.DateTimeEnd,
                DateTimeStart = voting.DateTimeStart,
                Description = voting.Description,
                IsEnableBlankVote = voting.IsEnableBlankVote,
                IsForAllUsers = voting.IsForAllUsers,
                QuantityBlankVotes = voting.QuantityBlankVotes,
                QuantityVotes = voting.QuantityVotes,
                Remarks = voting.Remarks,
                StateId = voting.StateId,
                VotingId = voting.VotingId,
            };






            return View(view);
        }



        [Authorize(Roles = "Admin")]
        public ActionResult Create()// visar view där man skapar valet 
        {
            ViewBag.StateId = new SelectList(db.States, "StateId", "Descripcion");

            //DateTime
            var view = new VotingView
            {
                DateStart = DateTime.Now,
                DateEnd = DateTime.Now,
            };



            return View(view);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(VotingView view)// postar valet man skapat 
        {
            if (ModelState.IsValid)
            {
                //DateTime
                var voting = new Voting
                {
                    DateTimeEnd = view.DateEnd
                                  .AddHours(view.TimeEnd.Hour)
                                  .AddMinutes(view.TimeEnd.Minute),
                    DateTimeStart = view.DateStart
                                  .AddHours(view.TimeStart.Hour)
                                  .AddMinutes(view.TimeStart.Minute),
                    Description = view.Description,
                    IsEnableBlankVote = view.IsEnabledBlankVote,
                    IsForAllUsers = view.IsForAllUsers,
                    Remarks = view.Remarks,
                    StateId = view.StateId,
                };



                db.Votings.Add(voting);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.StateId = new SelectList(db.States, "StateId", "Descripcion", view.StateId);
            return View(view);
        }

        

        [Authorize(Roles = "Admin")]
        public ActionResult Edit(int? id)// visar view för att ändra info om valet 
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var voting = db.Votings.Find(id);

            if (voting == null)
            {
                return HttpNotFound();
            }

            //DateTime
            var view = new VotingView
            {
                DateEnd = voting.DateTimeEnd,
                DateStart = voting.DateTimeStart,
                Description = voting.Description,
                IsEnabledBlankVote = voting.IsEnableBlankVote,
                IsForAllUsers = voting.IsForAllUsers,
                Remarks = voting.Remarks,
                StateId = voting.StateId,
                TimeEnd = voting.DateTimeEnd,
                TimeStart = voting.DateTimeStart,
                VotingId = voting.VotingId,
            };

            ViewBag.StateId = new SelectList(db.States, "StateId", "Descripcion", voting.StateId);
            return View(view);


        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(VotingView view)// postar ändringarna man gjort i valets info
        {
            if (ModelState.IsValid)
            {
                //DateTime
                var voting = new Voting
                {
                    DateTimeEnd = view.DateEnd
                                  .AddHours(view.TimeEnd.Hour)
                                  .AddMinutes(view.TimeEnd.Minute),
                    DateTimeStart = view.DateStart
                                  .AddHours(view.TimeStart.Hour)
                                  .AddMinutes(view.TimeStart.Minute),
                    Description = view.Description,
                    IsEnableBlankVote = view.IsEnabledBlankVote,
                    IsForAllUsers = view.IsForAllUsers,
                    Remarks = view.Remarks,
                    StateId = view.StateId,
                    VotingId = view.VotingId,
                };

                db.Entry(voting).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.StateId = new SelectList(db.States, "StateId", "Descripcion", view.StateId);
            return View(view);
        }



        [Authorize(Roles = "Admin")]
        public ActionResult Delete(int? id)// visar view där man kan ta bort valet 
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Voting voting = db.Votings.Find(id);
            if (voting == null)
            {
                return HttpNotFound();
            }
            return View(voting);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)// postar att man tar bort valet 
        {
            Voting voting = db.Votings.Find(id);
            db.Votings.Remove(voting);


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

                return View(voting);
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
