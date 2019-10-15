using Certera.Data;
using Certera.Web.Options;
using Certes;
using Certes.Acme;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Certera.Web
{
    public class Program
    {
        private static CancellationTokenSource _cancelTokenSource;
        private static bool _restartRequested;
        private static bool _shutdownRequested;
        public static string ConfigFileName { get; private set; }

        public static void Main(string[] args)
        {
            while (true)
            {
                _cancelTokenSource = new CancellationTokenSource();
                Thread appThread = new Thread(new ThreadStart(() =>
                {
                    var host = CreateHostBuilder(args).Build().InitializeDatabase();
                    try
                    {
                        var task = host.RunAsync(_cancelTokenSource.Token);
                        task.GetAwaiter().GetResult();

                        // User does CTRL+C
                        _shutdownRequested = task.IsCompletedSuccessfully;
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }));

                if (_shutdownRequested)
                {
                    if (_restartRequested)
                    {
                        // Clear flag
                        _restartRequested = false;
                        Thread.Sleep(3000);
                    }
                    else
                    {
                        break;
                    }
                }

                appThread.Start();

                // Block and wait until thread is terminated due to restart
                appThread.Join();
            }
        }

        public static void Restart()
        {
            _restartRequested = true;
            _cancelTokenSource.Cancel();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHost(builder =>
                {
                    builder.ConfigureAppConfiguration((hostingContext, appBuilder) =>
                    {
                        var env = hostingContext.HostingEnvironment;

                        ConfigFileName = env.IsProduction()
                            ? "config.json"
                            : $"config.{env.EnvironmentName}.json";

                        appBuilder.AddJsonFile("config.json", optional: true, reloadOnChange: true)
                                  .AddJsonFile($"config.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    })
                    .UseKestrel()
                    .ConfigureKestrel((context, options) =>
                    {
                        options.ConfigureEndpoints();
                    })
                    .UseStartup<Startup>();
                });
    }

    public static class WebHostExtensions
    {
        public static IHost InitializeDatabase(this IHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetService<DataContext>();

                var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

                if (env.IsProduction() ||
                    !(context.Database.GetService<IDatabaseCreator>() as RelationalDatabaseCreator).Exists())
                {
                    context.Database.Migrate();
                }

                Task.Run(async () =>
                {
                    var roleMgr = scope.ServiceProvider.GetService<RoleManager<Role>>();
                    if (!await roleMgr.RoleExistsAsync("Admin"))
                    {
                        await roleMgr.CreateAsync(new Role("Admin"));
                    }
                    if (!await roleMgr.RoleExistsAsync("User"))
                    {
                        await roleMgr.CreateAsync(new Role("User"));
                    }
                }).GetAwaiter().GetResult();
            }

            return host;
        }
    }

    public static class KestrelServerOptionsExtensions
    {
        private static X509Certificate2 _lastCert;
        private static long _lastCertId;

        public static void ConfigureEndpoints(this KestrelServerOptions options)
        {
            var configuration = options.ApplicationServices.GetRequiredService<IConfiguration>();

            var httpServer = new HttpServer();
            configuration.GetSection("HTTPServer").Bind(httpServer);

            // Configure HTTP on port 80 on any IP address
            options.ListenAnyIP(80);

            // Configure HTTPS on port user has chosen
            if (httpServer.HttpsPort != 0)
            {
                options.ListenAnyIP(httpServer.HttpsPort,
                    listenOptions =>
                    {
                        listenOptions.UseHttps(httpsOptions =>
                        {
                            httpsOptions.ServerCertificateSelector = (ctx, name) =>
                            {
                                // If we're here, it means we've already completed setup
                                // and there should be a cert.

                                // Try to get the cert and fallback to default localhost cert.
                                // TODO: check for closure issues on "options" below
                                return GetHttpsCertificate(options, name);
                            };
                        });
                    });
            }
        }

        private static X509Certificate2 GetHttpsCertificate(KestrelServerOptions options, string name)
        {
            if (string.Equals(name, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                return new X509Certificate2("localhost.pfx", "password");
            }

            using (var scope = options.ApplicationServices.CreateScope())
            {
                var logger = scope.ServiceProvider.GetService<ILogger<Program>>();

                var httpServerOptions = scope.ServiceProvider.GetService<IOptionsSnapshot<HttpServer>>();
                var host = httpServerOptions.Value.SiteHostname;

                if (!string.Equals(name, host))
                {
                    logger.LogWarning($"Cert requested for {name}, which differs from {host}. Will only attempt to locate certificate for {host}");
                    return null;
                }

                var dataContext = scope.ServiceProvider.GetService<DataContext>();
                var acmeCert = dataContext.GetAcmeCertificate(host);

                // Build the PFX to be used
                var order = acmeCert?.LatestValidAcmeOrder;
                if (order != null)
                {
                    if (_lastCertId == order.AcmeOrderId)
                    {
                        return _lastCert;
                    }
                    if (order.RawDataPem != null)
                    {
                        var certChain = new CertificateChain(order.RawDataPem);
                        var key = KeyFactory.FromPem(acmeCert.Key.RawData);
                        var pfxBuilder = certChain.ToPfx(key);
                        var pfx = pfxBuilder.Build(host, string.Empty);

                        _lastCertId = order.AcmeOrderId;
                        _lastCert = new X509Certificate2(pfx, string.Empty);
                        return _lastCert;
                    }
                }
                logger.LogWarning($"No certificate found for {host}. Falling back to default localhost certificate.");
            }

            return new X509Certificate2("localhost.pfx", "password");
        }
    }
}
