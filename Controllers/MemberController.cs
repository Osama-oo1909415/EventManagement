using Microsoft.AspNetCore.Identity;
using Umbraco.Cms.Core.Security;
using EventManagement.Web.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence;

namespace EventManagement.Web.Controllers
{
    public class MemberController : RenderController
    {
        private readonly SignInManager<MemberIdentityUser> _signInManager;
        private readonly IMemberManager _memberManager;
        private readonly IMemberService _memberService;

        public MemberController(
            IUmbracoContextAccessor umbracoContextAccessor,
            IUmbracoDatabaseFactory databaseFactory,
            ServiceContext services,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IPublishedUrlProvider publishedUrlProvider,
            SignInManager<MemberIdentityUser> signInManager,
            IMemberManager memberManager)
            : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
        {
            _signInManager = signInManager;
            _memberManager = memberManager;
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
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }

            // Attempt to sign in
            var result = await _signInManager.PasswordSignInAsync(
                model.Username, 
                model.Password, 
                isPersistent: false, 
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return Redirect("/");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt");
            return CurrentUmbracoPage();
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