using System;
using System.Linq;
using MP = MetaWeblog.Portable;

namespace MetaWeblog.Server
{
    [Serializable]
    public struct PostInfoRecord
    {
        public string Title;
        public string Link;
        public DateTime? DateCreated;
        public string PostId;
        public string UserId;
        public int CommentCount;
        public string PostStatus;
        public string Permalink;
        public string Description;
        public string Categories;

        public PostInfoRecord(MP.PostInfo p)
        {
            this.Title = p.Title;
            this.Link = p.Link;
            this.DateCreated = p.DateCreated;
            this.PostId = p.PostId;
            this.UserId = p.UserId;
            this.CommentCount = p.CommentCount;
            this.PostStatus = p.PostStatus;
            this.Permalink = p.Permalink;
            this.Description = p.Description;
            this.Categories = BlogServer.join_cat_strings(p.Categories.Select(s=>s.Trim()));
        }

        internal string[] SplitCategories()
        {
            return BlogServer.split_cat_strings(this.Categories);
        }

        public MP.PostInfo ToPostInfo()
        {
            var p = new MP.PostInfo();
            p.Title = this.Title;
            p.Link = this.Link;
            p.DateCreated = this.DateCreated;
            p.PostId = this.PostId;
            p.UserId = this.UserId;
            p.CommentCount = this.CommentCount;
            p.PostStatus = this.PostStatus;
            p.Permalink = this.Permalink;
            p.Description = this.Description;
            var cats = this.SplitCategories();
            foreach (string cat in cats)
            {
                p.Categories.Add(cat.Trim());
            }

            return p;
        }
    }
}