using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin.Security;
using OnlineVoting.Models;
using System.IO;
using OnlineVoting.Models.Repository;

namespace OnlineVoting.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private IUserRepository _userRepository;
        private IAccountRepository _accountRepository;

        public AccountController()
        {
            _accountRepository = new AccountRepository();
            _userRepository = new UserRepository();

        }


        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        // public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {

                var user = await _accountRepository.GetUserByEmailAndPassword(model.UserName, model.Password);// hämtar användar från Db genom metod i AccountRepository

      
                    if (user != null)
                    {
                        await SignInAsync(user, model.RememberMe);
                        return RedirectToLocal(returnUrl);
                    }
                    else
                    {
                        ModelState.AddModelError("", "Invalid username or password.");
                    }
        

            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterUserView userView)
        {
            if (ModelState.IsValid)
            {
                //Upload image
                string path = string.Empty;
                string pic = string.Empty;

                if (userView.Photo != null)
                {
                    //pic=  photo name  
                    pic = Path.GetFileName(userView.Photo.FileName);
                    //path= folder to save photos         ~= relative rute
                    path = Path.Combine(Server.MapPath("~/Content/Photos"), pic);
                    // local save 
                    userView.Photo.SaveAs(path);
                    //up photo
                    using (MemoryStream ms = new MemoryStream())
                    {
                        userView.Photo.InputStream.CopyTo(ms);
                        byte[] array = ms.GetBuffer();
                    }
                }

                //Save record
                var user = new User
                {
                    Adress = userView.Adress,
                    FirstName = userView.FirstName,
                    LastName = userView.LastName,
                    Phone = userView.Phone,
                    Photo = string.IsNullOrEmpty(pic) ? string.Empty : string.Format("~/Content/Photos/{0}", pic),
                    UserName = userView.UserName,

                };


                _userRepository.Add(user);// läger till anändar i DB

                // try catch for validation of error    
                try
                {

                    _userRepository.Save();

                    var userASP = _accountRepository.CreatesUserInASPdb(userView);// skpar användar i Asp.net DB


                    // auto login   -   isPersistent=remember in the sesion
                    await SignInAsync(userASP, isPersistent: false);
                    return RedirectToAction("Index", "Home");

                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null &&
                        ex.InnerException.InnerException != null &&
                        ex.InnerException.InnerException.Message.Contains("UserNameIndex"))
                    {
                        ModelState.AddModelError(string.Empty, "The email has already used for another user");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, ex.Message);

                    }

                    return View(userView);
                }
            }

            // If we reach this point , it is that it was an error and re- display the form
            return View(userView);
        }



        //
        // POST: /Account/Disassociate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Disassociate(string loginProvider, string providerKey)
        {
            ManageMessageId? message = null;

            IdentityResult result = await _accountRepository.RemoveLoginInASPdb(User.Identity.GetUserId(), loginProvider, providerKey);


            if (result.Succeeded)
            {
                message = ManageMessageId.RemoveLoginSuccess;
            }
            else
            {
                message = ManageMessageId.Error;
            }
            return RedirectToAction("Manage", new { Message = message });
        }

        //
        // GET: /Account/Manage
        public ActionResult Manage(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            ViewBag.HasLocalPassword = HasPassword();
            ViewBag.ReturnUrl = Url.Action("Manage");
            return View();
        }

        //
        // POST: /Account/Manage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Manage(ManageUserViewModel model)
        {
            bool hasPassword = HasPassword();
            ViewBag.HasLocalPassword = hasPassword;
            ViewBag.ReturnUrl = Url.Action("Manage");

            if (hasPassword)
            {
                if (ModelState.IsValid)
                {
                    IdentityResult result = await _accountRepository.ChangePasswordForUserInASPdb(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);// ändrar lösen i asp.net DB 
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Manage", new { Message = ManageMessageId.ChangePasswordSuccess });
                    }
                    else
                    {
                        AddErrors(result);
                    }
                }
            }
            else
            {
                // User does not have a password so remove any validation errors caused by a missing OldPassword field
                ModelState state = ModelState["OldPassword"];
                if (state != null)
                {
                    state.Errors.Clear();
                }

                if (ModelState.IsValid)
                {
                    IdentityResult result = await _accountRepository.AddPasswordForUserToASPdb(User.Identity.GetUserId(), model.NewPassword);// Läger till ny password til en användare i DB 
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Manage", new { Message = ManageMessageId.SetPasswordSuccess });
                    }
                    else
                    {
                        AddErrors(result);
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }


        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut();
            return RedirectToAction("Index", "Home");
        }


        [ChildActionOnly]
        public ActionResult RemoveAccountList()
        {
            var linkedAccounts = _accountRepository.GetUsersLoginsInfoFromASPdb(User.Identity.GetUserId());

            ViewBag.ShowRemoveButton = HasPassword() || linkedAccounts.Count > 1;
            return (ActionResult)PartialView("_RemoveAccountPartial", linkedAccounts);
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

        private async Task SignInAsync(ApplicationUser user, bool isPersistent)
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ExternalCookie);

            var identity = await _accountRepository.ControlIdentityInASPdb(user);
            //await UserManager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie);

            AuthenticationManager.SignIn(new AuthenticationProperties() { IsPersistent = isPersistent }, identity);
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private bool HasPassword()
        {
            var user = _accountRepository.GetUserByIdInASPdb(User.Identity.GetUserId());// hittar användar i ASP.net Db ut i från id

            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            Error
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        private class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri) : this(provider, redirectUri, null)
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
                var properties = new AuthenticationProperties() { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}