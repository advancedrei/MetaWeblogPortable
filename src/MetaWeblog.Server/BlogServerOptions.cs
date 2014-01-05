namespace MetaWeblog.Server
{
    public class BlogServerOptions
    {
        public int Port = 14228;
        public bool CreateSampleContent = false;
        public string StyleSheet = @"
html { font-family: ""Arial""; }";

        public string MetaWeblogUrl = "/metaweblogapi";
        public string ArchiveUrl = "/archive";
        public string PostUrl = "/post";
        public bool OverwriteLog;
        public string OutputFolder = System.IO.Path.Combine( System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments) , typeof(BlogServer).Name);
    }
}