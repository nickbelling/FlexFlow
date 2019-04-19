using System;
using FlexFlow.Data.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace FlexFlow.Api.Identity.TokenProviders
{
    public class PasswordResetTokenProvider : DataProtectorTokenProvider<User>
    {
        public PasswordResetTokenProvider(
            IDataProtectionProvider dataProtectionProvider, 
            IOptions<PasswordResetTokenProviderOptions> options) : 
            base(dataProtectionProvider, options)
        {
            if (options?.Value?.TokenLifespan != default)
            {
                Options.TokenLifespan = options.Value.TokenLifespan;
            }
            else
            {
                Options.TokenLifespan = TimeSpan.FromHours(1);
            }
        }
    }

    public class PasswordResetTokenProviderOptions : DataProtectionTokenProviderOptions { }
}
