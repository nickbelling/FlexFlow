using FlexFlow.Api.Controllers.Auth.DTO;
using FlexFlow.Api.Identity.JsonWebTokens;
using FlexFlow.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace FlexFlow.Api.Controllers.Auth
{
    /// <summary>
    /// Exposes endpoints for logging in/out, changing passwords, two-factor authentication, etc.
    /// </summary>
    [Route("api/auth")]
    public class AuthController : FlexFlowControllerBase
    {
        private ILogger<AuthController> _logger;
        private UserManager<User> _userManager;
        private SignInManager<User> _signInManager;
        private readonly IJwtTokenManager _tokenManager;
        private readonly ICurrentJwtTokenManager _currentTokenManager;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="userManager"></param>
        /// <param name="signInManager"></param>
        /// <param name="tokenManager"></param>
        /// <param name="currentTokenManager"></param>
        public AuthController(
            ILogger<AuthController> logger,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IJwtTokenManager tokenManager,
            ICurrentJwtTokenManager currentTokenManager)
        {
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenManager = tokenManager;
            _currentTokenManager = currentTokenManager;
        }

        /// <summary>
        /// Attempts to login the given user.
        /// </summary>
        /// <param name="loginRequest"></param>
        /// <returns></returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof( LoginResponse ), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status428PreconditionRequired)]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest loginRequest)
        {
            ActionResult<LoginResponse> result;

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

                if (signInResult.RequiresTwoFactor)
                {
                    if (!string.IsNullOrEmpty(loginRequest.TwoFactorToken))
                    {
                        signInResult = await _signInManager.TwoFactorAuthenticatorSignInAsync(loginRequest.TwoFactorToken, loginRequest.RememberMe, loginRequest.TwoFactorRememberMachine);

                        if (signInResult.Succeeded)
                        {
                            IList<string> roles = await _userManager.GetRolesAsync(user);

                            result = new LoginResponse
                            {
                                DisplayName = user.DisplayName,
                                UserId = user.Id,
                                Token = await _tokenManager.GenerateTokenAsync(user.UserName, user.Email, roles),
                                RequiresEmailValidation = false // Can't have 2fa turned on if email isn't validated
                            };
                        }
                        else
                        {
                            result = Unauthorized();
                        }
                    }
                    else
                    {
                        // If the result is "RequiresTwoFactor", we return a flag telling the client that they need to
                        // proceed to a TFA screen instead of just blindly attempting to forge ahead
                        result = new StatusCodeResult(StatusCodes.Status428PreconditionRequired);
                    }
                }
                else if (signInResult.Succeeded)
                {
                    IList<string> roles = await _userManager.GetRolesAsync(user);

                    result = new LoginResponse
                    {
                        DisplayName = user.DisplayName,
                        UserId = user.Id,
                        Token = await _tokenManager.GenerateTokenAsync(user.UserName, user.Email, roles),
                        // Also return a flag telling the user that their email is not validated if that's the case.
                        RequiresEmailValidation = !(emailConfirmed)
                    };
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

            // Blacklist their JWT token
            await _currentTokenManager.BlacklistAsync();
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
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
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
