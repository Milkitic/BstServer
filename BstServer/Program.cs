using BstServer;
using BstServer.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Milkitic.ApplicationHost;
using Milkitic.FileExplorer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(7);
    options.Cookie.HttpOnly = true;
});

ConfigureOtherServices(builder);
builder.Services.AddControllersWithViews();

var app = builder.Build();

Configure(app);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();

void ConfigureOtherServices(WebApplicationBuilder builder)
{
    var services = builder.Services;
    var l4dRootPath = builder.Configuration["ServerRootDir"];
    if (string.IsNullOrWhiteSpace(l4dRootPath))
    {
        l4dRootPath = Environment.CurrentDirectory;
        //throw new ArgumentException("ServerRootDir in appsettings.json is empty!");
    }

    Console.WriteLine(l4dRootPath);
    var executablePath = Path.Combine(l4dRootPath, "srcds_run");
    Console.WriteLine(executablePath);

    services.AddSingleton(new L4D2AppHost(executablePath,
        "-game left4dead2 " +
        "-strictportbind " +
        "-ip 0.0.0.0 " +
        "-port 27016 " +
        "+clientport 27005 " +
        "+map c3m1_plankcountry " +
        "+servercfgfile l4d2server.cfg " +
        "-maxplayers 16 " +
        "-tickrate 75",
        new HostSettings
        {
            ShowWindow = false
        })
    );
    services.AddSingleton(typeof(WebSocketHandler));
    services.AddScoped(k => new Explorer(Path.Combine(l4dRootPath)));
    //services.AddSingleton<IEmailSender, NeteaseEmailSender>(k =>
    //    new NeteaseEmailSender("l4d2tool", "******"));
}
void Configure(WebApplication webApplication)
{
    var webSocketHandler = webApplication.Services.GetService<WebSocketHandler>()!;
    var webSocketOptions = new WebSocketOptions
    {
        KeepAliveInterval = TimeSpan.FromSeconds(120),
    };
    app.UseWebSockets(webSocketOptions);
    app.Use(async (context, next) =>
    {
        if (context.Request.Path == "/ws")
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                webSocketHandler.WebSocket = await context.WebSockets.AcceptWebSocketAsync();
                await webSocketHandler.Response();
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
}
