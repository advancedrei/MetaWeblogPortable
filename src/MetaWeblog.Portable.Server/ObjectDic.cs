using System.Collections.Generic;
using Microsoft.Isam.Esent.Collections.Generic;

namespace MetaWeblog.Portable.Server
{
    public class ObjectDic<T> : IEnumerable<T>
    {
        protected readonly PersistentDictionary<string, T> pdic;

        public ObjectDic(string name)
        {
            string folder = System.IO.Path.Combine(BlogServer.GetOutputFolderRootPath(),name);
            BlogServer.CreateFolderSafe(folder);
            this.pdic = new PersistentDictionary<string, T>(folder);
        }

        ~ObjectDic()
        {
            if (this.pdic != null)
            {
                this.pdic.Dispose();
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.pdic.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}