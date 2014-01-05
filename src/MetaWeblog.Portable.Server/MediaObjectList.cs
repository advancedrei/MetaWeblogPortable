using System.Collections.Generic;
using Microsoft.Isam.Esent.Collections.Generic;

namespace MetaWeblog.Portable.Server
{
    public class MediaObjectList : ObjectDic<MediaObjectRecord>
    {
        public MediaObjectList()
            : base("MediaObjectsDB")
        {
        }

        public MediaObjectRecord StoreNewObject(string blogid, string userid, string name, string type, string bits)
        {
            var m = new MediaObjectRecord();
            m.OriginalName = name.Trim();

            var now = System.DateTime.Now;
            m.Name = name.Replace("/", "-").Replace("\\", "-");
            m.Filename = System.IO.Path.GetFileName(name);
            m.Id = now.Ticks.ToString();
            m.DateCreated = now;
            m.Type = type.Trim();
            m.Base64Bits = bits;
            m.BlogId = blogid;
            m.UserId = userid;
            m.Url = "/media/" + m.Filename + "?id=" + m.Id;
            pdic[m.Url] = m;
            pdic.Flush();
            return m;
        }

        public MediaObjectRecord? TryGetMediaObjectByUrl(string url)
        {
            if (this.pdic.ContainsKey(url))
            {
                return this.pdic[url];
            }
            return null;
        }
    }
}