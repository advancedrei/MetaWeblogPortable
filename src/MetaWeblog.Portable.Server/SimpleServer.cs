using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using MetaWeblog.Portable.XmlRpc;

namespace MetaWeblog.Portable.Server
{
    public static class SimpleServer
    {
        public readonly static int xport = 14228;
        private static readonly System.Net.HttpListener Listener = new System.Net.HttpListener();
        public static List<UserBlogInfo> userblogs = new List<UserBlogInfo>();
        public static List<PostInfo> posts = new List<PostInfo>();
        public static string logfilename;
        private static StreamWriter logf;
        public static void Start()
        {

            string mydocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string logfilename = System.IO.Path.Combine(mydocs, "SimpleServer.txt");
            logf = System.IO.File.AppendText(logfilename);
            logf.AutoFlush = true;

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
            log("Listen() started");
            while (true)
            {
                var context = await Listener.GetContextAsync();
                Console.WriteLine("Client connected");
                //Console.WriteLine(context.Request.AcceptTypes.ToString());
                
                log("Client Connected");
                log("    Request Url: {0}",context.Request.Url);
                log("    Request Url Absolute Path: {0}", context.Request.Url.AbsolutePath);
                ProcessRequest(context);
            }

            Listener.Close();
        }

        private static void ProcessRequest(System.Net.HttpListenerContext context)
        {
            log("ProcessRequest() started");


            if (context.Request.Url.AbsolutePath == "/metaweblogapi")
            {
                log("    Request sent to metaweblog api - treating as XmlRpcCall");
                string body = new System.IO.StreamReader(context.Request.InputStream).ReadToEnd();
                log("    Read {0} characters from input stream", body.Length);
                log("    Parsing body ");                
                var methodcall = MetaWeblog.Portable.XmlRpc.MethodCall.Parse(body);


                Console.WriteLine("Method Name: {0}", methodcall.Name);
                if (methodcall.Name == "blogger.getUsersBlogs")
                {
                    handle_blogger_getUsersBlog(context);
                }
                else if (methodcall.Name == "metaWeblog.getRecentPosts")
                {
                    handle_metaWeblog_getRecentPosts(context);
                }
                else if (methodcall.Name == "metaWeblog.newPost")
                {
                    handle_metaWeblog_newPost(context, methodcall);
                }
                else if (methodcall.Name == "metaWeblog.getPost")
                {
                    handle_metaWeblog_getPost(context, methodcall);
                }
                else
                {
                    handle_unknown_xmlrpc_method(context, methodcall, body);
                }                
            }
            else
            {
                handle_normal_request(context);
            }
        }

        private static void handle_normal_request(HttpListenerContext context)
        {
            log("treat as Non-XmlRpc request");

            if (context.Request.Url.AbsolutePath == "/")
            {
                log("Root page - print out blog contents");

                var xdoc = XDocument();
                var el_body = xdoc.Element("html").Element("body");
                el_body.Add(new System.Xml.Linq.XElement("h1", "Blog Home"));

                foreach (var post in SimpleServer.posts)
                {
                    var el_para = new System.Xml.Linq.XElement("p");
                    el_body.Add(el_para);

                    var el_text =
                        new System.Xml.Linq.XText(post.DateCreated == null ? "No Publish Date" : post.DateCreated.Value.ToShortDateString());
                    el_para.Add(el_text);

                    var el_a = new System.Xml.Linq.XElement("a");
                    el_a.SetAttributeValue("href", post.Link);
                    el_a.Value = post.Title;
                    el_para.Add(el_a);
                }
                write_string(context, xdoc.ToString(), 200);
            }
            else if (context.Request.Url.AbsolutePath.StartsWith("/post/"))
            {
                log("looking for a specific post");
                var tokens = context.Request.Url.AbsolutePath.Split(new char[] { '/' });


                string post_link = context.Request.Url.AbsolutePath;

                log("postlink = {0}" , post_link);


                PostInfo thepost = null;
                foreach (var post in SimpleServer.posts)
                {
                    if (post.Link == post_link)
                    {
                        thepost = post;
                    }
                }

                if (thepost == null)
                {
                    log("Root page - send 404");
                    var xdoc = XDocument();
                    var el_body = xdoc.Element("html").Element("body");
                    el_body.Value = string.Format("Not found {0}", context.Request.Url.AbsolutePath);
                    write_string(context, xdoc.ToString(), 404);
                }
                else
                {
                    var xdoc = XDocument();
                    var el_body = xdoc.Element("html").Element("body");
                    el_body.Add(new System.Xml.Linq.XElement("h1", thepost.Title));

                    var el_div = new System.Xml.Linq.XElement("div");
                    el_body.Add(el_div);

                    el_div.Add( thepost.Description );

                    write_string(context, xdoc.ToString(), 200);
                    
                }
                
            }
            else
            {
                log("Root page - send 404");
                var xdoc = XDocument();
                var el_body = xdoc.Element("html").Element("body");
                el_body.Value = string.Format("Not found {0}", context.Request.Url.AbsolutePath);
                write_string(context, xdoc.ToString(), 404);
            }
        }

