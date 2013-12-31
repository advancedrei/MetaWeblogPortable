namespace MetaWeblog.Portable.Server
{
    public class UserBlogInfo
    {
        public string ID;
        public string Url;
        public string Name;

        public UserBlogInfo( string id, string url, string name)
        {
            this.ID = id;
            this.Url = url;
            this.Name = name;
        }

        public XmlRpc.Struct ToStruct()
        {
            var struct_ = new XmlRpc.Struct();
            struct_["blogid"] = new XmlRpc.StringValue(this.ID);
            struct_["url"] = new XmlRpc.StringValue(this.Url);
            struct_["blogName"] = new XmlRpc.StringValue(this.Name);

            return struct_;
        }
    }
}