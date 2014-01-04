using System;

namespace MetaWeblog.Portable.Server
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