using System.Threading.Tasks;

namespace MetaWeblog.Portable.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            // NOTE: If running within Visual Studio you'll need to run VS as an administrator

            var options = new BlogServerOptions();
            options.CreateSampleContent = true;
            var blog_server = new BlogServer(options);
            blog_server.Start();
        }
    }
}
