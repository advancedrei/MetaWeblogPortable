using System;
using System.Collections.Generic;
using System.Linq;
using SXL=System.Xml.Linq;
using MP=MetaWeblog.Portable;

namespace MetaWeblog.Server
{
    public partial class BlogServer
    {
        private readonly System.Net.HttpListener HttpListener;
        private readonly System.IO.StreamWriter LogStream;
        private readonly string ServerUrlPrimary;
        private readonly string ServerUrlSecondary;
        private readonly List<UserBlogInfo> BlogList;
        private readonly List<BlogUser> BlogUsers;
        private readonly PostList PostList;
        private readonly MediaObjectList MediaObjectList;
        private readonly CategoryList CategoryList;

        public readonly string HostName;
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

            CreateFolderSafe(this.Options.OutputFolder);

            // Initialize the log
            string logfilename = GetLogFilename();
            Console.WriteLine("Log at: {0}", logfilename);
            if (this.Options.OverwriteLog)
            {
                LogStream = System.IO.File.CreateText(logfilename);                
            }
            else
            {
                LogStream = System.IO.File.AppendText(logfilename);                
            }
            LogStream.AutoFlush = true;

            this.WriteLog("----------------------------------------");
            this.WriteLog("Start New Server Session ");
            this.WriteLog("----------------------------------------");

            this.HostName = Environment.MachineName.ToLower();
            // The Primary url is what will normally be used
            // However the server supports using localhost as well
            this.ServerUrlPrimary = string.Format("http://{0}:{1}/", HostName, this.Options.Port);
            this.ServerUrlSecondary = string.Format("http://{0}:{1}/", "localhost", this.Options.Port);

            this.WriteLog("Primary Url: {0}", this.ServerUrlPrimary);
            this.WriteLog("Secondary Url: {0}", this.ServerUrlSecondary);
            // The title of the blog will be based on the class name
            this.BlogTitle = "Untitled Blog";

            // This server will contain a single user

            var adminuser = new BlogUser
            {
                Name = "admin",
                Password = "password"
            };

            // Setup Collections
            this.BlogList = new List<UserBlogInfo>();
            this.BlogUsers = new List<BlogUser>();
            this.PostList = new PostList();
            this.MediaObjectList = new MediaObjectList();
            this.CategoryList = new CategoryList();

            this.BlogUsers.Add(adminuser);
            
            // This server will contain a single blog
            this.BlogList.Add(new UserBlogInfo(adminuser.Name, this.ServerUrlPrimary, this.BlogTitle));

            // Add Placeholder Content
            if (this.Options.CreateSampleContent && this.PostList.Count < 1)
            {

                if (this.CategoryList.Count < 1)
                {
                    this.CategoryList.Add("0", "sports");
                    this.CategoryList.Add("0", "biology");
                    this.CategoryList.Add("0", "office supplies");
                    this.CategoryList.Add("0", "food");
                    this.CategoryList.Add("0", "tech");                    
                }

                var cats1 = new[] {"sports","biology", "office supplies"};
                var cats2 = new[] {"food"};
                var cats3 = new[] {"food" };
                var cats4 = new[] {"biology"};

                string lipsum =
                    "Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";

                this.PostList.Add(new System.DateTime(2012, 12, 2), "1000 Amazing Uses for Staples", lipsum, cats1, true);
                this.PostList.Add(new System.DateTime(2012, 1, 15), "Why Pizza is Great", lipsum, cats2, true);
                this.PostList.Add(new System.DateTime(2013, 4, 10), "Sandwiches I have loved", lipsum, cats3, true);
                this.PostList.Add(new System.DateTime(2013, 3, 31), "Useful Things You Can Do With a Giraffe", lipsum, cats4, true);
            }

            this.HttpListener = new System.Net.HttpListener();


        }

        private void WriteLogMethodName()
        {
            string s  = new System.Diagnostics.StackFrame(1).GetMethod().Name;
            this.WriteLog(s + "()");
        }

