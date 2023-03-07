using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using ProjectHorizon.ApplicationCore.Constants;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ProjectHorizon.WebAPI
{
    public static class ServicesExtensions
    {
        public static IServiceCollection ConfigureAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            string? secretKey = configuration.GetSection("JwtAuthentication:SecretKey").Value;
            SymmetricSecurityKey? securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, configureOptions =>
                {
                    configureOptions.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = configuration.GetSection("JwtAuthentication:Issuer").Value,

                        ValidateAudience = true,
                        ValidAudience = configuration.GetSection("JwtAuthentication:Audience").Value,

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = securityKey,

                        ValidateLifetime = true,

                        ValidAlgorithms = new HashSet<string> { SecurityAlgorithms.HmacSha256 },
                        ValidTypes = new HashSet<string> { "JWT" }
                    };

                    configureOptions.SaveToken = true;
                    configureOptions.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;

                            // If the request is for our hub...
                            if (!string.IsNullOrEmpty(accessToken) &&
                                (path.StartsWithSegments("/signalr-messages")))
                            {
                                // Read the token out of the query string
                                context.Token = accessToken;
                            }

                            // If the request is for the hangfire dashboard
                            if ((path.StartsWithSegments("/hangfire")))
                            {
                                var cookies = context.Request.Cookies;

                                if (cookies.ContainsKey("HangfireCookie"))
                                {
                                    context.Token = cookies["HangfireCookie"];
                                }
                            }

                            return Task.CompletedTask;
                        }
                    };
                })
            .AddMicrosoftIdentityWebApi(configuration, "AzureAd", AuthConstants.AzureAuthenticationScheme);

            return services;
        }

        public static void UseSecurityHeaders(this IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (!env.IsDevelopment())
            {
                app.UseHsts();
            }

            app.UseXfo(xFrameOptions => xFrameOptions.Deny()); // prevents clickjacking

            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add(
                    "Content-Security-Policy",
                    "default-src 'self'; " +
                    "script-src 'self' 'unsafe-eval' 'unsafe-inline'; " +
                    "style-src 'self' https://*.typekit.net 'unsafe-inline'; " +
                    "font-src 'self' https://*.typekit.net; " +
                    "img-src 'self' data: blob:; " +
                    "connect-src https: wss: ws:");

                context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

                // disables all in the list
                context.Response.Headers.Add("Permissions-Policy",
                    "accelerometer=(), camera=(), geolocation=(), " +
                    "gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");

                await next();
            });

            app.UseXContentTypeOptions();
        }
    }
}