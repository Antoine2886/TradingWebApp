using Bd.Infrastructure;
using IdentityCore.Infrastructure;
using IdentityCore.ViewModels.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using System.Text.Encodings.Web;
using WebApp.Utilities.Email;
using WebApp.Utilities.Token;
using WebApp.ViewModels.Account;
namespace IdentityCore.Controllers
{
    /// <summary>
    /// Author: Antoine Bélanger and Sphero
    /// Description: Controller to handle user account-related actions such as login, registration, password reset, and email verification.
    /// </summary>
    public class AccountController : Controller
    {
        private readonly SignInManager<AppUser> SignInManager;
        private readonly UserManager<AppUser> UserManager;
        private readonly IEmailSender _emailSender;
        private readonly ITokenRepository _tokenRepository;
        private readonly Context _context;

        /// <summary>
        /// Constructor to initialize dependencies.
        /// </summary>
        public AccountController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager, IEmailSender emailSender, ITokenRepository tokenRepository, Context context)
        {
            SignInManager = signInManager;
            UserManager = userManager;
            _emailSender = emailSender;
            _tokenRepository = tokenRepository;
            _context = context;
        }

        //[HttpGet]
        //public IActionResult ResetPassword(string email, string token)
        //{
        //    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
        //    {
        //        // Invalid or missing email/token parameters
        //        return RedirectToAction("Error");
        //    }

        //    var model = new ResetPasswordViewModel
        //    {
        //        Email = email,
        //        Token = token
        //    };

        //    return View(model);
        //}


        /// <summary>
        /// Handles the forgot password form submission.
        /// Generates a password reset token and sends an email with the reset link.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordVM model)
        {
            // Generate email confirmation token
            try
            {
                var user = await UserManager.FindByNameAsync(model.UserName!);
                if (user != null)
                {
                    var token = await _tokenRepository.CreateTokenAsync(user!.Id);
                    var token2 = await UserManager.GeneratePasswordResetTokenAsync(user!);

                    var resetPasswordLink = Url.Action("ResetPassword", "Account", new { userId = user!.Id, token = token, token2 = token2 }, Request.Scheme);
                    await _emailSender.SendEmailAsync(model.UserName!, "Reset your password",
                    $"Please reset your password by clicking this link : {resetPasswordLink}");
                }


                ViewBag.EmailVerificationSent = true;
            }
            catch (Exception ex)
            {
                return View();
            }



            return View(model);
        }

        /// <summary>
        /// Displays the forgot password view.
        /// </summary>
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        /// <summary>
        /// Displays the reset password view.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string userId, string token, string token2)
        {
            if (userId == null || token == null || token2 == null)
            {
                return RedirectToAction("Error");
            }

            var user = await UserManager.FindByIdAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Error");
            }



