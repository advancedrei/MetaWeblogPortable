using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MetaWeblog.Portable.XmlRpc;
using SXL=System.Xml.Linq;

namespace MetaWeblog.Portable.Server
{
    public class UserBlogInfo
    {
        public string ID;
        public string Url;
        public string Name;

        public UserBlogInfo( string id, string url, string name)
        {
            this.ID = id;
            this.Url = url;
            this.Name = name;
        }

        public MetaWeblog.Portable.XmlRpc.Struct GetStruct()
        {
            var struct_ = new MetaWeblog.Portable.XmlRpc.Struct();
            struct_["blogid"] = new MetaWeblog.Portable.XmlRpc.StringValue(this.ID);
            struct_["url"] = new MetaWeblog.Portable.XmlRpc.StringValue(this.Url);
            struct_["blogName"] = new MetaWeblog.Portable.XmlRpc.StringValue(this.Name);

            return struct_;
        }
    }

    class Program
    {


        static void Main(string[] args)
        {
            // NOTE: If running within Visual Studio you'll need to run VS as an administrator
            SimpleServer.userblogs.Add(new UserBlogInfo("admin", "http://localhost/blog/", "Test Blog"));
            SimpleServer.posts.Add(new PostInfo { DateCreated = new System.DateTime(2013, 1, 1), Title = "t1", Description = "d1", PostId = "10", Link = "/10" });
            SimpleServer.posts.Add(new PostInfo { DateCreated = new System.DateTime(2013, 2, 2), Title = "t2", Description = "d2", PostId = "20", Link = "/20" });
            SimpleServer.posts.Add(new PostInfo { DateCreated = new System.DateTime(2013, 3, 3), Title = "t3", Description = "d3", PostId = "30", Link = "/30" });
            SimpleServer.posts.Add(new PostInfo { DateCreated = new System.DateTime(2013, 4, 4), Title = "t4", Description = "d4", PostId = "40", Link = "/40" });
            SimpleServer.Start();

        }
    }

    public static class SimpleServer
    {
        public readonly static int xport = 14228;
        private static readonly System.Net.HttpListener Listener = new System.Net.HttpListener();
        public static List<UserBlogInfo> userblogs = new List<UserBlogInfo>();
        public static List<PostInfo> posts = new List<PostInfo>(); 

        public static void Start()
        {
            string hostname = "127.0.0.1";
            string url = string.Format("http://{0}:{1}/", hostname, xport);

         
            Listener.Prefixes.Add(url);
            Listener.Start();
            Listen();
            Console.WriteLine("Listening...");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static async void Listen()
        {
            while (true)
            {
                var context = await Listener.GetContextAsync();
                Console.WriteLine("Client connected");
                //Console.WriteLine(context.Request.AcceptTypes.ToString());
                Console.WriteLine(context.Request.Url.ToString());
                //Console.WriteLine(context.Request.UserAgent.ToString());
                ProcessRequest(context);
            }

            Listener.Close();
        }

        private static void ProcessRequest(System.Net.HttpListenerContext context)
        {
            // Get the data from the HTTP stream
            string body = new System.IO.StreamReader(context.Request.InputStream).ReadToEnd();

            var methodcall = MetaWeblog.Portable.XmlRpc.MethodCall.Parse(body);

            Console.WriteLine("Method Name: {0}", methodcall.Name);
            if (methodcall.Name == "blogger.getUsersBlogs")
            {
                var method_response = BuildStructArrrayResponse(SimpleServer.userblogs.Select(i=>i.GetStruct()));
                var method_response_xml = method_response.CreateDocument();
                var method_response_string = method_response_xml.ToString();

                write_string(context, method_response_string);
            }
            else if (methodcall.Name == "metaWeblog.getRecentPosts")
            {
                var method_response = BuildStructArrrayResponse(SimpleServer.posts.Select(i => i.GetStruct()));
                var method_response_xml = method_response.CreateDocument();
                var method_response_string = method_response_xml.ToString();

                write_string(context, method_response_string);
            }
            else if (methodcall.Name == "metaWeblog.newPost")
            {

                var blogid = (XmlRpc.StringValue)methodcall.Parameters[0];
                var username = (XmlRpc.StringValue)methodcall.Parameters[1];
                var password = (XmlRpc.StringValue)methodcall.Parameters[2];
                var struct_ =  (XmlRpc.Struct)methodcall.Parameters[3];
                var post_status = (XmlRpc.BooleanValue)methodcall.Parameters[4];

                var post_title = struct_.Get<XmlRpc.StringValue>("title");
                var post_description = struct_.Get<XmlRpc.StringValue>("description");
                var post_categories = struct_.Get<XmlRpc.Array>("categories",null);


                var np = new PostInfo();
                np.Title = post_title.String;
                np.Description = post_description.String;
                //np.Categories??
                np.DateCreated = System.DateTime.Now;
                np.Link = "/foo";
                np.Permalink = "/foo";
                np.PostId = System.DateTime.Now.Ticks.ToString();
                np.Permalink = np.Link;

                SimpleServer.posts.Add(np);

                var mr1 = new XmlRpc.MethodResponse();
                var arr1 = new XmlRpc.StringValue( np.PostId );

                mr1.Parameters.Add(arr1);

                write_string(context, mr1.CreateDocument().ToString() );

            }
            else if (methodcall.Name == "metaWeblog.getPost")
            {
                var postid = (XmlRpc.StringValue)methodcall.Parameters[0];
                var username = (XmlRpc.StringValue)methodcall.Parameters[1];
                var password = (XmlRpc.StringValue)methodcall.Parameters[2];

                foreach (var p in SimpleServer.posts)
                {
                    if (p.PostId == postid.String)
                    {
                        var mr1 = new XmlRpc.MethodResponse();
                        var struct_ = p.GetStruct();
                        mr1.Parameters.Add(struct_);
                        write_string(context, mr1.CreateDocument().ToString());
                    }
                }

            }
            else
            {
                write_string(context, "ACK");
            }


            
            Console.WriteLine("Response");
        }

        public static MethodResponse BuildStructArrrayResponse(IEnumerable<XmlRpc.Struct> structs)
        {
            var mr1 = new XmlRpc.MethodResponse();
            var arr1 = new XmlRpc.Array();

            foreach (var i in structs)
            {
                arr1.Add(i);
            }

            mr1.Parameters.Add(arr1);

            return mr1;
        }

        private static void write_string(HttpListenerContext context, string text)
        {
            Console.WriteLine(text);
            byte[] b = Encoding.UTF8.GetBytes(text);
            context.Response.StatusCode = 200;
            context.Response.KeepAlive = false;
            context.Response.ContentLength64 = b.Length;

            var output = context.Response.OutputStream;
            output.Write(b, 0, b.Length);
            context.Response.Close();
        }
    }

}
