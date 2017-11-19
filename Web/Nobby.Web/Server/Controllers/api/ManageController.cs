namespace AspNetCoreSpa.Server.Controllers.api
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using AspNetCoreSpa.Server.Extensions;
    using AspNetCoreSpa.Server.Filters;
    using AspNetCoreSpa.Server.ViewModels.ManageViewModels;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Nobby.Data;
    using Nobby.Data.Common.Repositories;
    using Nobby.Data.Models;
    using Nobby.Services.Messaging;

    [Route("api/[controller]")]
    public class ManageController : BaseController
    {
        private readonly IRepository<ApplicationUserPhoto> _photosRepository;
        private readonly IEmailSender _emailSender;
        private readonly ILogger _logger;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ISmsSender _smsSender;
        private readonly UserManager<ApplicationUser> _userManager;

        public ManageController(
            IRepository<ApplicationUserPhoto> photos,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            ISmsSender smsSender,
            ILoggerFactory loggerFactory)
        {
            this._photosRepository = photos;
            this._userManager = userManager;
            this._signInManager = signInManager;
            this._emailSender = emailSender;
            this._smsSender = smsSender;
            this._logger = loggerFactory.CreateLogger<ManageController>();
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(ManageMessageId? message = null)
        {
            this.ViewData["StatusMessage"] =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.SetTwoFactorSuccess ? "Your two-factor authentication provider has been set."
                : message == ManageMessageId.Error ? "An error has occurred."
                : message == ManageMessageId.AddPhoneSuccess ? "Your phone number was added."
                : message == ManageMessageId.RemovePhoneSuccess ? "Your phone number was removed."
                : "";

            ApplicationUser user = await this.GetCurrentUserAsync();
            var model = new IndexViewModel
            {
                HasPassword = await this._userManager.HasPasswordAsync(user),
                PhoneNumber = await this._userManager.GetPhoneNumberAsync(user),
                TwoFactor = await this._userManager.GetTwoFactorEnabledAsync(user),
                BrowserRemembered = await this._signInManager.IsTwoFactorClientRememberedAsync(user)
            };
            return this.View(model);
        }

        [HttpGet("getlogins")]
        public async Task<IActionResult> GetLogins()
        {
            ApplicationUser user = await this.GetCurrentUserAsync();
            return this.Ok(await this._userManager.GetLoginsAsync(user));
        }

        [HttpPost("removelogin")]
        public async Task<IActionResult> RemoveLogin([FromBody] RemoveLoginViewModel account)
        {
            ApplicationUser user = await this.GetCurrentUserAsync();
            IdentityResult result = await this._userManager.RemoveLoginAsync(user, account.LoginProvider, account.ProviderKey);
            if (result.Succeeded)
            {
                return this.NoContent();
            }
            return this.BadRequest(new ApiError("Login cannot be removed"));
        }

        [HttpPost("addphonenumber")]
        public async Task<IActionResult> AddPhoneNumber(AddPhoneNumberViewModel model)
        {
            // Generate the token and send it
            ApplicationUser user = await this.GetCurrentUserAsync();
            string code = await this._userManager.GenerateChangePhoneNumberTokenAsync(user, model.PhoneNumber);
            await this._smsSender.SendSmsAsync(model.PhoneNumber, "Your security code is: " + code);
            return this.NoContent();
        }

        [HttpPost("enabletwofactorauthentication")]
        public async Task<IActionResult> EnableTwoFactorAuthentication()
        {
            ApplicationUser user = await this.GetCurrentUserAsync();
            if (user != null)
            {
                await this._userManager.SetTwoFactorEnabledAsync(user, true);
                this._logger.LogInformation(1, "User enabled two-factor authentication.");
            }
            return this.NoContent();
        }

        [HttpPost("disabletwofactorauthentication")]
        public async Task<IActionResult> DisableTwoFactorAuthentication()
        {
            ApplicationUser user = await this.GetCurrentUserAsync();
            await this._userManager.SetTwoFactorEnabledAsync(user, false);
            this._logger.LogInformation(2, "User disabled two-factor authentication.");
            return this.NoContent();
        }

        [HttpGet("verifyphonenumber")]
        public async Task<IActionResult> VerifyPhoneNumber(string phoneNumber)
        {
            string code = await this._userManager.GenerateChangePhoneNumberTokenAsync(await this.GetCurrentUserAsync(), phoneNumber);
            // Send an SMS to verify the phone number
            if (string.IsNullOrEmpty(phoneNumber))
            {
                return this.BadRequest(new ApiError("Unable to verify phone number"));
            }
            return this.NoContent();
        }

        [HttpPost("verifyphonenumber")]
        public async Task<IActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model)
        {
            ApplicationUser user = await this.GetCurrentUserAsync();
            IdentityResult result = await this._userManager.ChangePhoneNumberAsync(user, model.PhoneNumber, model.Code);
            if (result.Succeeded)
            {
                return this.NoContent();
            }
            // If we got this far, something failed, redisplay the form
            return this.BadRequest(new ApiError("Failed to verify phone number"));
        }

        [HttpPost("removephonenumber")]
        public async Task<IActionResult> RemovePhoneNumber()
        {
            ApplicationUser user = await this.GetCurrentUserAsync();
            IdentityResult result = await this._userManager.SetPhoneNumberAsync(user, null);
            if (result.Succeeded)
            {
                return this.NoContent();
            }
            return this.BadRequest(new ApiError("Failed to remove phone number"));
        }

        [HttpGet("managelogins")]
        public async Task<IActionResult> ManageLogins(ManageMessageId? message = null)
        {
            this.ViewData["StatusMessage"] =
                message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.AddLoginSuccess ? "The external login was added."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            ApplicationUser user = await this.GetCurrentUserAsync();
            if (user == null)
            {
                return this.View("Error");
            }
            IList<UserLoginInfo> userLogins = await this._userManager.GetLoginsAsync(user);
            IEnumerable<AuthenticationScheme> schemes = await this._signInManager.GetExternalAuthenticationSchemesAsync();
            List<AuthenticationScheme> otherLogins = schemes.Where(auth => userLogins.All(ul => auth.Name != ul.LoginProvider)).ToList();
            this.ViewData["ShowRemoveButton"] = user.PasswordHash != null || userLogins.Count > 1;
            return this.View(new ManageLoginsViewModel
            {
                CurrentLogins = userLogins,
                OtherLogins = otherLogins
            });
        }

        [HttpPost("linklogin")]
        public IActionResult LinkLogin(string provider)
        {
            // Request a redirect to the external login provider to link a login for the current user
            string redirectUrl = this.Url.Action("LinkLoginCallback", "Manage");
            AuthenticationProperties properties = this._signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, this._userManager.GetUserId(this.User));
            return this.Challenge(properties, provider);
        }

        [HttpGet("linklogincallback")]
        public async Task<ActionResult> LinkLoginCallback()
        {
            ApplicationUser user = await this.GetCurrentUserAsync();
            ExternalLoginInfo info = await this._signInManager.GetExternalLoginInfoAsync(await this._userManager.GetUserIdAsync(user));
            if (info == null)
            {
                return this.BadRequest(new ApiError("Unable to find linked login info"));
            }
            IdentityResult result = await this._userManager.AddLoginAsync(user, info);
            if (result.Succeeded)
            {
                return this.NoContent();
            }
            return this.BadRequest(new ApiError("Unable to link login"));
        }

        [HttpGet("userinfo")]
        public async Task<IActionResult> UserInfo()
        {
            ApplicationUser user = await this.GetCurrentUserAsync();

            return this.Ok(new
            {
                FirstName = user.FirstName,
                LastName = user.LastName
            });
        }

        [HttpPost("userinfo")]
        public async Task<IActionResult> UserInfo([FromBody] UserInfoViewModel model)
        {
            ApplicationUser user = await this.GetCurrentUserAsync();

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;

            IdentityResult result = await this._userManager.UpdateAsync(user);
            if (result == IdentityResult.Success)
            {
                return this.Ok(new
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName
                });
            }

            return this.BadRequest(new ApiError("Unable to update user info"));
        }

        [HttpPost("changepassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordViewModel model)
        {
            ApplicationUser user = await this.GetCurrentUserAsync();
            IdentityResult result = await this._userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                this._logger.LogInformation(3, "User changed their password successfully.");
                return this.NoContent();
            }
            return this.BadRequest(new ApiError("Unable to change password"));
        }

        [HttpPost("setpassword")]
        public async Task<IActionResult> SetPassword([FromBody] SetPasswordViewModel model)
        {
            ApplicationUser user = await this.GetCurrentUserAsync();
            IdentityResult result = await this._userManager.AddPasswordAsync(user, model.NewPassword);
            if (result.Succeeded)
            {
                return this.NoContent();
            }
            return this.BadRequest(new ApiError("Unable to set password"));
        }

        [HttpGet("photo")]
        public async Task<IActionResult> UserPhoto()
        {
            var profileImage = await this._photosRepository.FirstOrDefault(i => i.ApplicationUser.Id == this.User.GetUserId());

            if (profileImage == null)
            {
                return this.NotFound();
            }

            return this.Ok(Convert.ToBase64String(profileImage.Content));
        }

        [HttpPost("photo")]
        public async Task<IActionResult> UserPhoto(IFormFile file)
        {
            {
                if (string.IsNullOrEmpty(file?.ContentType) || file.Length == 0)
                {
                    return this.BadRequest(new ApiError("Image provided is invalid"));
                }

                long size = file.Length;

                if (size > Convert.ToInt64(Startup.Configuration["MaxImageUploadSize"]))
                {
                    return this.BadRequest(new ApiError("Image size greater than allowed size"));
                }

                using (var memoryStream = new MemoryStream())
                {
                    var existingImage = await this._photosRepository.FirstOrDefault(i => i.ApplicationUserId == this.User.GetUserId());

                    await file.CopyToAsync(memoryStream);

                    if (existingImage == null)
                    {
                        var userImage = new ApplicationUserPhoto
                        {
                            ContentType = file.ContentType,
                            Content = memoryStream.ToArray(),
                            ApplicationUserId = this.User.GetUserId()
                        };
                        this._photosRepository.Add(userImage);
                    }
                    else
                    {
                        existingImage.ContentType = file.ContentType;
                        existingImage.Content = memoryStream.ToArray();
                        this._photosRepository.Update(existingImage);
                    }
                    await this._photosRepository.SaveChangesAsync();
                }

                return this.NoContent();
            }
        }

        #region Helpers

        public enum ManageMessageId
        {
            AddPhoneSuccess,
            AddLoginSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error
        }

        private Task<ApplicationUser> GetCurrentUserAsync()
        {
            return this._userManager.GetUserAsync(this.HttpContext.User);
        }

        #endregion
    }
}