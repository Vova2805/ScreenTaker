using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(ScreenTaker.Startup))]
namespace ScreenTaker
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
