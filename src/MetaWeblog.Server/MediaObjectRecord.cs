using System;
using MP = MetaWeblog.Portable;

namespace MetaWeblog.Server
{
    [Serializable]
    public struct MediaObjectRecord
    {
        public string Name;
        public string Id;
        public string OriginalName;
        public string Filename;
        public string Url;
        public string Type;
        public string Base64Bits;
        public DateTime DateCreated;
        public string UserId;
        public string BlogId;
    }
}

namespace MetaWeblog.Server
{
    [Serializable]
    public struct CategoryRecord
    {
        public DateTime DateCreated;
        public string Id;
        public string BlogId;
        public string Description;
        public string Name;
        public string HtmlUrl;
        public string RssUrl;

        public MP.XmlRpc.Struct ToStruct()
        {
            var struct_ = new MP.XmlRpc.Struct();
            struct_["description"] = new MP.XmlRpc.StringValue(this.Description);
            struct_["htmlUrl"] = new MP.XmlRpc.StringValue(this.HtmlUrl);
            struct_["rssUrl"] = new MP.XmlRpc.StringValue(this.RssUrl);
            return struct_;
        }
    }
}