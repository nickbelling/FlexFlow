namespace FlexFlow.Api.Controllers.Auth.DTO
{
    /// <summary>
    /// A user login request.
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// The username attempting to login.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The password for the user attempting to login.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// True if the login should be persistent across browser sessions.
        /// </summary>
        public bool RememberMe { get; set; }

        /// <summary>
        /// The two-factor authentication token.
        /// </summary>
        public string TwoFactorToken { get; set; }

        /// <summary>
        /// Whether or not this device should be remembered for two-factor authentication.
        /// </summary>
        public bool TwoFactorRememberMachine { get; set; }
    }

    /// <summary>
    /// Returned upon successful login.
    /// </summary>
    public class LoginResponse
    {
        /// <summary>
        /// The ID of the user that just logged in.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// The display name of the user that just logged in.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// True if the email for this user requires validation.
        /// </summary>
        public bool RequiresEmailValidation { get; set; }
    }
}
