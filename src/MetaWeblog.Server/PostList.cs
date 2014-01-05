using System;
using System.Collections.Generic;
using System.Linq;
using MP = MetaWeblog.Portable;

namespace MetaWeblog.Server
{
    public class PostList: ObjectDic<PostInfoRecord>
    {
        public PostList():
            base("PostsDB")
        {
        }

        public void Add(MP.PostInfo p)
        {
            this.Dictionary[p.PostId] = new PostInfoRecord(p);
            this.Dictionary.Flush();
        }

        public MP.PostInfo Add(DateTime? created, string title, string desc, IList<string> cats, bool publish)
        {
            var p = new MP.PostInfo();
            p.DateCreated = created != null ? created.Value : System.DateTime.Now;

            p.Title = title;
            p.Description = desc;
            p.PostId = System.DateTime.Now.Ticks.ToString();
            p.Link = "/post/" + this.TitleToPostId(p.Title);
            p.Permalink = p.Link;
            p.PostStatus = "published";

            if (cats != null)
            {
                if (cats.Any(c => c.Trim().Length < 1))
                {
                    throw new System.ArgumentException("Category cannot be empoty or whitespace");
                }
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

        public PostInfoRecord? TryGetPostById(string id)
        {
            if (Dictionary.ContainsKey(id))
            {
                return Dictionary[id];
            }
            return null;
        }

        public PostInfoRecord? TryGetPostByLink(string link)
        {
            var pair = this.Dictionary.FirstOrDefault(i => i.Value.Link == link);
            if (pair.Value.PostId != null)
            {
                return pair.Value;
            }
            return null;
        }

        public HashSet<string> GetCategories()
        {
            var hs = new HashSet<string>();
            foreach (var post in this)
            {
                var cats = post.SplitCategories();
                foreach (var cat in cats)
                {
                    
                    hs.Add(cat);
                }
            }
            return hs;
        }

        public Dictionary<string,List<PostInfoRecord>> GetPostsByCategory()
        {
            var dic = new Dictionary<string, List<PostInfoRecord>>();
            foreach (var post in this)
            {
                var cats = post.SplitCategories();
                foreach (var cat in cats)
                {
                    if (!dic.ContainsKey(cat))
                    {
                        dic[cat] = new List<PostInfoRecord>();
                    }
                    var list = dic[cat];
                    list.Add(post);
                }
            }
            return dic;
        }

        public void Delete(PostInfoRecord p)
        {
            this.Delete(p.PostId);
        }
    }
}