            var model = new ResetPasswordVM { UserId = userId, Token = token, Token2 = token2 };
            return View(model);
        }

        /// <summary>
        /// Handles the reset password form submission.
        /// Resets the user's password and deletes the used token.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordVM vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm); // Show the form with validation errors
            }

            var user = await UserManager.FindByIdAsync(vm.UserId!);
            if (user == null)
            {
                return RedirectToAction("Error");
            }

            // Reset the user's password
            var result = await UserManager.ResetPasswordAsync(user, vm.Token2!, vm.Password!);
            if (!Guid.TryParse(vm.UserId, out var parsedUserId))
            {
                return RedirectToAction("Error");
            }


            if (result.Succeeded)
            {

                await _tokenRepository.DeleteTokenAsync(parsedUserId, vm.Token!);
                // Password reset successful, redirect to a confirmation page or login page
                TempData["PasswordResetConfirmation"] = "Your password has been successfully changed.";
                return RedirectToAction("LogIn");
            }
            else
            {
                // Password reset failed, add errors to ModelState
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(vm); // Show the form with errors
            }
        }

        /// <summary>
        /// Displays the login view.
        /// </summary>
        [HttpGet]
        public IActionResult LogIn(string? returnUrl)
        {
            if (TempData.ContainsKey("EmailVerifiedMessage"))
            {
                ViewBag.EmailVerifiedMessage = TempData["EmailVerifiedMessage"];
            }
            if (TempData.ContainsKey("PasswordResetConfirmation"))
            {
                ViewBag.PasswordResetConfirmation = TempData["PasswordResetConfirmation"];
            }

            ViewBag.ReturnUrl = returnUrl ?? "/";

            // Read the "RememberMe" cookie
            var rememberedUserName = GetCookie("RememberMe");
            if (!string.IsNullOrEmpty(rememberedUserName))
            {
                ViewBag.RememberedUserName = rememberedUserName;
            }

            return View();
        }

        /// <summary>
        /// Handles the login form submission.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> LogIn(LogInVM vm, string? returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var user = await UserManager.FindByNameAsync(vm.UserName!);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid Login Attempt!");
                ViewBag.ReturnUrl = returnUrl;
                return View(vm);
            }
            if (!user.EmailConfirmed)
            {
                ModelState.AddModelError(string.Empty, "Please confirm your email before logging in. A new email has been sent");

                var token = await _tokenRepository.CreateTokenAsync(user!.Id);

                var confirmationLink = Url.Action("EmailVerified", "Account", new { userId = user.Id, token = token }, Request.Scheme);
                await _emailSender.SendEmailAsync(vm.UserName!, "Confirm your email",
                    $"Please confirm your account by clicking this link : {confirmationLink}");

                ViewBag.EmailVerificationSent = true;
                ViewBag.ReturnUrl = returnUrl;
                return View(vm);
            }

            var result = await SignInManager.PasswordSignInAsync(vm.UserName!, vm.Password!, vm.RememberMe, false);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Invalid Login Attempt!");
                ViewBag.ReturnUrl = returnUrl;
                return View(vm);
            }

            // Set the "RememberMe" cookie if the user opted for it
            if (vm.RememberMe)
            {
                SetCookie("RememberMe", vm.UserName!, 43200); // Cookie expires in 30 days
            }
            else
            {
                DeleteCookie("RememberMe");
            }

            if (string.IsNullOrEmpty(returnUrl))
            {
                return RedirectToAction("Index", "Home");
            }

            return Redirect(returnUrl);
        }


        /// <summary>
        /// Displays the registration view.
        /// </summary
        public IActionResult Register() => View();

        /// <summary>
        /// Handles the registration form submission.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Register(RegisterVM vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            if (!vm.TermsAccepted)
            {
                ModelState.AddModelError(string.Empty, "Please accept the terms.");
                return View(vm);
            }

            var existingUser = await UserManager.FindByNameAsync(vm.VisibleName!.Trim());
            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "Username is already in use.");
                return View(vm);
            }

            var user = new AppUser(vm.VisibleName!.Trim())
            {
                UserName = vm.Email!.Trim(),
                VisibleName = vm.VisibleName!.Trim(),
                ProfilePicture = "/images/user.jpg"
            };

            var result = await UserManager.CreateAsync(user, vm.Password!);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(vm);
            }

            var token = await _tokenRepository.CreateTokenAsync(user!.Id);
            var confirmationLink = Url.Action("EmailVerified", "Account", new { userId = user.Id, token = token }, Request.Scheme);

            var emailContent = $"<p>Dear {user.VisibleName},</p>" +
                               "<p>Thank you for registering. Please confirm your email by clicking the link below:</p>" +
                               $"<p><a href='{confirmationLink}'>Confirm Email</a></p>" +
                               "<p>If you did not register, please ignore this email.</p>" +
                               "<p>Thank you, <br /> Your Company Name</p>";

            await _emailSender.SendEmailAsync(vm.Email, "Confirm your email", emailContent);

            ViewBag.EmailVerificationSent = true;

            return View(vm);
        }

        /// <summary>
        /// Verifies the user's email address using the provided token.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EmailVerified(string userId, string token)
        {
            if (userId == null || token == null)
            {
                return RedirectToAction("Error");
            }

            var user = await UserManager.FindByIdAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Error");
            }

            if (!Guid.TryParse(userId, out var parsedUserId))
            {
                return RedirectToAction("Error");
            }
            var isTokenValid = await _tokenRepository.IsValidTokenAsync(parsedUserId, token);
            if (!isTokenValid)
            {
                // Token is not valid, handle accordingly (e.g., show error page)
                return RedirectToAction("Error");
            }
            if (isTokenValid)
            {
                user.EmailConfirmed = true;
                var updateResult = await UserManager.UpdateAsync(user);
               // var roleResult = await UserManager.AddToRoleAsync(user, "Utilisateur"); // laterrr *********************************************************888
                if (updateResult.Succeeded) //&& roleResult.Succeeded)
                {
                    TempData["EmailVerifiedMessage"] = "Your email has been successfully verified.";
                    await _tokenRepository.DeleteTokenAsync(parsedUserId, token);
                    // Redirect to a page indicating successful email verification
                    return RedirectToAction("LogIn");
                }
                else
                {
                    // Handle failure
                    return RedirectToAction("Error");
                }
            }
            else
            {
                // Handle failure
                return RedirectToAction("Error");
            }
        }



        /// <summary>
        /// Logs the user out.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> LogOut()
        {
            await SignInManager.SignOutAsync();
            return RedirectToAction("LogIn", "Account");
        }

        /// <summary>
        /// Handles the confirmation of account deletion.
        /// </summary>
        public async Task<IActionResult> ConfirmDeleteAccount(Guid userId, string token)
        {
            AppUser user = await UserManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return NotFound();
            }

            // Verify the token
            var isValidToken = await UserManager.VerifyUserTokenAsync(user, "Default", "DeleteAccount", token);
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
            var result = await UserManager.DeleteAsync(user);
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
            await SignInManager.SignOutAsync();

            return RedirectToAction("LogIn", "Account");
        }

        /// <summary>
        /// Sets a cookie with the specified key, value, and expiration time.
        /// </summary>
        public void SetCookie(string key, string value, int? expireTime)
        {
            CookieOptions option = new CookieOptions();

            if (expireTime.HasValue)
                option.Expires = DateTime.Now.AddMinutes(expireTime.Value);
            else
                option.Expires = DateTime.Now.AddMinutes(10);

            Response.Cookies.Append(key, value, option);
        }

        /// <summary>
        /// Retrieves the value of a cookie with the specified key.
        /// </summary>
        public string GetCookie(string key)
        {
            return Request.Cookies[key];
        }

        /// <summary>
        /// Deletes a cookie with the specified key.
        /// </summary>
        public void DeleteCookie(string key)
        {
            Response.Cookies.Delete(key);
        }

    }
}
