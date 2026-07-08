using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Jwt;
using Owin;
using System.Configuration;
using System.Text;

public static class JwtConfig
{
    public static void ConfigureJwt(IAppBuilder app)
    {
        var key = Encoding.UTF8.GetBytes(ConfigurationManager.AppSettings["JwtKey"]);
                    
        app.UseJwtBearerAuthentication(
            new JwtBearerAuthenticationOptions
            {
                AuthenticationMode =
                    AuthenticationMode.Active,

                TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = "TechStore",

                        ValidateAudience = true,
                        ValidAudience = "TechStore",

                        ValidateIssuerSigningKey = true,

                        IssuerSigningKey =
                            new SymmetricSecurityKey(key),

                        ValidateLifetime = true
                    }
            });
    }
}