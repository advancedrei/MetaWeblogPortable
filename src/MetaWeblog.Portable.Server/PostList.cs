using System;
using System.Collections.Generic;
using System.Linq;

namespace MetaWeblog.Portable.Server
{
    public class PostList: IEnumerable<PostInfo>
    {
        public readonly List<PostInfo> items = new List<PostInfo>();

        public PostList()
        {
            
        }

        public IEnumerator<PostInfo> GetEnumerator()
        {
            return this.items.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(PostInfo p)
        {
            this.items.Add(p);
            this.Sort();
        }

        public PostInfo Add( DateTime? created, string title, string desc, bool publish)
        {
            var p = new PostInfo();
            p.DateCreated = created != null ? created.Value : System.DateTime.Now;

            p.Title = title;
            p.Description = desc;
            p.PostId = this.items.Count.ToString();
            p.Link = this.TitleToPostId(p.Title);
            p.Permalink = p.Link;
            p.PostStatus = "published";

            this.items.Add(p);

            return p;
        }

        private string TitleToPostId(string t)
        {
            string safe_id = t.Trim();
            safe_id = safe_id.Replace(" ", "-");
            safe_id = safe_id.Replace("\t", "-");
            safe_id = safe_id.Replace("\r", "-");
            safe_id = safe_id.Replace("\n", "-");
            safe_id = safe_id.Replace("&", "-and-");
            safe_id = safe_id.Replace("<", "-lt-");
            safe_id = safe_id.Replace(">", "-gt-");
            safe_id = safe_id.Replace("?", "");
            safe_id = safe_id.Replace(".", "");
            safe_id = safe_id.Replace("!", "");
            safe_id = safe_id.Replace("$", "");
            safe_id = safe_id.Replace("@", "");
            safe_id = safe_id.Replace("@", "");
            string link = "/post/" + safe_id;
            return link;
        }

        public void Sort()
        {
            var unpublished_dt = System.DateTime.Now;
            this.items.Sort(
                (x, y) =>
                    y.DateCreated.GetValueOrDefault(unpublished_dt).CompareTo(x.DateCreated.GetValueOrDefault(unpublished_dt)));
        }



    }
}