using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WebApp.ViewModels.Community;
using Bd.Infrastructure;
using IdentityCore.Infrastructure;
using System.Security.Claims;
using System.Drawing.Printing;
using Bd.Infrastructure;
using WebApp.ViewModels.Settings;
using Microsoft.AspNetCore.Identity;
using SixLabors.ImageSharp;
using WebApp.Utilities.Email;

namespace WebApp.Controllers
{
    /// <summary>
    /// Author: Sphero
    /// Description: Controller to handle user settings including profile updates, account deletion, and billing information.
    /// </summary>
    public class SettingsController : Controller
    {
        private readonly Context _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IEmailSender _emailSender;
        public SettingsController(Context context, IEmailSender emailSender, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
           _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
        }

        /// <summary>
        /// Displays the user settings page.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userId = !string.IsNullOrEmpty(userIdString) ? Guid.Parse(userIdString) : Guid.Empty;

            var user = await _context.Users
                .Include(u => u.Posts)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound();
            }

            var model = new UserSettingsViewModel
            {
                newUserName = user.VisibleName,
                profilePicture = null,
                ExistingAvatarPath = user.ProfilePicture,
                user = user
            };

            return View(user);
        }

        /// <summary>
        /// Handles user settings updates including username and profile picture.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> makeChanges(UserSettingsViewModel model, IFormFile profilePicture)
        {
            // ModelState is invalid because user and profilepicture does not have a value filled by form
            if (ModelState.IsValid)
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userId = !string.IsNullOrEmpty(userIdString) ? Guid.Parse(userIdString) : Guid.Empty;

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound();
                }

                // Update Username
                if (!string.IsNullOrEmpty(model.newUserName))
                {
                    user.VisibleName = model.newUserName;
                }

                // Update Profile Picture
                if (profilePicture != null && profilePicture.Length > 0)
                {
                    // Check file size (e.g., limit to 5 MB)
                    if (profilePicture.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("ProfilePicture", "The file size exceeds the 5 MB limit.");
                        return RedirectToAction("Index");
                    }

                    // Check file type (e.g., only allow JPEG, PNG, or GIF)
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var fileExtension = Path.GetExtension(profilePicture.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("ProfilePicture", "Invalid file type. Only JPEG, PNG, and GIF are allowed.");
                        return RedirectToAction("Index");
                    }

                    // Validate that the file can be loaded as an image
                    try
                    {
                        using (var image = Image.Load(profilePicture.OpenReadStream()))
                        {
                            // Optionally perform additional checks like dimensions
                            if (image.Width <= 0 || image.Height <= 0)
                            {
                                ModelState.AddModelError("ProfilePicture", "Invalid image dimensions.");
                                return RedirectToAction("Index");
                            }
                        }
                    }
                    catch
                    {
                        ModelState.AddModelError("ProfilePicture", "The file is not a valid image.");
                        return RedirectToAction("Index");
                    }

                    // Generate a unique file name and save the file
                    var fileName = $"{Guid.NewGuid()}_{profilePicture.FileName}";
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await profilePicture.CopyToAsync(stream);
                    }

                    user.ProfilePicture = $"/images/{fileName}";
                }

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            // Check for validation errors
            foreach (var error in ModelState)
            {
                var key = error.Key;
                var errors = error.Value.Errors;
                foreach (var errorss in errors)
                {
                    // Log or inspect the errors
                    Console.WriteLine($"{key}: {errorss.ErrorMessage}");
                }
            }

            // If we got this far, something failed; redisplay form
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Updates the username of the logged-in user.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateUsername(string newUserName)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userId = !string.IsNullOrEmpty(userIdString) ? Guid.Parse(userIdString) : Guid.Empty;

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            user.VisibleName = newUserName;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Updates the profile picture of the logged-in user.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateProfilePicture(IFormFile profilePicture)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userId = !string.IsNullOrEmpty(userIdString) ? Guid.Parse(userIdString) : Guid.Empty;

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            if (profilePicture != null && profilePicture.Length > 0)
            {
                // Check file size (e.g., limit to 5 MB)
                if (profilePicture.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("ProfilePicture", "The file size exceeds the 5 MB limit.");
                    return RedirectToAction("Index");
                }

                // Check file type (e.g., only allow JPEG, PNG, or GIF)
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(profilePicture.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("ProfilePicture", "Invalid file type. Only JPEG, PNG, and GIF are allowed.");
                    return RedirectToAction("Index");
                }

                // Validate that the file can be loaded as an image
                try
                {
                    using (var image = Image.Load(profilePicture.OpenReadStream()))
                    {
                        // Optionally perform additional checks like dimensions
                        // Example: Check if the image has dimensions
                        if (image.Width <= 0 || image.Height <= 0)
                        {
                            ModelState.AddModelError("ProfilePicture", "Invalid image dimensions.");
                            return RedirectToAction("Index");
                        }
                    }
                }
                catch
                {
                    ModelState.AddModelError("ProfilePicture", "The file is not a valid image.");
                    return RedirectToAction("Index");
                }

                // Generate a unique file name and save the file
                var fileName = $"{Guid.NewGuid()}_{profilePicture.FileName}";
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePicture.CopyToAsync(stream);
                }

                user.ProfilePicture = $"/images/{fileName}";
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Initiates account deletion process for the logged-in user.
        /// </summary>
        public async Task<IActionResult> DeleteAccount()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userId = !string.IsNullOrEmpty(userIdString) ? Guid.Parse(userIdString) : Guid.Empty;

            AppUser user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return NotFound();
            }

            // Generate a token for account deletion
            var token = await _userManager.GenerateUserTokenAsync(user, "Default", "DeleteAccount");

            // Create the confirmation link
            var callbackUrl = Url.Action("ConfirmDeleteAccount", "Account", new { userId = user.Id, token = token }, protocol: HttpContext.Request.Scheme);

            // Send the email
            await _emailSender.SendEmailAsync(user.UserName, "Confirm Account Deletion",
                $"Please confirm your account deletion by clicking <a href='{callbackUrl}'>here</a>.");

            return RedirectToAction("LogIn", "Account");
        }


        /// <summary>
        /// Confirms account deletion for the user.
        /// </summary>
        public async Task<IActionResult> ConfirmDeleteAccount(Guid userId, string token)
        {
            AppUser user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return NotFound();
            }

            // Verify the token
            var isValidToken = await _userManager.VerifyUserTokenAsync(user, "Default", "DeleteAccount", token);
            if (!isValidToken)
            {
                return View("Error");
            }

            // First, remove all likes corresponding to the user
            var likesToDelete = _context.likes.Where(l => l.UserId == userId);
            _context.likes.RemoveRange(likesToDelete);

            // Save changes to delete the likes first
            await _context.SaveChangesAsync();

            // Remove the user using UserManager
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                // Handle deletion failure
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View("Error");
            }

            // Log out the user if they are currently logged in
            await _signInManager.SignOutAsync();

            return RedirectToAction("LogIn", "Account");
        }

        /// <summary>
        /// Displays the billing information page.
        /// </summary>
        public async Task<IActionResult> Billing()
        {
            List<string> subscription_names = new List<string>{"basic", "medium", "pro" };
            List<double> subscription_prices = new List<double> { 5, 10, 20 };
            List<string> subscription_descriptions = new List<string> { "basic tier with access to some things", "mid tier access with some more things",  "pro tier with access to more things"};
            List<string> subscription_priceId = new List<string> { "price_1PTZrX1P2nEddnYBJQzDbsTQ", "price_1PTZrh1P2nEddnYBasRQ6P5f", "price_1PTZrs1P2nEddnYBGafIVAg6" };
            var user = await getUser();

            var billingVM = new BillingVM
            {
                user = user,
                subscription_options_name = subscription_names,
                subscription_options_description = subscription_descriptions,
                subscription_options_price = subscription_prices,
                subscription_options_priceId = subscription_priceId
            };

            return View(billingVM);
        }

        /// <summary>
        /// Retrieves the current logged-in user.
        /// </summary>
        public async Task<AppUser> getUser()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userId = !string.IsNullOrEmpty(userIdString) ? Guid.Parse(userIdString) : Guid.Empty;

            AppUser user = await _userManager.FindByIdAsync(userId.ToString());

            return user;
        }

    }
}
