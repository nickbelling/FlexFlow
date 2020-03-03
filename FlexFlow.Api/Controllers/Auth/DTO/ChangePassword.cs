namespace FlexFlow.Api.Controllers.Auth.DTO
{
    /// <summary>
    /// Allows a user to change their password.
    /// </summary>
    public class ChangePasswordRequest
    {
        /// <summary>
        /// The password this user is currently using.
        /// </summary>
        public string OldPassword { get; set; }

        /// <summary>
        /// The user's new password.
        /// </summary>
        public string NewPassword { get; set; }
    }
}
