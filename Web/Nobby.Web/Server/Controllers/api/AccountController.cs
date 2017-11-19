namespace AspNetCoreSpa.Server.Controllers.api
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AspNetCoreSpa.Server.Filters;
    using AspNetCoreSpa.Server.ViewModels.AccountViewModels;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Nobby.Common;
    using Nobby.Data.Models;
    using Nobby.Services.Messaging;
    using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

    [Route("api/[controller]")]
    public class AccountController : BaseController
    {
        private readonly IEmailSender _emailSender;
        private readonly IOptions<IdentityOptions> _identityOptions;
        private readonly ILogger _logger;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ISmsSender _smsSender;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            IOptions<IdentityOptions> identityOptions,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            ISmsSender smsSender,
            ILoggerFactory loggerFactory)
        {
            this._userManager = userManager;
            this._identityOptions = identityOptions;
            this._signInManager = signInManager;
            this._emailSender = emailSender;
            this._smsSender = smsSender;
            this._logger = loggerFactory.CreateLogger<AccountController>();
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, set lockoutOnFailure: true
            SignInResult result = await this._signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
            if (result.Succeeded)
            {
                ApplicationUser user = await this._userManager.FindByEmailAsync(model.Email);
                IList<string> roles = await this._userManager.GetRolesAsync(user);
                this._logger.LogInformation(1, "User logged in.");
                return AppUtils.SignIn(user, roles);
            }
            if (result.RequiresTwoFactor)
            {
                return this.RedirectToAction(nameof(SendCode), new
                {
                    RememberMe = model.RememberMe
                });
            }
            if (result.IsLockedOut)
            {
                this._logger.LogWarning(2, "User account locked out.");
                return this.BadRequest(new ApiError("Lockout"));
            }
            return this.BadRequest(new ApiError("Invalid login attempt."));
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model, string returnUrl = null)
        {
            var currentUser = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.Firstname,
                LastName = model.Lastname
            };

            IdentityResult result = await this._userManager.CreateAsync(currentUser, model.Password);
            if (result.Succeeded)
            {
                // Add to roles
                IdentityResult roleAddResult = await this._userManager.AddToRoleAsync(currentUser, GlobalConstants.NormalUserRole);
                if (roleAddResult.Succeeded)
                {
                    string code = await this._userManager.GenerateEmailConfirmationTokenAsync(currentUser);

                    string host = this.Request.Scheme + "://" + this.Request.Host;
                    string callbackUrl = host + "?userId=" + currentUser.Id + "&emailConfirmCode=" + code;
                    string confirmationLink = "<a class='btn-primary' href=\"" + callbackUrl + "\">Confirm email address</a>";
                    this._logger.LogInformation(3, "User created a new account with password.");
                    await this._emailSender.SendEmailAsync(model.Email, "Registration confirmation email", confirmationLink);
                    return this.NoContent();
                }
            }
            this.AddErrors(result);
            // If we got this far, something failed, redisplay form
            return this.BadRequest(new ApiError(this.ModelState));
        }

        [HttpGet("ConfirmEmail")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return this.View("Error");
            }
            ApplicationUser user = await this._userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return this.View("Error");
            }
            IdentityResult result = await this._userManager.ConfirmEmailAsync(user, code);
            return this.View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        [HttpPost("ForgotPassword")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordViewModel model)
        {
            ApplicationUser currentUser = await this._userManager.FindByNameAsync(model.Email);
            if (currentUser == null || !await this._userManager.IsEmailConfirmedAsync(currentUser))
            {
                // Don't reveal that the user does not exist or is not confirmed
                return this.NoContent();
            }
            // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=532713
            // Send an email with this link
            string code = await this._userManager.GeneratePasswordResetTokenAsync(currentUser);

            string host = this.Request.Scheme + "://" + this.Request.Host;
            string callbackUrl = host + "?userId=" + currentUser.Id + "&passwordResetCode=" + code;
            string confirmationLink = "<a class='btn-primary' href=\"" + callbackUrl + "\">Reset your password</a>";
            await this._emailSender.SendEmailAsync(model.Email, "Forgotten password email", confirmationLink);
            return this.NoContent(); // sends 204
        }

        [HttpPost("resetpassword")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordViewModel model)
        {
            ApplicationUser user = await this._userManager.FindByNameAsync(model.Email);

            if (user == null)
            {
                // Don't reveal that the user does not exist
                return this.Ok("Reset confirmed");
            }
            IdentityResult result = await this._userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                return this.Ok("Reset confirmed");
                ;
            }
            this.AddErrors(result);
            return this.BadRequest(new ApiError(this.ModelState));
        }

        [HttpGet("SendCode")]
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl = null, bool rememberMe = false)
        {
            ApplicationUser user = await this._signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return this.BadRequest(new ApiError("Error"));
            }
            IList<string> userFactors = await this._userManager.GetValidTwoFactorProvidersAsync(user);
            List<SelectListItem> factorOptions = userFactors.Select(purpose => new SelectListItem
            {
                Text = purpose,
                Value = purpose
            }).ToList();
            return this.View(new SendCodeViewModel
            {
                Providers = factorOptions,
                ReturnUrl = returnUrl,
                RememberMe = rememberMe
            });
        }

        [HttpPost("SendCode")]
        [AllowAnonymous]
        public async Task<IActionResult> SendCode([FromBody] SendCodeViewModel model)
        {
            ApplicationUser user = await this._signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return this.BadRequest(new ApiError("Error"));
            }

            // Generate the token and send it
            string code = await this._userManager.GenerateTwoFactorTokenAsync(user, model.SelectedProvider);
            if (string.IsNullOrWhiteSpace(code))
            {
                return this.BadRequest(new ApiError("Error"));
            }

            string message = "Your security code is: " + code;
            if (model.SelectedProvider == "Email")
            {
                await this._emailSender.SendEmailAsync(user.Email, "Security Code", message);
            }
            else if (model.SelectedProvider == "Phone")
            {
                await this._smsSender.SendSmsAsync(await this._userManager.GetPhoneNumberAsync(user), message);
            }

            return this.RedirectToAction(nameof(VerifyCode), new
            {
                Provider = model.SelectedProvider,
                ReturnUrl = model.ReturnUrl,
                RememberMe = model.RememberMe
            });
        }

        [HttpGet("VerifyCode")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyCode(string provider, bool rememberMe, string returnUrl = null)
        {
            // Require that the user has already logged in via username/password or external login
            ApplicationUser user = await this._signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return this.BadRequest(new ApiError("Error"));
            }
            return this.View(new VerifyCodeViewModel
            {
                Provider = provider,
                ReturnUrl = returnUrl,
                RememberMe = rememberMe
            });
        }

        [HttpPost("VerifyCode")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            // The following code protects for brute force attacks against the two factor codes.
            // If a user enters incorrect codes for a specified amount of time then the user account
            // will be locked out for a specified amount of time.
            SignInResult result = await this._signInManager.TwoFactorSignInAsync(model.Provider, model.Code, model.RememberMe, model.RememberBrowser);
            if (result.Succeeded)
            {
                return this.RedirectToLocal(model.ReturnUrl);
            }
            if (result.IsLockedOut)
            {
                this._logger.LogWarning(7, "User account locked out.");
                return this.View("Lockout");
            }
            this.ModelState.AddModelError(string.Empty, "Invalid code.");
            return this.View(model);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> LogOff()
        {
            await this._signInManager.SignOutAsync();
            this._logger.LogInformation(4, "User logged out.");
            return this.NoContent();
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (IdentityError error in result.Errors)
            {
                this.ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private Task<ApplicationUser> GetCurrentUserAsync()
        {
            return this._userManager.GetUserAsync(this.HttpContext.User);
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (this.Url.IsLocalUrl(returnUrl))
            {
                return this.Redirect(returnUrl);
            }
            return this.RedirectToAction(nameof(HomeController.Index), "Home");
        }

        #endregion
    }
}