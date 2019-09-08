using System.Collections.Generic;
using System.Threading.Tasks;
using FlexFlow.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace FlexFlow.Api.Controllers.Auth
{
    [Route("api/auth")]
    public class AuthController : FlexFlowController
    {
        private ILogger<AuthController> _logger;
        private UserManager<User> _userManager;
        private SignInManager<User> _signInManager;

        public AuthController(
            ILogger<AuthController> logger,
            UserManager<User> userManager,
            SignInManager<User> signInManager)
        {
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>
        /// Attempts to login the given user.
        /// </summary>
        /// <param name="loginRequest"></param>
        /// <returns></returns>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<DTO.LoginResponse>> Login([FromBody] DTO.LoginRequest loginRequest)
        {
            ActionResult<DTO.LoginResponse> result;

            // Get the user
            User user = await _userManager.FindByNameAsync(loginRequest.Username);

            if (user != null)
            {
                // Sign them in
                SignInResult signInResult = await _signInManager.PasswordSignInAsync(
                    user, loginRequest.Password, loginRequest.RememberMe, true);

                // Check if their email is confirmed
                bool emailConfirmed = await _userManager.IsEmailConfirmedAsync(user);

                _logger.LogInformation("User {username} login result: {result}. Email confirmed: {emailConfirmed}. " +
                    "Requires TFA: {requiresTwoFactor}.",
                    user.UserName, signInResult.Succeeded, emailConfirmed, signInResult.RequiresTwoFactor);

                if (signInResult.Succeeded || signInResult.RequiresTwoFactor)
                {
                    result = Ok(new DTO.LoginResponse
                    {
                        DisplayName = user.DisplayName,
                        UserId = user.Id,

                        // If the result is "RequiresTwoFactor", we return a flag telling the client that they need to
                        // proceed to a TFA screen instead of just blindly attempting to forge ahead (the cookie won't
                        // actually be authorised to do anything until they do).
                        RequiresTwoFactorAuthenticatorKey = signInResult.RequiresTwoFactor,

                        // Also return a flag telling the user that their email is not validated if that's the case.
                        RequiresEmailValidation = !(emailConfirmed)
                    });
                }
                else
                {
                    _logger.LogInformation("User {username} login failed (wrong password).", user.UserName);
                    result = Unauthorized();
                }
            }
            else
            {
                _logger.LogInformation("User attempted to login with the username '{username}', but no user with that " +
                    "username was found.", loginRequest.Username);
                result = Unauthorized();
            }
            
            return result;
        }

        /// <summary>
        /// After a successful login request, accepts the user's two-factor authentication key.
        /// </summary>
        /// <param name="tfaRequest"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("tfa")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> AuthenticateTwoFactor([FromBody] DTO.TwoFactorAuthenticationRequest tfaRequest)
        {
            IActionResult result;
            SignInResult signInResult = await _signInManager.TwoFactorAuthenticatorSignInAsync(tfaRequest.Token, false, tfaRequest.RememberThisMachine);

            if (signInResult.Succeeded)
            {
                result = Ok();
            }
            else
            {
                result = Unauthorized();
            }

            return result;
        }

        /// <summary>
        /// Signs out the user. Note that this only deletes the cookie locally for the user in their browser; their
        /// cookie is otherwise still valid, and can be used to access endpoints until it expires.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task Logout()
        {
            // Sign out the user.
            await _signInManager.SignOutAsync();
        }

        /// <summary>
        /// Enables a user to change their password.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("changepassword")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesErrorResponseType(typeof(IEnumerable<IdentityResult>))]
        public async Task<IActionResult> ChangePassword(DTO.ChangePasswordRequest request)
        {
            IActionResult result;

            User user = await _userManager.GetUserAsync(User);

            IdentityResult changePasswordResult = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);

            if (changePasswordResult.Succeeded)
            {
                result = Ok();
            }
            else
            {
                result = BadRequest(changePasswordResult.Errors);
            }

            return result;
        }
    }
}
