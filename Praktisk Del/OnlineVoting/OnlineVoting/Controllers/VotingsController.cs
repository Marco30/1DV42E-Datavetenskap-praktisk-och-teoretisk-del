using System;
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
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

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
                    QuantityVotes = voting.QuantityVotes,
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
        public ActionResult VoteForCandidate(int candidateId, int votingId)// röstnings funktion, används för att rösta 
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

            //kör röstnings funktionen 
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

        //---------------------------------------------------- Testar Blankröst  

        [Authorize(Roles = "User")]
        public ActionResult VoteForBlankCandidate(int candidateId, int votingId)// röstnings funktion, validerign av användare för få möjlighet att rösta blankt 
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
            if (this.VoteBlank(user, candidate, voting))
            {
                return RedirectToAction("MyVotings");
            }

            return RedirectToAction("Index", "Home");
        }

        private bool VoteBlank(Models.User user, Candidate candidate, Voting voting) // röstnings funktion för att rösta blankt 
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


               /* candidate.QuantityVotes++;// läger till en röst 

                db.Entry(candidate).State = EntityState.Modified;// läger till röst i DB*/

                voting.QuantityVotes++;

                voting.QuantityBlankVotes++;

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

        //----------------------------------------------------  

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

            ViewBag.IsEnableBlankVote = voting.IsEnableBlankVote;

            var state = db.States.Find(voting.StateId);

            ViewBag.StateDescripcion = state.Descripcion;

            return View(view);
        }


        [Authorize(Roles = "User")]
        public ActionResult MyVotings()// visar valen som är pågång 
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

        //----------------------------------------------------  Testar ny AddCandidate

        [Authorize(Roles = "Admin")]
        public ActionResult _SearchAndAddCandidate(int id)// visar en lista på alla användare man kan läga till i ett val 
        {
            List<int> RemoveID = new List<int>();

            var users = db.Users.ToList();

            for (var i = 0; i < users.Count; i++)
            {
                //nsole.WriteLine("Amount is {0} and type is {1}", myMoney[i].amount, myMoney[i].type);
                var UserId = users[i].UserId;

                var candidate = db.Candidates
                   .Where(c => c.VotingId == id &&
                               c.UserId == UserId)
                               .FirstOrDefault();

                var userContext = new ApplicationDbContext();
                var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(userContext));
                var userASP = userManager.FindByEmail(users[i].UserName);

              
                if (userManager.IsInRole(userASP.Id, "Admin"))
                {
                        RemoveID.Add(i);
                }
                else if (candidate != null)//om canditaten redan fins i valet startat fi statsen som kommer spara index för att sen ta bort den användaren från user listan på Users som kan lägas till i valet 
                {
                    RemoveID.Add(i);
                }
            }

            for (int i = RemoveID.Count - 1; i >= 0; i--)
            {
                    users.RemoveAt(RemoveID[i]);
            }

 

            ViewBag.VotingId = id;

            if (TempData["Message"] != null)
            {
                ViewBag.Message = TempData["Message"].ToString();// visar medelande som tagit med fron Edit eller delit view
            }

            return PartialView(users);

        }

    
        //Post AddCandidate
        [HttpPost]
        public ActionResult _SearchAndAddCandidate(String SearchText, int id)// visar den användare man sökt på 
        {
            List<User> UsersList;

            if (string.IsNullOrEmpty(SearchText))
            {
                UsersList = db.Users.ToList();
            }
            else
            {
                if (SearchText.Contains(" "))
                {
                    string[] array = SearchText.Split(new char[] { ' ' }, 2);

                    var FirstNameText = array[0];
                    var LastNameText = array[1];

                    UsersList = db.Users.Where(x => x.FirstName.StartsWith(FirstNameText) & x.LastName.StartsWith(LastNameText)).ToList();// söker efter förnamn och efternam man sökt på i DB för att visas i viewn 
                }
                else
                {
                    UsersList = db.Users.Where(x => x.FirstName.StartsWith(SearchText)).ToList();// söker efter förnamn man sökt på i DB för att visas i viewn 
                } 
            }

            //test------------------------------------------------

            //List<int> RemoveID = new List<int>();

            for (var i = 0; i < UsersList.Count; i++)
            {
                //nsole.WriteLine("Amount is {0} and type is {1}", myMoney[i].amount, myMoney[i].type);
              /*  var UserId = UsersList[i].UserId;

                var candidate = db.Candidates
                   .Where(c => c.VotingId == id &&
                               c.UserId == UserId)
                               .FirstOrDefault();*/

                var userContext = new ApplicationDbContext();
                var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(userContext));
                var userASP = userManager.FindByEmail(UsersList[i].UserName);


                if (userManager.IsInRole(userASP.Id, "Admin"))
                {
                    //RemoveID.Add(i);
                    ViewBag.Admin = 1;
                }
                else
                {
                    ViewBag.Admin = 0;
                }
               /* else if (candidate != null)//om canditaten redan fins i valet startat fi statsen som kommer spara index för att sen ta bort den användaren från user listan på Users som kan lägas till i valet 
                {
                    RemoveID.Add(i);
                }*/
            }

            /*for (int i = RemoveID.Count - 1; i >= 0; i--)
            {
                UsersList.RemoveAt(RemoveID[i]);
            }*/

            //-------------------------------------------------------


            ViewBag.VotingId = id;

            return PartialView("_SearchAndAddCandidate", UsersList);
            //return RedirectToAction(string.Format("Details/{0}", ViewBag.VotingId));

        }

        public JsonResult GetNameSearch(String term)// funktion som används av autocomplete jquery
        {
                List<String> UsersList;// skapar lista som kommer användas för att spara alla User från DB


            if (term.Contains(" "))
            {
                string[] array = term.Split(new char[] { ' ' }, 2);

                var FirstNameText = array[0];
                var LastNameText = array[1];

                UsersList = db.Users.Where(x => x.FirstName.StartsWith(FirstNameText) & x.LastName.StartsWith(LastNameText)).Select(y => y.FirstName + " " + y.LastName).ToList();// söker efter förnamn och efternam man sökt på i DB för att visas på autocomplete 

            }
            else
            {
                UsersList = db.Users.Where(x => x.FirstName.StartsWith(term)).Select(y => y.FirstName + " " + y.LastName).ToList();// söker efter förnamnet man sökt på i DB för att visas på autocomplete
            }


            return Json(UsersList, JsonRequestBehavior.AllowGet);
        }


        //Post AddCandidate
        [HttpPost]
        public ActionResult MakeUserToCandidate(int UserID, int VotingID, string UserFullName)// postar användare man valt till kandidat
        {

            var view = new AddCandidateView
            {
                VotingId = VotingID,
                UserId = UserID,
            };

                //så man inte lägger inte samma kandidat två gånger 
                var candidate = db.Candidates
                    .Where(c => c.VotingId == view.VotingId &&
                                c.UserId == view.UserId)
                                .FirstOrDefault();

                if (candidate != null)
                {
                    //om canditaten redan fins i valet
                    //ModelState.AddModelError(string.Empty, "The candidate already belongs to voting");
                //ViewBag.Error = "The group already belongs to voting";
                /*ViewBag.UserId = new SelectList(
                                db.Users.OrderBy(u => u.FirstName)
                                .ThenBy(u => u.LastName),

                                "UserId",
                                "FullName");*/
                //return View(view);
                TempData["Message"] = "(" + UserFullName + ") is already a candidate in this election";
                //return RedirectToAction("_SearchAndAddCandidate", "Votings", new { id = view.VotingId });
                //return Json(new { url = Url.Action("_SearchAndAddCandidate", new { id = VotingID }) });
                //return RedirectToAction("_SearchAndAddCandidate", "Votings", new { id = view.VotingId });
                return Json(new { url = Url.Action("Details", new { id = VotingID }) });
                //return PartialView("_SearchAndAddCandidat", new { id = view.VotingId });

            }

                candidate = new Candidate
                {
                    UserId = view.UserId,
                    VotingId = view.VotingId,
                };


                db.Candidates.Add(candidate);
                db.SaveChanges();
           
            TempData["Message"] = "(" + UserFullName + ") is add to this election";
            ///return PartialView("_SearchAndAddCandidat", new { id = view.VotingId });
            //return RedirectToAction("_SearchAndAddCandidat", "Votings", new { id = view.VotingId });
            //return Json(new { ok = true, url = Url.Action("Details", "Votings", new { id = view.VotingId }) });
            return Json(new { url = Url.Action("Details", new { id = VotingID }) });
            /* ViewBag.UserId = new SelectList(
                 db.Users.OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName),
                "UserId",
                "FullName");*/

        }

        //-----------------------------------------------------------------------------------------



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
                    QuantityVotes = voting.QuantityVotes,
                    Remarks = voting.Remarks,
                    StateId = voting.StateId,
                    State = voting.State,
                    VotingId = voting.VotingId,
                    Winner = user,

                });
            }

            if (TempData["Message"] != null)
            {
                ViewBag.Message = TempData["Message"].ToString();// visar medelande som tagit med fron Edit view
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


            var state = db.States.Find(voting.StateId);

            ViewBag.StateDescripcion = state.Descripcion;

            //testar-------------
            ViewBag.UserModel = db.Votings.ToList();
            //-------------------

            return View(view);
        }



        [Authorize(Roles = "Admin")]
        public ActionResult Create()// visar view där man skapar valet 
        {
           var S1 = db.States.ToList();

            foreach (var item in S1)// gör så att drop down listan som visas i skapar viewn får Open state vald 
            { 

                 if ("Open" == item.Descripcion)
                { 
                ViewBag.StateId = new SelectList(db.States, "StateId", "Descripcion", item.StateId);
                }

            }
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

                //var c = db.Votings.FirstOrDefault(s => s.VotingId = voting.VotingId);

                //return RedirectToAction("Index");
                return RedirectToAction("Details",new { id = voting.VotingId });
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

            var state = db.States.Find(voting.StateId);

            if ("Open" == state.Descripcion)// används för att kontrollera om ett val är på gong eller avslutad, är ett val avslutat så ska man inte kunna ändra något i valt, detta är en funktion som lags till för att bevara valets integritet
            {
                //DateTime
                var FixTimeStart = voting.DateTimeStart.ToString("HH:mm");//får tiden från datetime objekt 
                var FixTimeEnd = voting.DateTimeEnd.ToString("HH:mm");// får tiden från datetime objekt 
                
                var V1 = db.Votings.AsNoTracking().Where(p => p.VotingId == voting.VotingId).FirstOrDefault();

                var view = new VotingView
                {
                    /* DateEnd = voting.DateTimeEnd,
                     DateStart = voting.DateTimeStart,
                     Description = voting.Description,
                     IsEnabledBlankVote = voting.IsEnableBlankVote,
                     IsForAllUsers = voting.IsForAllUsers,
                     Remarks = voting.Remarks,
                     StateId = voting.StateId,
                     TimeEnd = voting.DateTimeEnd,
                     TimeStart = voting.DateTimeStart,
                     VotingId = voting.VotingId,*/

                    CandidateWinId = voting.CandidateWinId,
                    DateEnd = voting.DateTimeEnd.Date,
                    DateStart = voting.DateTimeStart.Date,
                    TimeStart = DateTime.Parse(FixTimeStart, System.Globalization.CultureInfo.CurrentCulture),// start tid
                    TimeEnd = DateTime.Parse(FixTimeEnd, System.Globalization.CultureInfo.CurrentCulture),//slut tid 
                    Description = voting.Description,
                    IsEnabledBlankVote = voting.IsEnableBlankVote,
                    IsForAllUsers = voting.IsForAllUsers,
                    QuantityBlankVotes = V1.QuantityBlankVotes,
                    QuantityVotes = V1.QuantityVotes,
                    Remarks = voting.Remarks,
                    StateId = voting.StateId,
                    VotingId = voting.VotingId,

                };

                ViewBag.StateId = new SelectList(db.States, "StateId", "Descripcion", voting.StateId);

                return View(view);
            }
            else
            {
                TempData["Message"] = "You tried to Edit (" + voting.Description + "), This election is finished and can not be edited anymore!";
                return RedirectToAction("Index", "votings");
            }


        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(VotingView view)// postar ändringarna man gjort i valets info
        {

            //Voting voting1 = db.Votings.Find(view.VotingId);
            // används för att inte Entity Framework inte ska binda sig till state modelen så att den längre ner kan updateras utan problem
            var V1 = db.Votings.AsNoTracking().Where(p => p.VotingId == view.VotingId).FirstOrDefault();
            var state = db.States.Find(V1.StateId);

            // används här i den här post funktionen för att hindra post attacker som kan göras genom URL, man postar ändrignar som int ska gå att göras
            if ("Open" == state.Descripcion & V1.QuantityVotes == view.QuantityVotes & V1.QuantityBlankVotes == view.QuantityBlankVotes & V1.CandidateWinId == view.CandidateWinId)// används för att kontrollera om ett val är på gong eller avslutad, är ett val avslutat så ska man inte kunna ändra något i valt, detta är en funktion som lags till för att bevara valets integritet
            {
                if (ModelState.IsValid)
                {

                    TimeSpan timeOfEnd = view.TimeEnd.TimeOfDay;// kommer användas för att längre ner slå ihop tid och datume 
                    TimeSpan timeOfStart = view.TimeStart.TimeOfDay;// kommer användas för att längre ner slå ihop tid och datume 
                    //DateTime
                    var voting = new Voting
                    {
                        /*DateTimeEnd = view.DateEnd
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
                        VotingId = view.VotingId,*/

                        CandidateWinId = view.CandidateWinId,
                        DateTimeEnd = view.DateEnd.Add(timeOfEnd),// slåtr ihop tid i datetime objekt 
                        DateTimeStart = view.DateStart.Add(timeOfStart),// slåtr ihop tid i datetime objekt 
                        Description = view.Description,
                        IsEnableBlankVote = view.IsEnabledBlankVote,
                        IsForAllUsers = view.IsForAllUsers,
                        QuantityBlankVotes = view.QuantityBlankVotes,
                        QuantityVotes = view.QuantityVotes,
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
            else
            {
                TempData["Message"] = "You tried to use the URL to post Edit (" + V1.Description + "), This election is finished and can not be edited anymore!";
                return RedirectToAction("Index", "votings");
            }

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
