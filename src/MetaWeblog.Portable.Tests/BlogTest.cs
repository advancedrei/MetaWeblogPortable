using System.Threading.Tasks;
using MetaWeblog.Portable;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MetaWeblogSharpTests
{
    [TestClass]
    public class Test_BlogEngine
    {
        [TestMethod]
        public async Task GetPosts1()
        {

            var con1 = GetBlog1();

            var con2 = new BlogConnectionInfo(
                "http://localhost:14228/test2",
                "http://localhost:14882/test2/metaweblog.axd",
                "test2",
                "admin",
                "admin");


            var client = new Client(con1);

      

            var posts = await client.GetRecentPosts(10000);
            foreach (var p in posts)
            {
                await client.DeletePost(p.PostId);
            }

            posts = await client.GetRecentPosts(10000);
            Assert.AreEqual(0,posts.Count);

            // create and verify a normal post
            string postid = await client.NewPost("P1", "P1Content", null, false, null);
            posts = await client.GetRecentPosts(10000);
            Assert.AreEqual(1,posts.Count);
            Assert.AreEqual(postid, posts[0].PostId);


            // Create another post
            string postid2 = await client.NewPost("P2", "P2Content", null, false, null);
            posts = await client.GetRecentPosts(10000);
            Assert.AreEqual(2, posts.Count);
            Assert.AreEqual(postid2, posts[0].PostId);
            Assert.AreEqual(postid, posts[1].PostId);
            Assert.AreEqual(null, posts[0].PostStatus);


            var firstPost = posts[0];
            string newTitle = firstPost.Title + " Updated";
            await client.EditPost(firstPost.PostId, newTitle , firstPost.Description, null, true);
            var firstPostUpdated = await client.GetPost(firstPost.PostId);
            Assert.AreEqual(newTitle, firstPostUpdated.Title);



        }

        private static BlogConnectionInfo GetBlog1()
        {
            var con1 = new BlogConnectionInfo(
                "http://localhost:14228/test1",
                "http://localhost:14882/test1/metaweblog.axd",
                "test1",
                "admin",
                "admin");
            return con1;
        }

        private static BlogConnectionInfo GetBlog2()
        {
            var con1 = new BlogConnectionInfo(
                "http://localhost:14228/test2",
                "http://localhost:14882/test2/metaweblog.axd",
                "test2",
                "admin",
                "admin");
            return con1;
        }

        //[TestMethod]
        //public void SerializeTests()
        //{
        //    var con1 = new BlogConnectionInfo(
        //        "http://localhost:14228/test1",
        //        "http://localhost:14882/test1/metaweblog.axd",
        //        "test1",
        //        "admin",
        //        "admin");


        //     var client1 = new Client(con1);
        //     var posts = client1.GetRecentPosts(100000).ToArray();

        //     PostInfo.Serialize(posts,@"d:\blog_con_info.xml");

        //     var loaded_posts = PostInfo.Deserialize(@"d:\blog_con_info.xml");


        //     for (int i = 0; i < posts.Length; i++)
        //     {
        //         var original = posts[i];
        //         var loaded = loaded_posts[i];
        //         Assert.AreEqual(original.Description,loaded.Description);
        //         Assert.AreEqual(original.DateCreated, loaded.DateCreated);
        //         Assert.AreEqual(original.PostID, loaded.PostID);
        //         Assert.AreEqual(original.Title, loaded.Title);
                 
        //     }
        //}
    }
}
