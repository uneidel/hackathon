using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Hackathon.Startup))]
namespace Hackathon
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
