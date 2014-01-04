using System;
using System.Collections.Generic;
using System.Linq;

namespace MetaWeblog.Portable.Server
{
    public class PostList: IEnumerable<PostInfo>
    {
        private readonly List<PostInfo> items = new List<PostInfo>();

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

        public PostInfo Add(DateTime? created, string title, string desc, IList<string> cats, bool publish)
        {
            var p = new PostInfo();
            p.DateCreated = created != null ? created.Value : System.DateTime.Now;

            p.Title = title;
            p.Description = desc;
            p.PostId = this.items.Count.ToString();
            p.Link = this.TitleToPostId(p.Title);
            p.Permalink = p.Link;
            p.PostStatus = "published";

            if (cats != null)
            {
                p.Categories.AddRange(cats);
            }


            this.Add(p);

            return p;
        }

        private string TitleToPostId(string t)
        {
            t = StringUtils.CollapseWhiteSpace(t);
            var sb = new System.Text.StringBuilder(t.Length);
            foreach (char c in t)
            {
                if (Char.IsWhiteSpace(c))
                {
                    sb.Append("-");
                }
                else if (c == '?' || c == '.' || c == '!' || c == '!' || c == '$' || c == '@')
                {
                    // don't include these
                }
                else if (c == '&')
                {
                    sb.Append("-and-");
                }
                else if (c == '<')
                {
                    sb.Append("-lt-");
                }
                else if (c == '>')
                {
                    sb.Append("-gt-");
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private void Sort()
        {
            var unpublished_dt = System.DateTime.Now;
            this.items.Sort(
                (x, y) =>
                    y.DateCreated.GetValueOrDefault(unpublished_dt).CompareTo(x.DateCreated.GetValueOrDefault(unpublished_dt)));
        }


        public PostInfo TryGetPostById(string id)
        {
            foreach (var p in this.items)
            {
                if (p.PostId == id)
                {
                    return p;
                }
            }
            return null;
        }

        public PostInfo TryGetPostByLink(string link)
        {
            foreach (var post in this.items)
            {
                if (post.Link == link)
                {
                    return post;
                }
            }
            return null;
        }

        public int Count
        {
            get
            {
                return this.items.Count;
            }
        }

        public HashSet<string> GetCategories()
        {
            var hs = new HashSet<string>();
            foreach (var post in this)
            {
                foreach (var cat in post.Categories)
                {
                    hs.Add(cat);
                }
            }
            return hs;
        }

        public Dictionary<string,List<PostInfo>> GetPostsByCategory()
        {
            var dic = new Dictionary<string,List<PostInfo>>();
            foreach (var post in this)
            {
                foreach (var cat in post.Categories)
                {
                    if (!dic.ContainsKey(cat))
                    {
                        dic[cat]= new List<PostInfo>();
                    }
                    var list = dic[cat];
                    list.Add(post);
                }
            }
            return dic;
        }

    }
}