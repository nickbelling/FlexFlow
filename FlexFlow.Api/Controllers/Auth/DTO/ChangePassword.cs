namespace FlexFlow.Api.Controllers.Auth.DTO
{
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
