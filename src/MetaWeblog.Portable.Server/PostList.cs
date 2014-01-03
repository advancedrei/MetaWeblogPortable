using System.Collections.Generic;

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

        public void Sort()
        {
            var unpublished_dt = System.DateTime.Now;
            this.items.Sort(
                (x, y) =>
                    y.DateCreated.GetValueOrDefault(unpublished_dt).CompareTo(x.DateCreated.GetValueOrDefault(unpublished_dt)));
        }


    }
}