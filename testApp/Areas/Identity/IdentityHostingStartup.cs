using System;
using ARPG.Models.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: HostingStartup(typeof(ARPG.Areas.Identity.IdentityHostingStartup))]
namespace ARPG.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {
                services.AddDbContext<ARPGContext>(options =>
                options.UseSqlServer(
                    context.Configuration.GetConnectionString("ARPGContext")));
                services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                  .AddEntityFrameworkStores<ARPGContext>();
            });
        }
    }
}