using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MetaWeblog.Portable.XmlRpc;

namespace MetaWeblog.Portable
{
    public class Client
    {
        //http://xmlrpc.scripting.com/metaWeblogApi.html

        public string AppKey = "0123456789ABCDEF";
        public BlogConnectionInfo BlogConnectionInfo;

        public Client(BlogConnectionInfo connectionInfo)
        {
            this.BlogConnectionInfo = connectionInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="numposts"></param>
        /// <returns></returns>
        public async Task<List<PostInfo>> GetRecentPosts(int numposts)
        {
            var service = new Service(BlogConnectionInfo.MetaWeblogUrl);

            var method = new MethodCall("metaWeblog.getRecentPosts");
            method.Parameters.Add(BlogConnectionInfo.BlogId);
            method.Parameters.Add(BlogConnectionInfo.Username);
            method.Parameters.Add(BlogConnectionInfo.Password);
            method.Parameters.Add(numposts);

            var response = await service.Execute(method);

            var param = response.Parameters[0];
            var array = (Array)param;

            return (from Struct s in array
                    select new PostInfo
                    {
                        Title = s.Get("title", StringValue.NullString).String,
                        DateCreated = s.Get<DateTimeValue>("dateCreated").Data,
                        Link = s.Get("link", StringValue.NullString).String,
                        PostId = s.Get("postid", StringValue.NullString).String,
                        UserId = s.Get("userid", StringValue.NullString).String,
                        CommentCount = s.Get<IntegerValue>("commentCount", 0).Integer,
                        PostStatus = s.Get("post_status", StringValue.NullString).String,
                        Permalink = s.Get("permaLink", StringValue.NullString).String,
                        Description = s.Get("description", StringValue.NullString).String
                    }).ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="bits"></param>
        /// <returns></returns>
        public async Task<MediaObjectInfo> NewMediaObject(string name, string type, byte[] bits)
        {
            var service = new Service(BlogConnectionInfo.MetaWeblogUrl);

            var inputStruct = new Struct();
            inputStruct["name"] = new StringValue(name);
            inputStruct["type"] = new StringValue(type);
            inputStruct["bits"] = new Base64Data(bits);

            var method = new MethodCall("metaWeblog.newMediaObject");
            method.Parameters.Add(BlogConnectionInfo.BlogId);
            method.Parameters.Add(BlogConnectionInfo.Username);
            method.Parameters.Add(BlogConnectionInfo.Password);
            method.Parameters.Add(inputStruct);

            var response = await service.Execute(method);
            var param = response.Parameters[0];
            var _struct = (Struct)param;

            var mediaobject = new MediaObjectInfo { Url = _struct.Get("url", StringValue.NullString).String };

            return mediaobject;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="postid"></param>
        /// <returns></returns>
        public async Task<PostInfo> GetPost(string postid)
        {
            var service = new Service(this.BlogConnectionInfo.MetaWeblogUrl);

            var method = new MethodCall("metaWeblog.getPost");
            method.Parameters.Add(postid); // notice this is the postid, not the blogid
            method.Parameters.Add(BlogConnectionInfo.Username);
            method.Parameters.Add(BlogConnectionInfo.Password);

            var response = await service.Execute(method);
            var param = response.Parameters[0];
            var _struct = (Struct)param;

            var postinfo = new PostInfo
            {
                PostId = _struct.Get<StringValue>("postid").String,
                Description = _struct.Get<StringValue>("description").String,
                Link = _struct.Get("link", StringValue.NullString).String,
                DateCreated = _struct.Get<DateTimeValue>("dateCreated").Data,
                Permalink = _struct.Get("permaLink", StringValue.NullString).String,
                PostStatus = _struct.Get("post_status", StringValue.NullString).String,
                Title = _struct.Get<StringValue>("title").String,
                UserId = _struct.Get("userid", StringValue.NullString).String
            };
            //item.Categories 
            //item.Tags

            return postinfo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pi"></param>
        /// <param name="categories"></param>
        /// <param name="publish"></param>
        /// <returns></returns>
        public async Task<string> NewPost(PostInfo pi, IList<string> categories, bool publish)
        {
            return await NewPost(pi.Title, pi.Description, categories, publish, pi.DateCreated);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <param name="categories"></param>
        /// <param name="publish"></param>
        /// <param name="dateCreated"></param>
        /// <returns></returns>
        public async Task<string> NewPost(string title, string description, IList<string> categories, bool publish, System.DateTime? dateCreated)
        {
            Array categoriesArray;

            if (categories == null)
            {
                categoriesArray = new Array(0);
            }
            else
            {
                categoriesArray = new Array(categories.Count);
                categoriesArray.AddRange(categories.Select(c => new StringValue(c)));
            }

            var service = new Service(BlogConnectionInfo.MetaWeblogUrl);

            var _struct = new Struct();
            _struct["title"] = new StringValue(title);
            _struct["description"] = new StringValue(description);
            _struct["categories"] = categoriesArray;
            if (dateCreated.HasValue)
            {
                _struct["dateCreated"] = new DateTimeValue(dateCreated.Value);
                _struct["date_created_gmt"] = new DateTimeValue(dateCreated.Value.ToUniversalTime());

            }
            var method = new MethodCall("metaWeblog.newPost");
            method.Parameters.Add(BlogConnectionInfo.BlogId);
            method.Parameters.Add(BlogConnectionInfo.Username);
            method.Parameters.Add(BlogConnectionInfo.Password);
            method.Parameters.Add(_struct);
            method.Parameters.Add(publish);

            var response = await service.Execute(method);
            var param = response.Parameters[0];
            var postid = ((StringValue)param).String;

            return postid;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="postid"></param>
        /// <returns></returns>
        public async Task<bool> DeletePost(string postid)
        {
            var service = new Service(BlogConnectionInfo.MetaWeblogUrl);

            var method = new MethodCall("blogger.deletePost");
            method.Parameters.Add(AppKey);
            method.Parameters.Add(postid);
            method.Parameters.Add(BlogConnectionInfo.Username);
            method.Parameters.Add(BlogConnectionInfo.Password);
            method.Parameters.Add(true);

            var response = await service.Execute(method);
            var param = response.Parameters[0];
            var success = (BooleanValue)param;

            return success.Boolean;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<List<BlogInfo>> GetUsersBlogs()
        {
            var service = new Service(BlogConnectionInfo.MetaWeblogUrl);

            var method = new MethodCall("blogger.getUsersBlogs");
            method.Parameters.Add(AppKey);
            method.Parameters.Add(BlogConnectionInfo.Username);
            method.Parameters.Add(BlogConnectionInfo.Password);

            var response = await service.Execute(method);
            var list = (Array)response.Parameters[0];

            var blogs = new List<BlogInfo>(list.Count);
            blogs.AddRange(from Struct _struct in list
                           select new BlogInfo
                           {
                               BlogId = _struct.Get("blogid", StringValue.NullString).String,
                               Url = _struct.Get("url", StringValue.NullString).String,
                               BlogName = _struct.Get("blogName", StringValue.NullString).String,
                               IsAdmin = _struct.Get<BooleanValue>("isAdmin", false).Boolean,
                               SiteName = _struct.Get("siteName", StringValue.NullString).String,
                               Capabilities = _struct.Get("capabilities", StringValue.NullString).String,
                               XmlRpcEndpoint = _struct.Get("xmlrpc", StringValue.NullString).String
                           });

            return blogs;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="postid"></param>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <param name="categories"></param>
        /// <param name="publish"></param>
        /// <returns></returns>
        public async Task<bool> EditPost(string postid, string title, string description, IList<string> categories, bool publish)
        {
            // Create an array to hold any categories
            var _categories = new Array(categories == null ? 0 : categories.Count);
            if (categories != null)
            {
                _categories.AddRange(categories.Select(c => new StringValue(c)));
            }

            var service = new Service(BlogConnectionInfo.MetaWeblogUrl);
            var struct_ = new Struct();
            struct_["title"] = new StringValue(title);
            struct_["description"] = new StringValue(description);
            struct_["categories"] = _categories;

            var method = new MethodCall("metaWeblog.editPost");
            method.Parameters.Add(postid);
            method.Parameters.Add(BlogConnectionInfo.Username);
            method.Parameters.Add(BlogConnectionInfo.Password);
            method.Parameters.Add(struct_);
            method.Parameters.Add(publish);

            var response = await service.Execute(method);
            var param = response.Parameters[0];
            var success = (BooleanValue)param;

            return success.Boolean;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<List<CategoryInfo>> GetCategories()
        {
            var service = new Service(BlogConnectionInfo.MetaWeblogUrl);

            var method = new MethodCall("metaWeblog.getCategories");
            method.Parameters.Add(BlogConnectionInfo.BlogId);
            method.Parameters.Add(BlogConnectionInfo.Username);
            method.Parameters.Add(BlogConnectionInfo.Password);

            var response = await service.Execute(method);

            var param = response.Parameters[0];
            var array = (Array)param;

            return (from Struct _struct in array
                    select new CategoryInfo
                    {
                        Title = _struct.Get("title", StringValue.NullString).String,
                        Description = _struct.Get("description", StringValue.NullString).String,
                        HtmlUrl = _struct.Get("htmlUrl", StringValue.NullString).String,
                        RssUrl = _struct.Get("rssUrl", StringValue.NullString).String,
                        CategoryId = _struct.Get("categoryid", StringValue.NullString).String
                    }).ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<UserInfo> GetUserInfo()
        {
            var service = new Service(BlogConnectionInfo.MetaWeblogUrl);

            var method = new MethodCall("blogger.getUserInfo");
            method.Parameters.Add(AppKey);
            method.Parameters.Add(BlogConnectionInfo.Username);
            method.Parameters.Add(BlogConnectionInfo.Password);

            var response = await service.Execute(method);
            var param = response.Parameters[0];
            var struct_ = (Struct)param;
            var item = new UserInfo
            {
                UserId = struct_.Get("userid", StringValue.NullString).String,
                Nickname = struct_.Get("nickname", StringValue.NullString).String,
                FirstName = struct_.Get("firstname", StringValue.NullString).String,
                LastName = struct_.Get("lastname", StringValue.NullString).String,
                Url = struct_.Get("url", StringValue.NullString).String
            };

            return item;
        }
    }
}