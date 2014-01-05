using System;
using System.Collections.Generic;
using System.Linq;
using SXL=System.Xml.Linq;
using MP=MetaWeblog.Portable;

namespace MetaWeblog.Server
{
    public partial class BlogServer
    {
        private void handle_xmlrpc_method(System.Net.HttpListenerContext context)
        {
            this.WriteLogMethodName();

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


        private void respond_unknown_xmlrpc_method(System.Net.HttpListenerContext context, MP.XmlRpc.MethodCall methodcall, string body)
        {
            this.WriteLogMethodName();

            WriteLog("Unhandled XmlRpcMethod {0}", methodcall.Name);
            WriteLog("{0}", body);

            var f = new MP.XmlRpc.Fault();
            f.FaultCode = 0;
            f.FaultString = string.Format("unsupported method {0}", methodcall.Name);

            WriteResponseString(context, f.CreateDocument().ToString(), 200);
        }

        private void handle_metaWeblog_getPost(System.Net.HttpListenerContext context, MP.XmlRpc.MethodCall methodcall)
        {
            this.WriteLogMethodName();

            var postid = (MP.XmlRpc.StringValue)methodcall.Parameters[0];
            var username = (MP.XmlRpc.StringValue)methodcall.Parameters[1];
            var password = (MP.XmlRpc.StringValue)methodcall.Parameters[2];

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

        private void handle_metaWeblog_newMediaObject(System.Net.HttpListenerContext context, MP.XmlRpc.MethodCall methodcall)
        {
            this.WriteLogMethodName();

            var blogid = (MP.XmlRpc.StringValue)methodcall.Parameters[0];
            var username = (MP.XmlRpc.StringValue)methodcall.Parameters[1];
            var password = (MP.XmlRpc.StringValue)methodcall.Parameters[2];

            this.WriteLog("BlogId = {0}", blogid.String);
            this.WriteLog("Username = {0}", username.String);

            var struct_ = (MP.XmlRpc.Struct)methodcall.Parameters[3];

            var name = struct_.Get<MP.XmlRpc.StringValue>("name");
            var type = struct_.Get<MP.XmlRpc.StringValue>("type");
            var bits = struct_.Get<MP.XmlRpc.Base64Data>("bits");

            this.WriteLog("Name = {0}", name.String);
            this.WriteLog("Type = {0}", type.String);
            this.WriteLog("Bits  = {0} Bytes Characters", bits.Bytes);

            var mo = this.MediaObjectList.StoreNewObject(blogid.String, username.String, name.String, type.String,
                Convert.ToBase64String(bits.Bytes));

            var s_ = new MP.XmlRpc.Struct();
            s_["url"] = new MP.XmlRpc.StringValue(this.ServerUrlPrimary + mo.Url.Substring(1));

            var method_response = new MP.XmlRpc.MethodResponse();
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
            this.WriteLogMethodName();

            var blogid = (MP.XmlRpc.StringValue)methodcall.Parameters[0];
            var username = (MP.XmlRpc.StringValue)methodcall.Parameters[1];
            var password = (MP.XmlRpc.StringValue)methodcall.Parameters[2];

            this.WriteLog("BlogId = {0}", blogid.String);
            this.WriteLog("Username = {0}", username.String);

            var struct_ = (MP.XmlRpc.Struct)methodcall.Parameters[3];
            var publish = (MP.XmlRpc.BooleanValue)methodcall.Parameters[4];
            var post_title = struct_.Get<MP.XmlRpc.StringValue>("title").String;
            post_title = clean_post_title(post_title);

            var post_description = struct_.Get<MP.XmlRpc.StringValue>("description");
            var post_categories = struct_.Get<MP.XmlRpc.Array>("categories", null);

            var cats = GetCategoriesFromArray(post_categories);

            this.WriteLog(" Categories {0}", string.Join(",", cats));
            var new_post = this.PostList.Add(null, post_title, post_description.String, cats, publish.Boolean);

            var method_response = new MP.XmlRpc.MethodResponse();
            method_response.Parameters.Add(new_post.PostId);

            this.WriteLog("New Post Created with ID = {0}", new_post.PostId);
            WriteResponseString(context, method_response.CreateDocument().ToString(), 200);
        }

        private void handle_metaWeblog_deletePost(System.Net.HttpListenerContext context, MP.XmlRpc.MethodCall methodcall)
        {
            this.WriteLogMethodName();

            var appkey = (MP.XmlRpc.StringValue)methodcall.Parameters[0];
            var postid = (MP.XmlRpc.StringValue)methodcall.Parameters[1];
            var username = (MP.XmlRpc.StringValue)methodcall.Parameters[2];
            var password = (MP.XmlRpc.StringValue)methodcall.Parameters[3];

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

            var method_response = new MP.XmlRpc.MethodResponse();
            method_response.Parameters.Add(true); // this is supposed to always return true
            WriteResponseString(context, method_response.CreateDocument().ToString(), 200);
        }


        private void handle_metaWeblog_editPost(System.Net.HttpListenerContext context, MP.XmlRpc.MethodCall methodcall)
        {
            this.WriteLogMethodName();

            var postid = (MP.XmlRpc.StringValue)methodcall.Parameters[0];
            var username = (MP.XmlRpc.StringValue)methodcall.Parameters[1];
            var password = (MP.XmlRpc.StringValue)methodcall.Parameters[2];
            var struct_ = (MP.XmlRpc.Struct)methodcall.Parameters[3];
            var publish = (MP.XmlRpc.BooleanValue)methodcall.Parameters[4];

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
            var post_title = struct_.Get<MP.XmlRpc.StringValue>("title", null);
            if (post_title.String != null)
            {
                newpost.Title = clean_post_title(post_title.String);
            }

            var post_description = struct_.Get<MP.XmlRpc.StringValue>("description", null);
            if (post_description.String != null)
            {
                newpost.Description = post_description.String;
            }


            var post_categories = struct_.Get<MP.XmlRpc.Array>("categories", null);
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

            var method_response = new MP.XmlRpc.MethodResponse();
            method_response.Parameters.Add(true); // this is supposed to always return true
            WriteResponseString(context, method_response.CreateDocument().ToString(), 200);
        }

        private void handle_metaWeblog_getRecentPosts(System.Net.HttpListenerContext context, MP.XmlRpc.MethodCall methodcall)
        {
            this.WriteLogMethodName();

            var method_response = BuildStructArrayResponse(this.PostList.Select(i => i.ToPostInfo().ToStruct()));
            var method_response_xml = method_response.CreateDocument();
            var method_response_string = method_response_xml.ToString();

            WriteResponseString(context, method_response_string, 200);
        }

        private void handle_blogger_getUsersBlog(System.Net.HttpListenerContext context, MP.XmlRpc.MethodCall methodcall)
        {
            this.WriteLogMethodName();

            var method_response = BuildStructArrayResponse(this.BlogList.Select(i => i.ToStruct()));
            var method_response_xml = method_response.CreateDocument();
            var method_response_string = method_response_xml.ToString();

            WriteResponseString(context, method_response_string, 200);
        }

        private void handle_blogger_getCategories(System.Net.HttpListenerContext context, MP.XmlRpc.MethodCall methodcall)
        {
            this.WriteLogMethodName();

            var hs = this.PostList.GetCategories();

            this.WriteLog(" Categories {0}", string.Join(",", hs));

            var method_response = BuildStructArrayResponse(hs.Select(cat => CatToStruct(cat)));
            var method_response_xml = method_response.CreateDocument();
            var method_response_string = method_response_xml.ToString();

            WriteResponseString(context, method_response_string, 200);
        }

        private MP.XmlRpc.Struct CatToStruct(string cat)
        {
            var struct_ = new MP.XmlRpc.Struct();
            struct_["name"] = new MP.XmlRpc.StringValue(cat);
            struct_["description"] = new MP.XmlRpc.StringValue(cat);
            return struct_;
        }
    }
}