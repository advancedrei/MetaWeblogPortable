namespace MetaWeblog.Portable.Server
{
    public class BlogServerOptions
    {
        public int Port = 14228;
        public bool CreateDefaultPosts = false;
        public string StyleSheet = @"
html { font-family: ""Arial""; }";
    }
}