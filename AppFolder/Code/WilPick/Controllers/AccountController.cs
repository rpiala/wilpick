using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration.UserSecrets;
using WilPick.Data;
using WilPick.Models;
using WilPick.ViewModels;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Constants = WilPick.Common.Constant;

namespace WilPick.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<Users> signInManager;
        private readonly UserManager<Users> userManager;
        private readonly DataHelper _helper;

        public AccountController(SignInManager<Users> signInManager, UserManager<Users> userManager, DataHelper helper)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            _helper = helper;
        }        

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);                
                
                if (result.Succeeded)
                {
                    _helper.CreateLoginLogoutTransactionLog(Constants.LOGINTRANSTYPE, model.Email);

                    var dt = await _helper.GetTableDataAsync(
                        "COLUMNS{:}*{|}TABLES{:}dbo.AspNetUsers");

                    var dtModel = await _helper.GetTableDataModelAsync<Users>(
                        "COLUMNS{:}*{|}TABLES{:}dbo.AspNetUsers");

                    //var permutations = await _helper.GetTableDataAsync(
                    //    "COLUMNS{:}*{|}TABLES{:}dbo.ufn_Permutations4(0, 0)");                    

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Email or password is incorrect.");
                    return View(model);
                }
            }
            return View(model);
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyAgentCode(string agentCode)
        {
            if (string.IsNullOrWhiteSpace(agentCode))
            {
                return Json("Agent code is required.");
            }

            var exists = await _helper.AgentCodeExistsAsync(agentCode);
            if (exists)
            {
                // Remote expects 'true' for valid
                return Json(true);
            }

            return Json("Agent code not found.");
        }

        

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                Users users = new Users
                {
                    FullName = model.Name,
                    Email = model.Email,
                    UserName = model.Email,
                };

                var result = await userManager.CreateAsync(users, model.Password);
               
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    return View(model);
                }

                WpAppUserViewModel createUser = new WpAppUserViewModel
                {
                    AspNetUserId = users.Id,
                    AgentCode = model.AgentCode,
                    UserName = users.UserName,
                    Email = users.Email,
                    FirstName = model.Name,
                    LastName = "",
                    MiddleName = "",
                    BetTicketPrice = Constants.BETTICKETPRICE,
                    WinningPrize = Constants.WINNINGPRICE
                };
                
                _helper.CreateWpAppUser(createUser);

                //userManager.DeleteAsync(users);

                return RedirectToAction("Login", "Account");

            }
            return View(model);
        }

        public IActionResult VerifyEmail()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.FindByNameAsync(model.Email);

                if(user == null)
                {
                    ModelState.AddModelError("", "Something is wrong!");
                    return View(model);
                }
                else
                {
                    return RedirectToAction("ChangePassword","Account", new {username = user.UserName});
                }
            }
            return View(model);
        }

        public IActionResult ChangePassword(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("VerifyEmail", "Account");
            }
            return View(new ChangePasswordViewModel { Email= username });
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if(ModelState.IsValid)
            {
                var user = await userManager.FindByNameAsync(model.Email);
                if(user != null)
                {
                    var result = await userManager.RemovePasswordAsync(user);
                    if (result.Succeeded)
                    {
                        result = await userManager.AddPasswordAsync(user, model.NewPassword);
                        return RedirectToAction("Login", "Account");
                    }
                    else
                    {

                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }

                        return View(model);
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Email not found!");
                    return View(model);
                }
            }
            else
            {
                ModelState.AddModelError("", "Something went wrong. try again.");
                return View(model);
            }
        }

        public async Task<IActionResult> Logout()
        {
            // Get the currently logged-in Users entity (null if not authenticated)
            var user = await userManager.GetUserAsync(User);

            if (user != null)
            {
                // Example: access properties
                var email = user.Email;
                var username = user.UserName;

                // Optional: create a logout log entry using your helper
                _helper.CreateLoginLogoutTransactionLog(Constants.LOGOUTTRANSTYPE, email);
            }

            await signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }        

        [HttpPost]
        [IgnoreAntiforgeryToken] // sendBeacon cannot include antiforgery token reliably; keep this endpoint lightweight
        public async Task<IActionResult> LogoutOnClose()
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                var user = await userManager.GetUserAsync(User);
                if (user != null)
                {
                    //_helper.CreateLoginLogoutTransactionLog(Constants.LOGOUTTRANSTYPE, user.Email);
                }

                //await signInManager.SignOutAsync();
            }

            return Ok();
        }
    }
}
