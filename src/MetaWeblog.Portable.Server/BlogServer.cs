using System;
using System.Collections.Generic;
using System.Linq;
using SXL=System.Xml.Linq;
using MP=MetaWeblog.Portable;

namespace MetaWeblog.Portable.Server
{
    public class PostList: IEnumerable<PostInfo>
    {
        private readonly List<PostInfo> items = new List<PostInfo>();

        public PostList()
        {
            
        }

        public IEnumerator<PostInfo> GetEnumerator()
        {
            return this.items.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(PostInfo p)
        {
            this.items.Add(p);
            this.Sort();
        }

        public void Sort()
        {
            var unpublished_dt = System.DateTime.Now;
            this.items.Sort(
                (x, y) =>
                    y.DateCreated.GetValueOrDefault(unpublished_dt).CompareTo(x.DateCreated.GetValueOrDefault(unpublished_dt)));
        }


    }
    public class BlogServer
    {
        private readonly System.Net.HttpListener HttpListener = new System.Net.HttpListener();
        private readonly System.IO.StreamWriter LogStream;
        private readonly string ServerUrlPrimary;
        private readonly string ServerUrlSecondary;
        private readonly List<UserBlogInfo> BlogList = new List<UserBlogInfo>();
        private readonly PostList PostList = new PostList();

        public readonly string HostName = Environment.MachineName.ToLower();
        public readonly string LogFilename;
        public string BlogTitle;

        public BlogServerOptions Options;

        public BlogServer(BlogServerOptions options)
        {
            if (options == null)
            {
                throw new System.ArgumentNullException("options");
            }
            this.Options = options;

            // Initialize the log
            string logfilename = GetLogFilename();
            Console.WriteLine("Log at: {0}", logfilename);
            LogStream = System.IO.File.AppendText(logfilename);
            LogStream.AutoFlush = true;

            // The Primary url is what will normally be used
            // However the server supports using localhost as well
            this.ServerUrlPrimary = string.Format("http://{0}:{1}/", HostName, this.Options.Port);
            this.ServerUrlSecondary = string.Format("http://{0}:{1}/", "localhost", this.Options.Port);

            // The title of the blog will be based on the class name
            this.BlogTitle = this.GetType().Name;

            // This server will contain a single blog
            this.BlogList.Add(new UserBlogInfo("admin", this.ServerUrlPrimary, this.BlogTitle));            

            // Add Dummy Content
            if (this.Options.CreateDefaultPosts)
            {
                this.PostList.Add(new PostInfo { DateCreated = new System.DateTime(2012, 12, 2), Title = "1000 Amazing Uses for Staples", Description = "staples", PostId = "20", Link = "/post/1000AmazingUsesForStaples" });
                this.PostList.Add(new PostInfo { DateCreated = new System.DateTime(2012, 1, 15), Title = "Why Pizza is Great", Description = "pizza", PostId = "10", Link = "/post/WhyPizzaIsGreat" });
                this.PostList.Add(new PostInfo { DateCreated = new System.DateTime(2013, 4, 10), Title = "Sandwiches I have loved", Description = "d4", PostId = "sandwiches", Link = "/post/SandwichesIHaveLoved" });
                this.PostList.Add(new PostInfo { DateCreated = new System.DateTime(2013, 3, 31), Title = "Useful Things You Can Do With a Giraffe", Description = "giraffe", PostId = "30", Link = "/post/UsefulThingsYouCanDoWithAGiraffe" });

                this.PostList.Sort();
            }
        }

