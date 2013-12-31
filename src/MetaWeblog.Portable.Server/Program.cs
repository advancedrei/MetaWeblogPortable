using System.Threading.Tasks;

namespace MetaWeblog.Portable.Server
{
    class Program
    {


        static void Main(string[] args)
        {
            // NOTE: If running within Visual Studio you'll need to run VS as an administrator
            SimpleServer.userblogs.Add(new UserBlogInfo("admin", "http://localhost:14228/", "Test Blog"));
            SimpleServer.posts.Add(new PostInfo { DateCreated = new System.DateTime(2012, 1, 15), Title = "Why Pizza is Great", Description = "pizza", PostId = "10", Link = "/post/WhyPizzaIsGreat" });
            SimpleServer.posts.Add(new PostInfo { DateCreated = new System.DateTime(2012, 12, 2), Title = "1000 Amazing Uses for Staples", Description = "staples", PostId = "20", Link = "/post/1000AmazingUsesForStaples" });
            SimpleServer.posts.Add(new PostInfo { DateCreated = new System.DateTime(2013, 3, 31), Title = "Useful Things You Can Do With a Giraffe", Description = "giraffe", PostId = "30", Link = "/post/UsefulThingsYouCanDoWithAGiraffe" });
            SimpleServer.posts.Add(new PostInfo { DateCreated = new System.DateTime(2013, 4, 10), Title = "Sandwiches I have loved", Description = "d4", PostId = "sandwiches", Link = "/post/SandwichesIHaveLoved" });
            SimpleServer.Start();

        }
    }
}
