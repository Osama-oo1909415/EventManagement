using EventManagement.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Security;
using Umbraco.Cms.Web.Common.Models;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;

namespace EventManagement.Web.Controllers
{
    public class MemberController : RenderController
    {
        private readonly IMemberManager _memberManager;
        private readonly IMemberService _memberService;

        public MemberController(
            ILogger<MemberController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IMemberManager memberManager,
            IMemberService memberService)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _memberManager = memberManager;
            _memberService = memberService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }

            var existingMember = await _memberManager.FindByEmailAsync(model.Email);
            if (existingMember != null)
            {
                ModelState.AddModelError("Registration", "A member with that email address already exists.");
                return CurrentUmbracoPage();
            }

            var memberIdentity = new MemberIdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                Name = model.Name
            };

            var result = await _memberManager.CreateAsync(memberIdentity, model.Password);

            if (result.Succeeded)
            {
                // Optionally, log the new member in immediately
                await _memberManager.SignInAsync(memberIdentity, isPersistent: true);
                return RedirectToCurrentUmbracoPage();
            }

            // If we got this far, something failed, add the errors to the model state
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("Registration", error.Description);
            }

            return CurrentUmbracoPage();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }

            var result = await _memberManager.PasswordSignInAsync(model.Username, model.Password, isPersistent: true, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                return RedirectToCurrentUmbracoPage();
            }
            else
            {
                ModelState.AddModelError("Login", "Invalid username or password");
                return CurrentUmbracoPage();
            }
        }

        public async Task<IActionResult> Logout()
        {
            await _memberManager.SignOutAsync();
            return RedirectToCurrentUmbracoPage();
        }
    }

    public class RegisterViewModel
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
    }

    public class LoginViewModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}