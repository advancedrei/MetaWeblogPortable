using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Array = MetaWeblog.Portable.XmlRpc.Array;
using SXL=System.Xml.Linq;
using MP=MetaWeblog.Portable;

namespace MetaWeblog.Portable.Server
{
    public class BlogServer
    {
        private readonly System.Net.HttpListener HttpListener = new System.Net.HttpListener();
        private readonly System.IO.StreamWriter LogStream;
        private readonly string ServerUrlPrimary;
        private readonly string ServerUrlSecondary;
        private readonly List<UserBlogInfo> BlogList = new List<UserBlogInfo>();
        private readonly PostList PostList = new PostList();
        private readonly MediaObjectList MediaObjectList = new MediaObjectList();
        private readonly List<BlogUser> BlogUsers = new List<BlogUser>();

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

            // The Primary url is what will normally be used
            // However the server supports using localhost as well
            this.ServerUrlPrimary = string.Format("http://{0}:{1}/", HostName, this.Options.Port);
            this.ServerUrlSecondary = string.Format("http://{0}:{1}/", "localhost", this.Options.Port);

            // The title of the blog will be based on the class name
            this.BlogTitle = this.GetType().Name;

            // This server will contain a single user

            var adminuser = new BlogUser
            {
                Name = "admin",
                Password = "password"
            };

            this.BlogUsers.Add(adminuser);

            // This server will contain a single blog

            this.BlogList.Add(new UserBlogInfo(adminuser.Name, this.ServerUrlPrimary, this.BlogTitle));