        private static void handle_unknown_xmlrpc_method(HttpListenerContext context, MethodCall methodcall, string body)
        {
            log("    Unhandled XmlRpcMethod {0}", methodcall.Name);
            log("{0}", body);

            var f = new XmlRpc.Fault();
            f.FaultCode = 0;
            f.FaultString = string.Format("unsupported method {0}", methodcall.Name);

            write_string(context, f.CreateDocument().ToString(), 200);
        }

        private static void handle_metaWeblog_getPost(HttpListenerContext context, MethodCall methodcall)
        {
            var postid = (XmlRpc.StringValue) methodcall.Parameters[0];
            var username = (XmlRpc.StringValue) methodcall.Parameters[1];
            var password = (XmlRpc.StringValue) methodcall.Parameters[2];

            foreach (var p in SimpleServer.posts)
            {
                if (p.PostId == postid.String)
                {
                    var mr1 = new XmlRpc.MethodResponse();
                    var struct_ = p.ToStruct();
                    mr1.Parameters.Add(struct_);
                    write_string(context, mr1.CreateDocument().ToString(), 200);
                }
            }
        }

        private static void handle_metaWeblog_newPost(HttpListenerContext context, MethodCall methodcall)
        {
            var blogid = (XmlRpc.StringValue) methodcall.Parameters[0];
            var username = (XmlRpc.StringValue) methodcall.Parameters[1];
            var password = (XmlRpc.StringValue) methodcall.Parameters[2];
            var struct_ = (XmlRpc.Struct) methodcall.Parameters[3];
            var post_status = (XmlRpc.BooleanValue) methodcall.Parameters[4];

            var post_title = struct_.Get<XmlRpc.StringValue>("title");
            var post_description = struct_.Get<XmlRpc.StringValue>("description");
            var post_categories = struct_.Get<XmlRpc.Array>("categories", null);


            var link = get_post_if_from_title(post_title);

            var np = new PostInfo();
            np.Title = post_title.String;
            np.Description = post_description.String;
            //np.Categories??
            np.DateCreated = System.DateTime.Now;
            np.Link = link;
            np.Permalink = link;
            np.PostId = System.DateTime.Now.Ticks.ToString();
            np.Permalink = np.Link;

            SimpleServer.posts.Add(np);

            var mr1 = new XmlRpc.MethodResponse();
            var arr1 = new XmlRpc.StringValue(np.PostId);

            mr1.Parameters.Add(arr1);

            write_string(context, mr1.CreateDocument().ToString(), 200);
        }

        private static string get_post_if_from_title(StringValue post_title)
        {
            string safe_id = post_title.String.Trim();
            safe_id = safe_id.Replace(" ", "-");
            safe_id = safe_id.Replace("\t", "-");
            safe_id = safe_id.Replace("\r", "-");
            safe_id = safe_id.Replace("\n", "-");
            safe_id = safe_id.Replace("&", "-and-");
            safe_id = safe_id.Replace("<", "-lt-");
            safe_id = safe_id.Replace(">", "-gt-");
            safe_id = safe_id.Replace("?", "");
            safe_id = safe_id.Replace(".", "");
            safe_id = safe_id.Replace("!", "");
            safe_id = safe_id.Replace("$", "");
            safe_id = safe_id.Replace("@", "");
            safe_id = safe_id.Replace("@", "");
            string link = "/post/" + safe_id;
            return link;
        }

        private static void handle_metaWeblog_getRecentPosts(HttpListenerContext context)
        {
            var method_response = BuildStructArrrayResponse(SimpleServer.posts.Select(i => i.ToStruct()));
            var method_response_xml = method_response.CreateDocument();
            var method_response_string = method_response_xml.ToString();

            write_string(context, method_response_string, 200);
        }

        private static void handle_blogger_getUsersBlog(HttpListenerContext context)
        {
            var method_response = BuildStructArrrayResponse(SimpleServer.userblogs.Select(i => i.ToStruct()));
            var method_response_xml = method_response.CreateDocument();
            var method_response_string = method_response_xml.ToString();

            write_string(context, method_response_string, 200);
        }

        private static System.Xml.Linq.XDocument XDocument()
        {
            var xdoc = new System.Xml.Linq.XDocument();

            var el_html = new System.Xml.Linq.XElement("html");
            xdoc.Add(el_html);

            var el_head = new System.Xml.Linq.XElement("head");
            el_html.Add(el_head);

            var el_body = new System.Xml.Linq.XElement("body");
            el_html.Add(el_body);
            return xdoc;
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

        private static void write_string(HttpListenerContext context, string text, int status_code)
        {
            byte[] b = Encoding.UTF8.GetBytes(text);
            context.Response.StatusCode = status_code;
            context.Response.KeepAlive = false;
            context.Response.ContentLength64 = b.Length;

            var output = context.Response.OutputStream;
            output.Write(b, 0, b.Length);
            context.Response.Close();
        }

        private static void log(string fmt, params object[] objects)
        {
            string s = string.Format(fmt, objects);
            logf.Write(System.DateTime.Now);
            logf.Write(" ");
            logf.WriteLine(s);
        }
    }
}