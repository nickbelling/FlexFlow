namespace FlexFlow.Api.Controllers.Auth.DTO
{
    /// <summary>
    /// A two-factor authentication request - occurs after successful validation of username and password, when the
    /// <see cref="LoginResponse.RequiresTwoFactorAuthenticatorKey"/> property of the login response is true.
    /// </summary>
    public class TwoFactorAuthenticationRequest
    {
        /// <summary>
        /// The authenticator token to login with.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// True if FlexFlow should remember this machine when attempting to login.
        /// </summary>
        public bool RememberThisMachine { get; set; }
    }
}