        public void Start()
        {
            Console.WriteLine("{0}", this.ServerUrlPrimary);
            Console.WriteLine("{0}", this.ServerUrlSecondary);

            HttpListener.Prefixes.Add(this.ServerUrlPrimary);
            HttpListener.Prefixes.Add(this.ServerUrlSecondary);
            
            // IMPORTANT: If you aren't running this with elevated privileges
            // then the Start() method below will throw an
            // System.Net.HttpListenerException and will have "Access is denied"
            // in its Additional Information

            HttpListener.Start();
            Listen();
            Console.WriteLine("{0} Listening...", this.GetType().Name );
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private string GetLogFilename()
        {
            // Get the absolute path to be used as the logfile
            // note that the filename will be based on the class name
            string mydocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string basename = this.GetType().Name + ".txt";
            string logfilename = System.IO.Path.Combine(mydocs, basename);
            return logfilename;
        }

        private async void Listen()
        {
            WriteLog("{0}.Listen() started", this.GetType().Name);
            while (true)
            {
                var context = await HttpListener.GetContextAsync();
                Console.WriteLine("Client connected");
                //Console.WriteLine(context.Request.AcceptTypes.ToString());
                
                WriteLog("Client Connected");
                WriteLog("Request Url: {0}",context.Request.Url);
                WriteLog("Request Url Absolute Path: {0}", context.Request.Url.AbsolutePath);
                ProcessRequest(context);
            }

            HttpListener.Close();
        }

        private void ProcessRequest(System.Net.HttpListenerContext context)
        {
            WriteLog("{0}.ProcessRequest() started", this.GetType().Name);

            if (context.Request.Url.AbsolutePath == "/metaweblogapi")
            {
                WriteLog("Request sent to metaweblog api - treating as XmlRpcCall");
                string body = new System.IO.StreamReader(context.Request.InputStream).ReadToEnd();
                WriteLog("Read {0} characters from input stream", body.Length);
                WriteLog("Parsing body ");                
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

        private void handle_normal_request(System.Net.HttpListenerContext context)
        {
            WriteLog("treat as Non-XmlRpc request");

            if (context.Request.Url.AbsolutePath == "/")
            {
                handle_blog_home_page(context);
            }
            else if (context.Request.Url.AbsolutePath == "/archive" || context.Request.Url.AbsolutePath == "/archive/")
            {
                handle_blog_archive_page(context);
            }
            else if (context.Request.Url.AbsolutePath.StartsWith("/post/"))
            {
                handle_post(context);
            }
            else
            {
                handle_404_not_found(context);
            }
        }

        private void handle_404_not_found(System.Net.HttpListenerContext context)
        {
            WriteLog("Root page - send 404");
            var xdoc = CreateHtmlDom();
            var el_body = xdoc.Element("html").Element("body");
            el_body.Value = string.Format("Not found {0}", context.Request.Url.AbsolutePath);
            WriteResponseString(context, xdoc.ToString(), 404);
        }

        private void handle_post(System.Net.HttpListenerContext context)
        {
            WriteLog("looking for a specific post");
            var tokens = context.Request.Url.AbsolutePath.Split(new char[] {'/'});

            string post_link = context.Request.Url.AbsolutePath;
            WriteLog("postlink = {0}", post_link);

            PostInfo thepost = null;
            foreach (var post in this.PostList)
            {
                if (post.Link == post_link)
                {
                    thepost = post;
                }
            }

            if (thepost == null)
            {
                WriteLog("Root page - send 404");
                var xdoc = CreateHtmlDom();
                var el_body = xdoc.Element("html").Element("body");
                el_body.Value = string.Format("Not found {0}", context.Request.Url.AbsolutePath);
                WriteResponseString(context, xdoc.ToString(), 404);
            }
            else
            {
                var xdoc = CreateHtmlDom();
                var el_body = xdoc.Element("html").Element("body");
                var el_div_post = GetPostContentElement(thepost);
                el_body.Add(el_div_post);
                
                string html = xdoc.ToString();
                html = html.Replace(GetReplacementString(thepost), thepost.Description);
                WriteResponseString(context, html, 200);
            }
        }

        private SXL.XElement GetPostContentElement(PostInfo thepost)
        {
            var el_div_post = new System.Xml.Linq.XElement("div");
            var el_blog_content = el_div_post.AddH1Element(thepost.Title);
            var el_div = el_div_post.AddDivElement();

            el_div.Add(GetReplacementString(thepost));
            return el_div_post;
        }

        private string GetReplacementString(PostInfo thepost)
        {
            string replacement_string = "$$$$$$$$$$" + thepost.Link + "$$$$$$$$$$";
            return replacement_string;
        }

        private void handle_blog_home_page(System.Net.HttpListenerContext context)
        {
            WriteLog("Root page - print out blog contents");

            var xdoc = this.CreateHtmlDom();
            var el_body = xdoc.Element("html").Element("body");
            var el_title = el_body.AddH1Element(this.BlogTitle);

            var el_para0 = el_body.AddParagraphElement();

            el_para0.AddAnchorElement("/archive", "Archive");

            foreach (var post in this.PostList)
            {
                var el_para = el_body.AddParagraphElement();

                var post_content_el = GetPostContentElement(post);
                el_body.Add(post_content_el);
            }

            string html = xdoc.ToString();
            foreach (var post in this.PostList)
            {
                string replacement_string = GetReplacementString(post);
                html = html.Replace(replacement_string, post.Description);
            }

            WriteResponseString(context, html, 200);
        }


        private void handle_blog_archive_page(System.Net.HttpListenerContext context)
        {
            WriteLog("Archive page - print out blog contents");

            var xdoc = CreateHtmlDom();
            var el_body = xdoc.Element("html").Element("body");
            el_body.AddH1Element(this.BlogTitle);

            foreach (var post in this.PostList)
            {
                var el_para = el_body.AddParagraphElement();

                var el_text =
                    new System.Xml.Linq.XText(post.DateCreated == null
                        ? "No Publish Date"
                        : post.DateCreated.Value.ToShortDateString());
                el_para.Add(el_text);

                el_para.AddAnchorElement(post.Link, post.Title);
            }
            WriteResponseString(context, xdoc.ToString(), 200);
        }

        private void handle_unknown_xmlrpc_method(System.Net.HttpListenerContext context, MP.XmlRpc.MethodCall methodcall, string body)
        {
            WriteLog("    Unhandled XmlRpcMethod {0}", methodcall.Name);
            WriteLog("{0}", body);

            var f = new XmlRpc.Fault();
            f.FaultCode = 0;
            f.FaultString = string.Format("unsupported method {0}", methodcall.Name);

            WriteResponseString(context, f.CreateDocument().ToString(), 200);
        }

        private void handle_metaWeblog_getPost(System.Net.HttpListenerContext context, MP.XmlRpc.MethodCall methodcall)
        {
            var postid = (XmlRpc.StringValue) methodcall.Parameters[0];
            var username = (XmlRpc.StringValue) methodcall.Parameters[1];
            var password = (XmlRpc.StringValue) methodcall.Parameters[2];

            foreach (var p in this.PostList)
            {
                if (p.PostId == postid.String)
                {
                    var method_response = new XmlRpc.MethodResponse();
                    var struct_ = p.ToStruct();
                    method_response.Parameters.Add(struct_);
                    WriteResponseString(context, method_response.CreateDocument().ToString(), 200);
                }
            }
        }

        private void handle_metaWeblog_newPost(System.Net.HttpListenerContext context, MP.XmlRpc.MethodCall methodcall)
        {
            var blogid = (XmlRpc.StringValue) methodcall.Parameters[0];
            var username = (XmlRpc.StringValue) methodcall.Parameters[1];
            var password = (XmlRpc.StringValue) methodcall.Parameters[2];
            var struct_ = (XmlRpc.Struct) methodcall.Parameters[3];
            var post_status = (XmlRpc.BooleanValue) methodcall.Parameters[4];
            var post_title = struct_.Get<XmlRpc.StringValue>("title");
            var post_description = struct_.Get<XmlRpc.StringValue>("description");
            var post_categories = struct_.Get<XmlRpc.Array>("categories", null);


            var link = TitleToPostId(post_title);

            var np = new PostInfo();
            np.Title = post_title.String;
            np.Description = post_description.String;
            //np.Categories??
            np.DateCreated = System.DateTime.Now;
            np.Link = link;
            np.Permalink = link;
            np.PostId = System.DateTime.Now.Ticks.ToString();
            np.Permalink = np.Link;

            this.PostList.Add(np);

            var method_response = new XmlRpc.MethodResponse();
            method_response.Parameters.Add(np.PostId);

            WriteResponseString(context, method_response.CreateDocument().ToString(), 200);
        }

        private string TitleToPostId(MP.XmlRpc.StringValue post_title)
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

        private void handle_metaWeblog_getRecentPosts(System.Net.HttpListenerContext context)
        {
            var method_response = BuildStructArrrayResponse(this.PostList.Select(i => i.ToStruct()));
            var method_response_xml = method_response.CreateDocument();
            var method_response_string = method_response_xml.ToString();

            WriteResponseString(context, method_response_string, 200);
        }

        private void handle_blogger_getUsersBlog(System.Net.HttpListenerContext context)
        {
            var method_response = BuildStructArrrayResponse(this.BlogList.Select(i => i.ToStruct()));
            var method_response_xml = method_response.CreateDocument();
            var method_response_string = method_response_xml.ToString();

            WriteResponseString(context, method_response_string, 200);
        }

        private System.Xml.Linq.XDocument CreateHtmlDom()
        {
            var xdoc = new System.Xml.Linq.XDocument();

            var el_html = new System.Xml.Linq.XElement("html");
            xdoc.Add(el_html);

            var el_head = new System.Xml.Linq.XElement("head");
            el_html.Add(el_head);

            var el_style = new System.Xml.Linq.XElement("style");
            el_head.Add(el_style);

            el_style.Value = this.Options.StyleSheet;

            var el_body = new System.Xml.Linq.XElement("body");
            el_html.Add(el_body);
            return xdoc;
        }

        public MP.XmlRpc.MethodResponse BuildStructArrrayResponse(IEnumerable<XmlRpc.Struct> structs)
        {
            var method_response = new XmlRpc.MethodResponse();
            var arr = new XmlRpc.Array();

            foreach (var struct_ in structs)
            {
                arr.Add(struct_);
            }

            method_response.Parameters.Add(arr);

            return method_response;
        }

        private void WriteResponseString(System.Net.HttpListenerContext context, string text, int status_code)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
            context.Response.StatusCode = status_code;
            context.Response.KeepAlive = false;
            context.Response.ContentLength64 = bytes.Length;

            var output = context.Response.OutputStream;
            output.Write(bytes, 0, bytes.Length);
            context.Response.Close();
        }

        private void WriteLog(string fmt, params object[] objects)
        {
            string s = string.Format(fmt, objects);
            LogStream.Write(System.DateTime.Now);
            LogStream.Write(" ");
            LogStream.WriteLine(s);
        }
    }
}