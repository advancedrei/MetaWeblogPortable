using System;
using System.Collections.Generic;
using System.Linq;
using SXL=System.Xml.Linq;
using MP=MetaWeblog.Portable;

namespace MetaWeblog.Server
{
    public partial class BlogServer
    {
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
            else if (context.Request.Url.AbsolutePath.StartsWith("/category/"))
            {
                handle_category(context);
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
            WriteResponseString(context, xdoc.ToString(), 404, ContentType_TextHtml);
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
            WriteResponseString(context, html, 200, ContentType_TextXml);
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

            foreach (var cat in this.CategoryList)
            {
                var p = el_body.AddParagraphElement();
                p.AddAnchorElement(cat.HtmlUrl, cat.Name);
            }

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

            WriteResponseString(context, html, 200, ContentType_TextHtml);
        }

        private void handle_category(System.Net.HttpListenerContext context)
        {
            var tokens = context.Request.Url.AbsolutePath.Split(new char[] { '/' });

            this.WriteLogMethodName();

            var xdoc = this.CreateHtmlDom();
            var el_body = xdoc.Element("html").Element("body");
            var el_title = el_body.AddH1Element("Debug Page");

            el_body.AddH1Element("Posts");

            foreach (var post in this.PostList)
            {
                var cats = BlogServer.split_cat_strings(post.Categories);
                if (cats.Contains(tokens[tokens.Length - 1]))
                {
                    el_body.AddParagraphElement(string.Format("Title=\"{0}\"", post.Title));
                    el_body.AddParagraphElement(string.Format("Link=\"{0}\"", post.Link));

                }
            }

            string html = xdoc.ToString();

            WriteResponseString(context, html, 200, ContentType_TextHtml);
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

            el_body.AddH1Element(this.CategoryList.Count.ToString() + " Categories");
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

            WriteResponseString(context, html, 200, ContentType_TextHtml);
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
            WriteResponseString(context, xdoc.ToString(), 200, ContentType_TextHtml);
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
    }
}