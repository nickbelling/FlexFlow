using System;
using FlexFlow.Data.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace FlexFlow.Api.Identity.TokenProviders
{
    public class EmailConfirmationTokenProvider : DataProtectorTokenProvider<User>
    {
        public EmailConfirmationTokenProvider(
            IDataProtectionProvider dataProtectionProvider, 
            IOptions<EmailConfirmationTokenProviderOptions> options) : 
            base(dataProtectionProvider, options)
        {
            if (options?.Value?.TokenLifespan != default)
            {
                Options.TokenLifespan = options.Value.TokenLifespan;
            }
            else
            {
                Options.TokenLifespan = TimeSpan.FromDays(1);
            }
        }
    }

    public class EmailConfirmationTokenProviderOptions : DataProtectionTokenProviderOptions { }
}
