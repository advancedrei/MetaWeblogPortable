namespace MetaWeblog.Portable.Server
{
    public class BlogServerOptions
    {
        public int Port = 14228;
        public bool CreateDefaultPosts = false;
        public string StyleSheet = @"
html { font-family: ""Arial""; }";

        public string MetaWeblogUrl = "/metaweblogapi";
        public string ArchiveUrl = "/archive";
        public string PostUrl = "/post";
    }
}