        public void Start()
        {
            this.WriteLogMethodName();
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
            Console.WriteLine("{0} Listening...", typeof(BlogServer).Name );
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private string GetLogFilename()
        {
            // Get the absolute path to be used as the logfile
            // note that the filename will be based on the class name
            string basename = typeof(BlogServer) + ".txt";
            string logfilename = System.IO.Path.Combine(this.Options.OutputFolder, basename);
            return logfilename;
        }

        private async void Listen()
        {
            this.WriteLogMethodName();
            while (true)
            {
                var context = await HttpListener.GetContextAsync();
                Console.WriteLine("Client connected");
                //Console.WriteLine(context.Request.AcceptTypes.ToString());
                
                WriteLog("Client Connected");
                WriteLog("Request Url: {0}",context.Request.Url);
                WriteLog("Request Url Absolute Path: {0}", context.Request.Url.AbsolutePath);
                WriteLog("Request Url Path and Query: {0}", context.Request.Url.PathAndQuery);
                WriteLog("Request UserAgent: {0}", context.Request.UserAgent);
                WriteLog("Request UserHostAddress: {0}", context.Request.UserHostAddress);
                WriteLog("Request UserHostName: {0}", context.Request.UserHostName);
                WriteLog("Request UserLanguages: {0}", context.Request.UserLanguages == null ? "" : string.Join(",", context.Request.UserLanguages));
                ProcessRequest(context);
            }

            HttpListener.Close();
        }

        private void ProcessRequest(System.Net.HttpListenerContext context)
        {
            this.WriteLogMethodName();

            if (context.Request.Url.AbsolutePath == this.Options.MetaWeblogUrl)
            {
                if (context.Request.HttpMethod == "POST")
                {
                    handle_xmlrpc_method(context);                    
                }
            }
            else
            {
                if (context.Request.HttpMethod == "GET")
                {
                    handle_normal_request(context);
                    
                }
            }
        }

        private void handle_normal_request(System.Net.HttpListenerContext context)
        {
            this.WriteLogMethodName();
            if (context.Request.Url.AbsolutePath == "/")
            {
                handle_blog_home_page(context);
            }
            else if (context.Request.Url.AbsolutePath == this.Options.ArchiveUrl)
            {
                handle_blog_archive_page(context);
            }
            else if (context.Request.Url.AbsolutePath.StartsWith("/media/"))
            {
                handle_media(context);
            }
            else if (context.Request.Url.AbsolutePath == ("/debug"))
            {
                handle_debug(context);
            }
            else if (context.Request.Url.AbsolutePath.StartsWith(this.Options.PostUrl + "/"))
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
            this.WriteLogMethodName();
            var xdoc = CreateHtmlDom();
            var el_body = xdoc.Element("html").Element("body");

            el_body.AddParagraphElement(string.Format("404 {0}", "Not found"));
            el_body.AddParagraphElement(string.Format("Url.AbvsolutePath {0}", context.Request.Url.AbsolutePath));
            el_body.AddParagraphElement(string.Format("Url.Query {0}", context.Request.Url.Query));
            WriteResponseString(context, xdoc.ToString(), 404);
        }

        private void handle_media(System.Net.HttpListenerContext context)
        {
            this.WriteLogMethodName();
            var mo = this.MediaObjectList.TryGetMediaObjectByUrl(context.Request.Url.AbsolutePath + context.Request.Url.Query);
            if (mo == null)
            {
                handle_404_not_found(context);
                return;
            }

            var m = mo.Value;


            var bytes = System.Convert.FromBase64String(m.Base64Bits);

            context.Response.StatusCode = 200;
            context.Response.KeepAlive = false;
            context.Response.ContentLength64 = bytes.Length;

            var output = context.Response.OutputStream;
            output.Write(bytes, 0, bytes.Length);
            context.Response.Close();
        }

        private void handle_post(System.Net.HttpListenerContext context)
        {
            this.WriteLogMethodName();
            var tokens = context.Request.Url.AbsolutePath.Split(new char[] { '/' });

            string post_link = context.Request.Url.AbsolutePath;
            WriteLog("postlink = {0}", post_link);

            var thepost = this.PostList.TryGetPostByLink(post_link);

            if (thepost == null)
            {
                handle_404_not_found(context);
                return;
            }

            var xdoc = CreateHtmlDom();
            var el_body = xdoc.Element("html").ElementSafe("body");
            var el_div_post = GetPostContentElement(thepost.Value);

            el_body.Add(el_div_post);
                
            string html = xdoc.ToString();
            html = html.Replace(GetReplacementString(thepost.Value), thepost.Value.Description);
            WriteResponseString(context, html, 200);
        }

        private SXL.XElement GetPostContentElement(PostInfoRecord thepost)
        {
            this.WriteLogMethodName();
            var el_div_post = new System.Xml.Linq.XElement("div");
            var el_blog_content = el_div_post.AddH1Element(thepost.Title + (thepost.PostStatus == "draft" ? "[DRAFT]" : ""));
            
            var el_para_cats = el_div_post.AddParagraphElement("Categories: " + string.Join(",", thepost.Categories));
            var el_div = el_div_post.AddDivElement();

            el_div.Add(GetReplacementString(thepost));
            return el_div_post;
        }

        private string GetReplacementString(PostInfoRecord thepost)
        {
            string replacement_string = "$$$$$$$$$$" + thepost.Link + "$$$$$$$$$$";
            return replacement_string;
        }

        private void handle_blog_home_page(System.Net.HttpListenerContext context)
        {
            this.WriteLogMethodName();

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

        private void handle_debug(System.Net.HttpListenerContext context)
        {
            this.WriteLogMethodName();

            var xdoc = this.CreateHtmlDom();
            var el_body = xdoc.Element("html").Element("body");
            var el_title = el_body.AddH1Element("Debug Page");

            el_body.AddH1Element(this.PostList.Count.ToString() + " Posts");

            foreach (var kv in this.PostList.Dictionary)
            {
                var post = kv.Value;
                var key = kv.Key;
                el_body.AddH1Element(post.Title);
                el_body.AddParagraphElement(string.Format("Key=\"{0}\"", key));
                el_body.AddParagraphElement(string.Format("Title=\"{0}\"", post.Title));
                el_body.AddParagraphElement(string.Format("Link=\"{0}\"", post.Link));
                el_body.AddParagraphElement(string.Format("Permalin=\"{0}\"", post.Permalink));
                el_body.AddParagraphElement(string.Format("PostStatus=\"{0}\"", post.PostStatus));
                el_body.AddParagraphElement(string.Format("PostId=\"{0}\"", post.PostId));
                el_body.AddParagraphElement(string.Format("PostUserId=\"{0}\"", post.UserId));
            }

            el_body.AddH1Element( this.CategoryList.Count.ToString() + " Categories");
            foreach (var kv in this.CategoryList.Dictionary)
            {
                var cat = kv.Value;
                var key = kv.Key;
                el_body.AddH1Element(cat.Description);
                el_body.AddParagraphElement(string.Format("Key=\"{0}\"", key));
                el_body.AddParagraphElement(string.Format("Description=\"{0}\"", cat.Description));
                el_body.AddParagraphElement(string.Format("Name=\"{0}\"", cat.Name));
                el_body.AddParagraphElement(string.Format("BlogId=\"{0}\"", cat.BlogId));
                el_body.AddParagraphElement(string.Format("DateCreated=\"{0}\"", cat.DateCreated));
                el_body.AddParagraphElement(string.Format("Id=\"{0}\"", cat.Id));
                el_body.AddParagraphElement(string.Format("HtmlUrl=\"{0}\"", cat.HtmlUrl));
                el_body.AddParagraphElement(string.Format("RssUrl=\"{0}\"", cat.RssUrl));
            }


            string html = xdoc.ToString();

            WriteResponseString(context, html, 200);
        }

        private void handle_blog_archive_page(System.Net.HttpListenerContext context)
        {
            this.WriteLogMethodName();

            var xdoc = CreateHtmlDom();
            var el_body = xdoc.Element("html").Element("body");
            el_body.AddH1Element(this.BlogTitle);

            el_body.AddAnchorElement("/", "Home");
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


        private void respond_post(System.Net.HttpListenerContext context, PostInfoRecord post)
        {
            this.WriteLogMethodName();
            var method_response = new MP.XmlRpc.MethodResponse();
            var struct_ = post.ToPostInfo().ToStruct();
            method_response.Parameters.Add(struct_);
            WriteResponseString(context, method_response.CreateDocument().ToString(), 200);
        }

        private void respond_error_invalid_postid_parameter(System.Net.HttpListenerContext context, int status_code)
        {
            this.WriteLogMethodName();
            var f = new MP.XmlRpc.Fault();
            f.FaultCode = 2041;
            f.FaultString = string.Format("Invalid postid parameter");

            WriteResponseString(context, f.CreateDocument().ToString(), status_code);
        }

        private List<string> GetCategoriesFromArray(MP.XmlRpc.Array post_categories)
        {
            List<string> cats;
            if (post_categories.Items == null)
            {
                cats = new List<string>(0);
            }
            else
            {
                cats = new List<string>(post_categories.Count);
                foreach (var c in post_categories.Items)
                {
                    var sv = c as MP.XmlRpc.StringValue;
                    cats.Add(sv.String);
                }
            }
            return cats;
        }

        private string clean_post_title(string title)
        {
            string new_title = title;
            new_title = StringUtils.CollapseWhiteSpace(new_title);
            return new_title;
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

        public MP.XmlRpc.MethodResponse BuildStructArrayResponse(IEnumerable<MP.XmlRpc.Struct> structs)
        {
            var method_response = new MP.XmlRpc.MethodResponse();
            var arr = new MP.XmlRpc.Array();

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

        public static string GetOutputFolderRootPath()
        {
            string mydocs = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            string folder = System.IO.Path.Combine(mydocs, typeof(BlogServer).Name);
            return folder;
        }
    }
}