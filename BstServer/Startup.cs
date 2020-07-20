using BstServer.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Milkitic.ApplicationHost;
using Milkitic.FileExplorer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace BstServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            //services.Configure<IdentityOptions>(options =>
            //{
            //    // Password settings
            //    options.Password.RequireDigit = true;
            //    options.Password.RequiredLength = 8;
            //    options.Password.RequireNonAlphanumeric = false;
            //    options.Password.RequireUppercase = false;
            //    options.Password.RequireLowercase = false;
            //    options.Password.RequiredUniqueChars = 6;

            //    // Lockout settings
            //    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
            //    options.Lockout.MaxFailedAccessAttempts = 10;
            //    options.Lockout.AllowedForNewUsers = true;

            //    // User settings
            //    options.User.RequireUniqueEmail = true;
            //});
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(
                    Configuration.GetConnectionString("DefaultConnection")));
            services.AddDefaultIdentity<IdentityUser>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromDays(7);
                options.Cookie.HttpOnly = true;
            });

            var dirRoot = Directory.GetDirectoryRoot(AppDomain.CurrentDomain.BaseDirectory);
            Console.WriteLine(dirRoot);
            var dir = @"/home/l4d2server/coop/serverfiles/srcds_run";
            dir = Path.Combine(dirRoot, "home", "l4d2server", "coop", "serverfiles");
            var path = Path.Combine(dir, "srcds_run");
            Console.WriteLine(path);
            Console.WriteLine(File.Exists(path));
            services.AddSingleton(new L4D2AppHost(path,
                "-game left4dead2 " +
                "-strictportbind " +
                "-ip 172.17.16.2 " +
                "-port 27015 " +
                "+clientport 27005 " +
                "+map c3m1_plankcountry " +
                "+servercfgfile l4d2server.cfg " +
                "-maxplayers 8 " +
                "-tickrate 45",
                new HostSettings
            {
                ShowWindow = false
            }));
            services.AddSingleton(typeof(WebSocketHandler));
            //services.AddSingleton(TerminalAppHost.GetInstance());

            services.AddScoped(k =>
                new Explorer(dir));
            
            services.AddSingleton<IEmailSender, NeteaseEmailSender>(k =>
                new NeteaseEmailSender("l4d2tool", "******"));

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.Configure<FormOptions>(x =>
            {
                x.ValueLengthLimit = int.MaxValue;
                x.MultipartBodyLengthLimit = long.MaxValue; // In case of multipart
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, WebSocketHandler wsh)
        {
            var webSocketOptions = new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120),
                ReceiveBufferSize = 4 * 1024,
            };
            app.UseWebSockets(webSocketOptions);

            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/ws")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        wsh.WebSocket = await context.WebSockets.AcceptWebSocketAsync();
                        await wsh.Response();
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }

            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            //app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseAuthentication();
            app.UseSession();

            //app.UseMvc(routes =>
            //{
            //    routes.MapRoute(
            //        name: "default",
            //        template: "{controller=Home}/{action=Index}/{id?}");
            //});
            app.UseMvc(routes =>
             {
                 routes.MapRoute(
                     name: "default",
                     template: "{controller}/{action}/{id?}",
                     defaults: new { controller = "Home", action = "Index" });
             });
        }
    }

    public class NeteaseEmailSender : SmtpClient, IEmailSender
    {
        private string _userName;

        public NeteaseEmailSender(string userName, string password)
        {
            _userName = userName;
            this.UseDefaultCredentials = true;
            this.DeliveryFormat = SmtpDeliveryFormat.International;
            this.DeliveryMethod = SmtpDeliveryMethod.Network;
            Port = 25;
            Credentials = new NetworkCredential(userName, password);
            Host = "smtp.126.com";
            //Send($"{_userName}@126.com", "milkitic@126.com", "测试", "测试邮件");
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var msg = new MailMessage(new MailAddress($"{_userName}@126.com"), new MailAddress(email))
            {
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            await SendMailAsync(msg);
        }
    }
}
