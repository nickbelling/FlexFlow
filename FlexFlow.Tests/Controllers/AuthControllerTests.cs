using FlexFlow.Api.Controllers.Auth;
using FlexFlow.Api.Controllers.Auth.DTO;
using FlexFlow.Api.Identity.JsonWebTokens;
using FlexFlow.Data.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace FlexFlow.Tests.Controllers
{
    [TestClass]
    public class AuthControllerTests
    {
        [TestMethod]
        public async Task CanLoginWithValidPassword()
        {
            User adminUser = new User
            {
                Id = 1,
                UserName = "admin",
                DisplayName = "Administrator"
            };

            Mock<UserManager<User>> userManager = GetUserManager();

            // Asking for the admin user should return the admin user, and the email should be confirmed
            userManager.Setup(x => x.FindByNameAsync("admin")).ReturnsAsync(adminUser).Verifiable();
            userManager.Setup(x => x.IsEmailConfirmedAsync(adminUser)).ReturnsAsync(true).Verifiable();

            Mock<SignInManager<User>> signInManager = GetMockSignInManager(userManager.Object);
            
            // Signing in with a valid password should work, and an invalid password should fail
            signInManager.Setup(x => x.PasswordSignInAsync(adminUser, "admin", It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(SignInResult.Success).Verifiable();
            signInManager.Setup(x => x.PasswordSignInAsync(adminUser, It.IsNotIn("admin"), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(SignInResult.Failed).Verifiable();

            // The token manager should have generated a token
            Mock<IJwtTokenManager> tokenManager = new Mock<IJwtTokenManager>();
            tokenManager.Setup(x => x.GenerateTokenAsync("admin", It.IsAny<string>(), It.IsAny<IList<string>>())).Verifiable();

            AuthController controller = new AuthController(
                Mock.Of<ILogger<AuthController>>(),
                userManager.Object,
                signInManager.Object,
                tokenManager.Object,
                Mock.Of<ICurrentJwtTokenManager>());

            ActionResult<LoginResponse> response = await controller.Login(new LoginRequest
            {
                Username = "admin",
                Password = "admin"
            });

            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Value);
            Assert.AreEqual(1, response.Value.UserId);
            Assert.AreEqual("Administrator", response.Value.DisplayName);

            tokenManager.Verify(x => x.GenerateTokenAsync("admin", It.IsAny<string>(), It.IsAny<IList<string>>()), Times.Once);
        }

        private Mock<UserManager<User>> GetUserManager(IUserStore<User> store = null)
        {
            store ??= new Mock<IUserStore<User>>().Object;
            
            Mock<IOptions<IdentityOptions>> options = new Mock<IOptions<IdentityOptions>>();
            IdentityOptions idOptions = new IdentityOptions();
            idOptions.Lockout.AllowedForNewUsers = false;
            options.Setup(o => o.Value).Returns(idOptions);

            List<IUserValidator<User>> userValidators = new List<IUserValidator<User>>();
            Mock<IUserValidator<User>> validator = new Mock<IUserValidator<User>>();
            userValidators.Add(validator.Object);
            List<PasswordValidator<User>> pwdValidators = new List<PasswordValidator<User>>
            {
                new PasswordValidator<User>()
            };

            Mock<UserManager<User>> userManager = new Mock<UserManager<User>>(
                store, 
                options.Object, new PasswordHasher<User>(),
                userValidators, pwdValidators, new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(), null,
                new Mock<ILogger<UserManager<User>>>().Object);

            validator.Setup(v => v.ValidateAsync(userManager.Object, It.IsAny<User>()))
                .Returns(Task.FromResult(IdentityResult.Success)).Verifiable();
            return userManager;
        }

        private Mock<SignInManager<User>> GetMockSignInManager(UserManager<User> userManager)
        {
            Mock<IHttpContextAccessor> contextAccessor = new Mock<IHttpContextAccessor>();
            Mock<IUserClaimsPrincipalFactory<User>> userPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<User>>();

            return new Mock<SignInManager<User>>(
                userManager,
                Mock.Of<IHttpContextAccessor>(),
                Mock.Of<IUserClaimsPrincipalFactory<User>>(),
                Mock.Of<IOptions<IdentityOptions>>(),
                Mock.Of<ILogger<SignInManager<User>>>(),
                Mock.Of<IAuthenticationSchemeProvider>(),
                Mock.Of<IUserConfirmation<User>>());
        }
    }
}
