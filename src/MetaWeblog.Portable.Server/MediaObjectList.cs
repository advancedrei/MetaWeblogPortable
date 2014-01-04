using System.Collections.Generic;
using Microsoft.Isam.Esent.Collections.Generic;

namespace MetaWeblog.Portable.Server
{
    public class MediaObjectList : IEnumerable<MediaObjectRecord>
    {
        private readonly PersistentDictionary<string, MediaObjectRecord> pdic;

        public MediaObjectList()
        {
            string mydocs = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            string folder = System.IO.Path.Combine(mydocs, typeof(BlogServer).Name + "/" + "MediaObjectsDB");
            if (!System.IO.Directory.Exists(folder))
            {
                System.IO.Directory.CreateDirectory(folder);
            }
            this.pdic = new PersistentDictionary<string, MediaObjectRecord>(folder);               
        }

        public IEnumerator<MediaObjectRecord> GetEnumerator()
        {
            return this.pdic.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
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