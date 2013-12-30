using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MP = MetaWeblog.Portable;
using SXL = MetaWeblog.Portable;

namespace MetaWeblog.Portable.Samples
{
 /*
 Save an XML file like below to your Mydocs folder and call it blog.xml
  
<?xml version="1.0" encoding="utf-8"?>
<blogconnectioninfo>
  <blogurl>http://yourblog.com</blogurl>
  <blogid>1329839</blogid>
  <metaweblog_url>http://something</metaweblog_url>
  <username>user</username>
  <password>password</password>
</blogconnectioninfo>
  
 */
    class Program
    {
        static void Main(string[] args)
        {
            GetListOfPosts();
        }

        static async void GetListOfPosts()
        {
            var coninfo = GetDefaultConnectionInfo();
            var client = new MP.Client(coninfo);
            var posts = client.GetRecentPosts(10);
            posts.Wait();
            foreach (var post in posts.Result)
            {
                Console.WriteLine("{0} {1}", post.PostId, post.Title);
            }
        }

        public static  MP.BlogConnectionInfo GetDefaultConnectionInfo()
        {
            string mydocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string blog_xml = System.IO.Path.Combine(mydocs, "blog.xml");
            var coninfo = Helpers.LoadBlogConnectionInfo(blog_xml);
            return coninfo;
        }


    }

    public static class Helpers
    {
        public static MP.BlogConnectionInfo LoadBlogConnectionInfo(string filename)
        {
            var doc = System.Xml.Linq.XDocument.Load(filename);
            var root = doc.Root;

            string blogurl = root.GetElementString("blogurl");
            string blogId = root.GetElementString("blogid");
            string metaWeblogUrl = root.GetElementString("metaweblog_url");
            string username = root.GetElementString("username");
            string password = root.GetElementString("password");

            var coninfo = new BlogConnectionInfo(blogurl, metaWeblogUrl, blogId, username, password);

            return coninfo;
        }

        public static void SaveBlogConnectionInfo(MP.BlogConnectionInfo coninfo, string filename)
        {
            var doc = new System.Xml.Linq.XDocument();
            var p = new System.Xml.Linq.XElement("blogconnectioninfo");
            doc.Add(p);
            p.Add(new System.Xml.Linq.XElement("blogurl", coninfo.BlogUrl));
            p.Add(new System.Xml.Linq.XElement("blogid", coninfo.BlogId));
            p.Add(new System.Xml.Linq.XElement("metaweblog_url", coninfo.MetaWeblogUrl));
            p.Add(new System.Xml.Linq.XElement("username", coninfo.Username));
            p.Add(new System.Xml.Linq.XElement("password", coninfo.Password));
            doc.Save(filename);
        }

        public static void SerializeToXmlFile(MP.PostInfo[] posts, string filename)
        {
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(MP.PostInfo[]));
            var textWriter = new System.IO.StreamWriter(filename);
            serializer.Serialize(textWriter, posts);
            textWriter.Close();
        }

        public static MP.PostInfo[] DeserializePostsFromXmlFile(string filename)
        {
            var fp = System.IO.File.OpenText(filename);
            var posts_serializer = new System.Xml.Serialization.XmlSerializer(typeof(MP.PostInfo[]));
            var loaded_posts = (MP.PostInfo[])posts_serializer.Deserialize(fp);
            fp.Close();
            return loaded_posts;
        }

    }
}
