using System;
using System.Collections.Generic;
using System.Linq;
using SXL=System.Xml.Linq;
using MP=MetaWeblog.Portable;

namespace MetaWeblog.Portable.Server
{
    public class BlogServer
    {
        private readonly System.Net.HttpListener HttpListener = new System.Net.HttpListener();
        private readonly System.IO.StreamWriter LogStream;
        private readonly string ServerUrl;

        public readonly int ListeningPort = 14228;
        public readonly string HostName = "localhost";
        public readonly List<UserBlogInfo> BlogList = new List<UserBlogInfo>();
        public readonly List<PostInfo> PostList = new List<PostInfo>();

        public readonly string logfilename;

        public BlogServer()
        {
            var logfilename = GetLogFilename();
            LogStream = System.IO.File.AppendText(logfilename);
            LogStream.AutoFlush = true;
            ServerUrl = string.Format("http://{0}:{1}/", HostName, ListeningPort);
        }

        public void Start()
        {
            HttpListener.Prefixes.Add(ServerUrl);
            HttpListener.Start();
            Listen();
            Console.WriteLine("{0} Listening...", this.GetType().Name );
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static string GetLogFilename()
        {
            string mydocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string logfilename = System.IO.Path.Combine(mydocs, "BlogServer.txt");
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
                WriteLog("    Request Url: {0}",context.Request.Url);
                WriteLog("    Request Url Absolute Path: {0}", context.Request.Url.AbsolutePath);
                ProcessRequest(context);
            }

            HttpListener.Close();
        }

        private void ProcessRequest(System.Net.HttpListenerContext context)
        {
            WriteLog("{0}.ProcessRequest() started", this.GetType().Name);

            if (context.Request.Url.AbsolutePath == "/metaweblogapi")
            {
                WriteLog("    Request sent to metaweblog api - treating as XmlRpcCall");
                string body = new System.IO.StreamReader(context.Request.InputStream).ReadToEnd();
                WriteLog("    Read {0} characters from input stream", body.Length);
                WriteLog("    Parsing body ");                
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
            var xdoc = XDocument();
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
                var xdoc = XDocument();
                var el_body = xdoc.Element("html").Element("body");
                el_body.Value = string.Format("Not found {0}", context.Request.Url.AbsolutePath);
                WriteResponseString(context, xdoc.ToString(), 404);
            }
            else
            {
                var xdoc = XDocument();
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
            var el_blog_content = new System.Xml.Linq.XElement("h1", thepost.Title);
            var el_div = new System.Xml.Linq.XElement("div");
            el_div_post.Add(el_blog_content);
            el_div_post.Add(el_div);
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

            var xdoc = this.XDocument();
            var el_body = xdoc.Element("html").Element("body");
            el_body.Add(new System.Xml.Linq.XElement("h1", "Blog Home"));

            var el_para0 = new System.Xml.Linq.XElement("p");
            el_body.Add(el_para0);

            var el_a0 = new System.Xml.Linq.XElement("a");
            el_a0.SetAttributeValue("href", "/archive");
            el_a0.Value = "Archive";
            el_para0.Add(el_a0);

            foreach (var post in this.PostList)
            {
                var el_para = new System.Xml.Linq.XElement("p");
                el_body.Add(el_para);

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

            var xdoc = XDocument();
            var el_body = xdoc.Element("html").Element("body");
            el_body.Add(new System.Xml.Linq.XElement("h1", "Blog Home"));

            foreach (var post in this.PostList)
            {
                var el_para = new System.Xml.Linq.XElement("p");
                el_body.Add(el_para);

                var el_text =
                    new System.Xml.Linq.XText(post.DateCreated == null
                        ? "No Publish Date"
                        : post.DateCreated.Value.ToShortDateString());
                el_para.Add(el_text);

                var el_a = new System.Xml.Linq.XElement("a");
                el_a.SetAttributeValue("href", post.Link);
                el_a.Value = post.Title;
                el_para.Add(el_a);
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
                    var mr1 = new XmlRpc.MethodResponse();
                    var struct_ = p.ToStruct();
                    mr1.Parameters.Add(struct_);
                    WriteResponseString(context, mr1.CreateDocument().ToString(), 200);
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

            var mr1 = new XmlRpc.MethodResponse();
            var arr1 = new XmlRpc.StringValue(np.PostId);

            mr1.Parameters.Add(arr1);

            WriteResponseString(context, mr1.CreateDocument().ToString(), 200);
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

        private System.Xml.Linq.XDocument XDocument()
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

        public MP.XmlRpc.MethodResponse BuildStructArrrayResponse(IEnumerable<XmlRpc.Struct> structs)
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

        private void WriteResponseString(System.Net.HttpListenerContext context, string text, int status_code)
        {
            byte[] b = System.Text.Encoding.UTF8.GetBytes(text);
            context.Response.StatusCode = status_code;
            context.Response.KeepAlive = false;
            context.Response.ContentLength64 = b.Length;

            var output = context.Response.OutputStream;
            output.Write(b, 0, b.Length);
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