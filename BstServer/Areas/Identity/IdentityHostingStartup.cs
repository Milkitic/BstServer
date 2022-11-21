[assembly: HostingStartup(typeof(BstServer.Areas.Identity.IdentityHostingStartup))]
namespace BstServer.Areas.Identity;

public class IdentityHostingStartup : IHostingStartup
{
    public void Configure(IWebHostBuilder builder)
    {
        builder.ConfigureServices((context, services) =>
        {
            services.AddSingleton(new UserSettingsManager());
        });
    }
}