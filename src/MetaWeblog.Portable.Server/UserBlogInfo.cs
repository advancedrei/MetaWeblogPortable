using MP = MetaWeblog.Portable;

namespace MetaWeblog.Server
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

        public MP.XmlRpc.Struct ToStruct()
        {
            var struct_ = new MP.XmlRpc.Struct();
            struct_["blogid"] = new MP.XmlRpc.StringValue(this.ID);
            struct_["url"] = new MP.XmlRpc.StringValue(this.Url);
            struct_["blogName"] = new MP.XmlRpc.StringValue(this.Name);

            return struct_;
        }
    }
}