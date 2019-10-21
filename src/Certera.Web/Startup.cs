using Certera.Core.Mail;
using Certera.Data;
using Certera.Web.AcmeProviders;
using Certera.Web.Authentication;
using Certera.Web.Middleware;
using Certera.Web.Options;
using Certera.Web.Services;
using Certera.Web.Services.HostedServices;
using DnsClient;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace Certera.Web
{
    public class Startup
    {
        public IConfiguration Configuration { get; set; }
        private IHostEnvironment Environment { get; set; }

        public Startup(IConfiguration configuration, IHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.ConfigureWritable<HttpServer>(Configuration.GetSection("HTTPServer"));
            services.ConfigureWritable<MailSenderInfo>(Configuration.GetSection("SMTP"));
            services.ConfigureWritable<Setup>(Configuration.GetSection("Setup"));
            services.Configure<AllowedRemoteIPAddresses>(Configuration.GetSection("AllowedRemoteIPAddresses"));

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => false;
            });

            services.AddDbContext<DataContext>(options =>
                options
                    .UseSqlite(Configuration.GetConnectionString("DefaultConnection"),
                        x => x.MigrationsAssembly(typeof(DataContext).Assembly.FullName))
                    .EnableSensitiveDataLogging(Environment.IsDevelopment()));

            services.AddIdentity<ApplicationUser, Role>(options =>
                {
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequiredLength = 8;
                    options.Password.RequiredUniqueChars = 1;
                })
                .AddRoles<Role>()
                .AddEntityFrameworkStores<DataContext>()
                .AddDefaultTokenProviders();

            services.AddTransient<KeyGenerator>();
            services.AddTransient<DomainScanService>();
            services.AddTransient<CertesAcmeProvider>();
            services.AddTransient<MailSender>();
            services.AddTransient<CertificateAcquirer>();
            var lookupClient = new LookupClient();
            services.AddSingleton<ILookupClient>(lookupClient);
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddHttpClient();

            // Background services
            services.AddHostedService<QueuedHostedService>();
            services.AddHostedService<DomainScanIntervalService>();
            services.AddHostedService<CertificateAcquiryService>();
            services.AddHostedService<CertificateChangeNotificationService>();
            services.AddHostedService<CertificateExpirationNotificationService>();

            services.Configure<RouteOptions>(options =>
            {
                options.LowercaseQueryStrings = true;
                options.LowercaseUrls = true;
            });

#pragma warning disable ASP0000 // Do not call 'IServiceCollection.BuildServiceProvider' in 'ConfigureServices'
            var httpServerOptions = services.BuildServiceProvider().GetService<IOptions<HttpServer>>()?.Value;
#pragma warning restore ASP0000 // Do not call 'IServiceCollection.BuildServiceProvider' in 'ConfigureServices'

            // Only configure the allowed hosts after setup has completed.
            // If this is being setup on VPS/Cloud, a hostname won't be known until after setup.
            if (!string.IsNullOrWhiteSpace(httpServerOptions?.SiteHostname))
            {
                var allowedHosts = new List<string>
                {
                    "localhost",
                    httpServerOptions.SiteHostname
                };

                services.Configure<HostFilteringOptions>(options =>
                {
                    options.AllowedHosts = allowedHosts;
                });
            }

            services.Configure<AntiforgeryOptions>(options =>
            {
                options.Cookie = new CookieBuilder
                {
                    HttpOnly = true,
                    Name = ".antiforgery.cookie",
                    Path = "/",
                    SecurePolicy = CookieSecurePolicy.SameAsRequest,
                    IsEssential = true
                };
            });

            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                    ApiKeyAuthenticationHandler.AuthScheme,
                    "API Key",
                    null);

            services.AddControllersWithViews().AddRazorRuntimeCompilation();

            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
                .AddRazorPagesOptions(options =>
                {
                    options.Conventions.AuthorizeFolder("/Account/Manage");
                    options.Conventions.AuthorizePage("/Account/Logout");

                    options.Conventions.AuthorizePage("/Index");
                    options.Conventions.AuthorizeFolder("/Acme");
                    options.Conventions.AuthorizeFolder("/Certificates");
                    options.Conventions.AuthorizeFolder("/Keys");
                    options.Conventions.AuthorizeFolder("/Notifications");
                    options.Conventions.AuthorizeFolder("/Settings");
                    options.Conventions.AuthorizeFolder("/Tracking");
                });

            services.AddHsts(options =>
            {
                options.MaxAge = TimeSpan.FromDays(365);
            });

            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie = new CookieBuilder
                {
                    HttpOnly = true,
                    Name = ".auth.cookie",
                    Path = "/",
                    SecurePolicy = CookieSecurePolicy.SameAsRequest,
                    IsEssential = true
                };
                options.ExpireTimeSpan = TimeSpan.FromDays(1);
                options.SlidingExpiration = true;
                options.LoginPath = "/account/login";
                options.LogoutPath = "/account/logout";
                options.AccessDeniedPath = "/account/accessdenied";
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseStatusCodePagesWithRedirects("/errors/{0}");

            var httpServerOptions = app.ApplicationServices.GetService<IOptions<HttpServer>>();
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var dataContext = scope.ServiceProvider.GetService<DataContext>();

                if (!SetupMiddleware.SetupFinished(dataContext, httpServerOptions.Value))
                {
                    var setupOptions = scope.ServiceProvider.GetService<IWritableOptions<Setup>>();
                    setupOptions.Update(x => x.Finished = false);
                }
            }

            // The built-in UseHttpsRedirection does a few things we don't want to do. 
            // It will do 5001 for dev, which sometimes we don't want
            // and it will do it for all requests, including the .well-known. 
            //app.UseHttpsRedirection();

            // It's best to set up how and when we want to do http --> https redirection
            // Always redirect to https except when serving the ACME challenge and the API test endpoint to restart the server
            var options = new RewriteOptions()
                .Add(ctx =>
                {
                    var req = ctx.HttpContext.Request;

                    if (!req.IsHttps &&
                        (!req.Path.StartsWithSegments("/.well-known/acme-challenge") &&
                        (!req.Path.StartsWithSegments("/api/test"))))
                    {
                        var redirectUrl = UriHelper.BuildAbsolute(
                            "https",
                            req.Host,
                            req.PathBase,
                            req.Path,
                            req.QueryString);

                        ctx.HttpContext.Response.Redirect(redirectUrl);
                        ctx.Result = RuleResult.EndResponse;
                    }
                });

            app.UseRewriter(options);

            app.UseStatusCodePages();

            app.UseStaticFiles();
            app.UseMiddleware<AllowedRemoteIPMiddleware>();

            // This middleware is above auth to ensure we always redirect to setup
            // on first launch and no other paths work until setup has been completed.
            app.UseMiddleware<SetupMiddleware>();

            app.UseRouting();

            app.UseCookiePolicy(new CookiePolicyOptions
            {
                Secure = CookieSecurePolicy.SameAsRequest,
                HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
                endpoints.MapDefaultControllerRoute();
            });

            // Handle setup steps where we can control writing to the response stream and
            // giving instant feedback
            app.MapWhen(ctx => ctx.Request.Path.Equals("/setup/get-acme-cert"),
                builder =>
                {
                    builder.UseMiddleware<SetupAcmeCertMiddleware>();
                });
        }
    }
}
