using System.Collections.Generic;
using Microsoft.Isam.Esent.Collections.Generic;

namespace MetaWeblog.Server
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
            Dictionary[m.Url] = m;
            Dictionary.Flush();
            return m;
        }

        public MediaObjectRecord? TryGetMediaObjectByUrl(string url)
        {
            if (this.Dictionary.ContainsKey(url))
            {
                return this.Dictionary[url];
            }
            return null;
        }
    }

    public class CategoryList : ObjectDic<CategoryRecord>
    {
        public CategoryList()
            : base("CategoryDB")
        {
        }

        public CategoryRecord Add(string blogid, string description)
        {
            var now = System.DateTime.Now;

            var m = new CategoryRecord();
            m.Id = now.Ticks.ToString();
            m.BlogId = blogid;
            m.Description = description.Trim();
            m.Name = m.Description;
            m.DateCreated = now;
            m.HtmlUrl = "/category/" + description;
            m.RssUrl = m.HtmlUrl + "?format=rss";

            this.Dictionary[m.Id] = m;
            this.Dictionary.Flush();

            return m;
        }

        public CategoryRecord? TryGetCategoryById(string id)
        {
            if (this.Dictionary.ContainsKey(id))
            {
                return this.Dictionary[id];
            }
            return null;
        }
    }
}