            // Add Placeholder Content
            if (this.Options.CreateSampleContent && this.PostList.Count < 1)
            {

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
            Console.WriteLine("{0} Listening...", this.GetType().Name );
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private string GetLogFilename()
        {
            // Get the absolute path to be used as the logfile
            // note that the filename will be based on the class name
            string basename = this.GetType().Name + ".txt";
            string logfilename = System.IO.Path.Combine(this.Options.OutputFolder, basename);
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

            if (context.Request.Url.AbsolutePath == this.Options.MetaWeblogUrl)
            {

                handle_xmlrpc_method(context);
            }
            else
            {
                handle_normal_request(context);
            }
        }

        private void handle_xmlrpc_method(HttpListenerContext context)
        {
            WriteLog("Request sent to metaweblog api - treating as XmlRpcCall");
            string body = new System.IO.StreamReader(context.Request.InputStream).ReadToEnd();
            WriteLog("Read {0} characters from input stream", body.Length);
            WriteLog("Parsing body ");
            var methodcall = MetaWeblog.Portable.XmlRpc.MethodCall.Parse(body);
            WriteLog("METHODCALL {0}", methodcall.Name);

            Console.WriteLine("Method Name: {0}", methodcall.Name);

            if (methodcall.Name == "blogger.getUsersBlogs")
            {
                handle_blogger_getUsersBlog(context, methodcall);
            }
            else if (methodcall.Name == "metaWeblog.getRecentPosts")
            {
                handle_metaWeblog_getRecentPosts(context, methodcall);
            }
            else if (methodcall.Name == "metaWeblog.newPost")
            {
                handle_metaWeblog_newPost(context, methodcall);
            }
            else if (methodcall.Name == "metaWeblog.getPost")
            {
                handle_metaWeblog_getPost(context, methodcall);
            }
            else if (methodcall.Name == "metaWeblog.editPost")
            {
                handle_metaWeblog_editPost(context, methodcall);
            }
            else if (methodcall.Name == "metaWeblog.deletePost" || methodcall.Name == "blogger.deletePost")
            {
                // Windows Live Writer seems to only send blogger.deletPost
                handle_metaWeblog_deletePost(context, methodcall);
            }
            else if (methodcall.Name == "metaWeblog.getCategories")
            {
                handle_blogger_getCategories(context, methodcall);
            }
            else if (methodcall.Name == "metaWeblog.newMediaObject")
            {
                handle_metaWeblog_newMediaObject(context, methodcall);
            }
            else
            {
                respond_unknown_xmlrpc_method(context, methodcall, body);
            }
        }

        private void handle_normal_request(System.Net.HttpListenerContext context)
        {
            WriteLog("treat as Non-XmlRpc request");

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
            WriteLog("Root page - send 404");
            var xdoc = CreateHtmlDom();
            var el_body = xdoc.Element("html").Element("body");

            el_body.AddParagraphElement(string.Format("404 {0}", "Not found"));
            el_body.AddParagraphElement(string.Format("Url.AbvsolutePath {0}", context.Request.Url.AbsolutePath));
            el_body.AddParagraphElement(string.Format("Url.Query {0}", context.Request.Url.Query));
            WriteResponseString(context, xdoc.ToString(), 404);
        }

        private void handle_media(System.Net.HttpListenerContext context)
        {
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
            WriteLog("looking for a specific post");
            var tokens = context.Request.Url.AbsolutePath.Split(new char[] {'/'});

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

        private void handle_debug(System.Net.HttpListenerContext context)
        {
            var xdoc = this.CreateHtmlDom();
            var el_body = xdoc.Element("html").Element("body");
            var el_title = el_body.AddH1Element("Debug Page");

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

            string html = xdoc.ToString();

            WriteResponseString(context, html, 200);
        }

        private void handle_blog_archive_page(System.Net.HttpListenerContext context)
        {
            WriteLog("Archive page - print out blog contents");

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

        private void respond_unknown_xmlrpc_method(System.Net.HttpListenerContext context, MP.XmlRpc.MethodCall methodcall, string body)
        {
            WriteLog("Unhandled XmlRpcMethod {0}", methodcall.Name);
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

            this.WriteLog("PostId = {0}", postid.String);
            this.WriteLog("Username = {0}", username.String);

            var post = this.PostList.TryGetPostById(postid.String);

            if (post == null)
            {
                // Post was not found
                respond_error_invalid_postid_parameter(context, 200);
                return;
            }

            // Post was found
            respond_post(context, post.Value);
        }

        private void respond_post(HttpListenerContext context, PostInfoRecord post)
        {
            var method_response = new XmlRpc.MethodResponse();
            var struct_ = post.ToPostInfo().ToStruct();
            method_response.Parameters.Add(struct_);
            WriteResponseString(context, method_response.CreateDocument().ToString(), 200);
        }

        private void respond_error_invalid_postid_parameter(HttpListenerContext context, int status_code)
        {
            var f = new XmlRpc.Fault();
            f.FaultCode = 2041;
            f.FaultString = string.Format("Invalid postid parameter");

            WriteResponseString(context, f.CreateDocument().ToString(), status_code);
        }


        private void handle_metaWeblog_newMediaObject(System.Net.HttpListenerContext context, MP.XmlRpc.MethodCall methodcall)
        {
            var blogid = (XmlRpc.StringValue) methodcall.Parameters[0];
            var username = (XmlRpc.StringValue) methodcall.Parameters[1];
            var password = (XmlRpc.StringValue) methodcall.Parameters[2];

            this.WriteLog("BlogId = {0}",blogid.String);
            this.WriteLog("Username = {0}", username.String);

            var struct_ = (XmlRpc.Struct)methodcall.Parameters[3];

            var name = struct_.Get<XmlRpc.StringValue>("name");
            var type = struct_.Get<XmlRpc.StringValue>("type");
            var bits = struct_.Get<XmlRpc.Base64Data>("bits");

            this.WriteLog("Name = {0}", name.String);
            this.WriteLog("Type = {0}", type.String);
            this.WriteLog("Bits  = {0} Bytes Characters", bits.Bytes);

            var mo = this.MediaObjectList.StoreNewObject(blogid.String, username.String, name.String, type.String,
                Convert.ToBase64String(bits.Bytes));

            var s_ = new XmlRpc.Struct();
            s_["url"] = new XmlRpc.StringValue(this.ServerUrlPrimary + mo.Url.Substring(1));

            var method_response = new XmlRpc.MethodResponse();
            method_response.Parameters.Add(s_);

            string response_body = method_response.CreateDocument().ToString();

            this.WriteLog(response_body);
            WriteResponseString(context, response_body, 200);
        }

        public static void CreateFolderSafe(string f1)
        {
            if (!System.IO.Directory.Exists(f1))
            {
                System.IO.Directory.CreateDirectory(f1);
            }
        }


        private void handle_metaWeblog_newPost(System.Net.HttpListenerContext context, MP.XmlRpc.MethodCall methodcall)
        {
            var blogid = (XmlRpc.StringValue) methodcall.Parameters[0];
            var username = (XmlRpc.StringValue) methodcall.Parameters[1];
            var password = (XmlRpc.StringValue) methodcall.Parameters[2];

            this.WriteLog("BlogId = {0}",blogid.String);
            this.WriteLog("Username = {0}", username.String);

            var struct_ = (XmlRpc.Struct)methodcall.Parameters[3];
            var publish = (XmlRpc.BooleanValue) methodcall.Parameters[4];
            var post_title = struct_.Get<XmlRpc.StringValue>("title").String;
            post_title = clean_post_title(post_title);

            var post_description = struct_.Get<XmlRpc.StringValue>("description");
            var post_categories = struct_.Get<XmlRpc.Array>("categories", null);

            var cats = GetCategoriesFromArray(post_categories);

            this.WriteLog( " Categories {0}", string.Join(",",cats));
            var new_post = this.PostList.Add(null, post_title, post_description.String, cats, publish.Boolean);

            var method_response = new XmlRpc.MethodResponse();
            method_response.Parameters.Add(new_post.PostId);

            this.WriteLog("New Post Created with ID = {0}", new_post.PostId);
            WriteResponseString(context, method_response.CreateDocument().ToString(), 200);
        }

        private List<string> GetCategoriesFromArray(Array post_categories)
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
                    var sv = c as XmlRpc.StringValue;
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

        private void handle_metaWeblog_deletePost(System.Net.HttpListenerContext context, MP.XmlRpc.MethodCall methodcall)
        {
            var appkey = (XmlRpc.StringValue)methodcall.Parameters[0];
            var postid = (XmlRpc.StringValue)methodcall.Parameters[1];
            var username = (XmlRpc.StringValue)methodcall.Parameters[2];
            var password = (XmlRpc.StringValue)methodcall.Parameters[3];

            this.WriteLog("AppKey = {0}", postid.String);
            this.WriteLog("PostId = {0}", postid.String);
            this.WriteLog("Username = {0}", username.String);

            var post = this.PostList.TryGetPostById(postid.String);

            if (post == null)
            {
                this.WriteLog("No such Post with ID {0}", postid.String);
                // Post was not found
                respond_error_invalid_postid_parameter(context, 404);
                return;
            }

            // Post was found
            this.WriteLog("Found Post with ID {0}", postid.String);
            this.PostList.Delete(post.Value);

            var method_response = new XmlRpc.MethodResponse();
            method_response.Parameters.Add(true); // this is supposed to always return true
            WriteResponseString(context, method_response.CreateDocument().ToString(), 200);
        }


        private void handle_metaWeblog_editPost(System.Net.HttpListenerContext context, MP.XmlRpc.MethodCall methodcall)
        {
            var postid = (XmlRpc.StringValue)methodcall.Parameters[0];
            var username = (XmlRpc.StringValue)methodcall.Parameters[1];
            var password = (XmlRpc.StringValue)methodcall.Parameters[2];
            var struct_ = (XmlRpc.Struct)methodcall.Parameters[3];
            var publish = (XmlRpc.BooleanValue)methodcall.Parameters[4];

            this.WriteLog("PostId = {0}", postid.String);
            this.WriteLog("Username = {0}", username.String);
            this.WriteLog("Publish = {0}", publish.Boolean);

            var post = this.PostList.TryGetPostById(postid.String);

            if (post == null)
            {
                // Post was not found
                respond_error_invalid_postid_parameter(context, 200);
                return;
            }
            var newpost = post.Value;

            // Post was found
            var post_title = struct_.Get<XmlRpc.StringValue>("title",null);
            if (post_title.String != null)
            {
                newpost.Title = clean_post_title(post_title.String);
            }

            var post_description = struct_.Get<XmlRpc.StringValue>("description", null);
            if (post_description.String != null)
            {
                newpost.Description = post_description.String;
            }


            var post_categories = struct_.Get<XmlRpc.Array>("categories", null);
            if (post_categories.Items != null)
            {
                // Reset the post categories
                var cats = GetCategoriesFromArray(post_categories);
                newpost.Categories = string.Join(";", cats);
            }

            if (publish.Boolean)
            {
                newpost.PostStatus = "published";
            }
            else
            {
                newpost.PostStatus = "draft";                    
            }

            this.PostList.Dictionary[newpost.PostId] = newpost;
                
            var method_response = new XmlRpc.MethodResponse();
            method_response.Parameters.Add(true); // this is supposed to always return true
            WriteResponseString(context, method_response.CreateDocument().ToString(), 200);
        }

        private void handle_metaWeblog_getRecentPosts(System.Net.HttpListenerContext context, MP.XmlRpc.MethodCall methodcall)
        {
            var method_response = BuildStructArrayResponse(this.PostList.Select(i => i.ToPostInfo().ToStruct()));
            var method_response_xml = method_response.CreateDocument();
            var method_response_string = method_response_xml.ToString();

            WriteResponseString(context, method_response_string, 200);
        }

        private void handle_blogger_getUsersBlog(System.Net.HttpListenerContext context, MP.XmlRpc.MethodCall methodcall)
        {
            var method_response = BuildStructArrayResponse(this.BlogList.Select(i => i.ToStruct()));
            var method_response_xml = method_response.CreateDocument();
            var method_response_string = method_response_xml.ToString();

            WriteResponseString(context, method_response_string, 200);
        }

        private void handle_blogger_getCategories(System.Net.HttpListenerContext context, MP.XmlRpc.MethodCall methodcall)
        {
            var hs = this.PostList.GetCategories();

            this.WriteLog(" Categories {0}", string.Join(",", hs));

            var method_response = BuildStructArrayResponse( hs.Select( cat => CatToStruct(cat)));
            var method_response_xml = method_response.CreateDocument();
            var method_response_string = method_response_xml.ToString();

            WriteResponseString(context, method_response_string, 200);
        }

        private XmlRpc.Struct CatToStruct(string cat)
        {
            var struct_ = new XmlRpc.Struct();
            struct_["name"] = new XmlRpc.StringValue(cat);
            struct_["description"] = new XmlRpc.StringValue(cat);
            return struct_;
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

        public MP.XmlRpc.MethodResponse BuildStructArrayResponse(IEnumerable<XmlRpc.Struct> structs)
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

        public static string GetOutputFolderRootPath()
        {
            string mydocs = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            string folder = System.IO.Path.Combine(mydocs, typeof(BlogServer).Name);
            return folder;
        }

    }
}