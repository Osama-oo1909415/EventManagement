using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.Controllers;
using System.Linq;

namespace EventManagement.Web.Controllers
{
    public class RegistrationController : UmbracoApiController
    {
        private readonly IContentService _contentService;
        private readonly IContentTypeService _contentTypeService;

        public RegistrationController(IContentService contentService, IContentTypeService contentTypeService)
        {
            _contentService = contentService;
            _contentTypeService = contentTypeService;
        }

        [HttpPost]
        public IActionResult Submit(RegistrationModel model)
        {
            if (!ModelState.IsValid)
            {
                // If the model is not valid (e.g., missing name or email), return an error.
                return BadRequest("Invalid data. Please check your inputs.");
            }

            // --- Find the "Registrations Repository" folder ---
            // 1. Get the root content items of the site.
            var rootContent = _contentService.GetRootContent().FirstOrDefault();
            if (rootContent == null) return NotFound("Site root not found.");

            // 2. Find the repository folder among the children of the homepage.
            var registrationsRepository = _contentService.GetChildren(rootContent.Id)
                .FirstOrDefault(c => c.ContentType.Alias == "registrationsRepository");

            if (registrationsRepository == null)
            {
                // This is an important check. If the folder doesn't exist, we can't save anything.
                return NotFound("Registrations Repository not found. Please create it in the content section.");
            }

            // --- Create and Save the New Registration ---
            // 1. Create a new content item. The name will be the person's name and the date.
            string registrationNodeName = $"{model.Name} - {System.DateTime.Now.ToString("yyyy-MM-dd")}";
            IContent newRegistration = _contentService.Create(registrationNodeName, registrationsRepository.Id, "registration");

            // 2. Set the properties using the aliases we defined in the Document Type.
            newRegistration.SetValue("registrantName", model.Name);
            newRegistration.SetValue("registrantEmail", model.Email);
            newRegistration.SetValue("event", model.EventId.ToString());

            // 3. Save and publish the new registration item.
            _contentService.SaveAndPublish(newRegistration);

            // Return a success message to the user.
            return Ok(new { message = $"Thank you for registering, {model.Name}! Your submission has been saved." });
        }
    }

    // This is the model for the data we expect from the form.
    public class RegistrationModel
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public int EventId { get; set; }
    }
}