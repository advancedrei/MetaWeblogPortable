using System;
using System.Collections.Generic;
using Microsoft.Isam.Esent.Collections.Generic;

namespace MetaWeblog.Server
{
    public class ObjectDic<T> : IEnumerable<T>
    {
        public readonly PersistentDictionary<string, T> Dictionary;

        public ObjectDic(string name)
        {
            string folder = System.IO.Path.Combine(BlogServer.GetOutputFolderRootPath(),name);
            BlogServer.CreateFolderSafe(folder);
            this.Dictionary = new PersistentDictionary<string, T>(folder);
        }

        ~ObjectDic()
        {
            if (this.Dictionary != null)
            {
                this.Dictionary.Dispose();
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.Dictionary.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count
        {
            get { return this.Dictionary.Count; }
        }

        public void Delete(string key)
        {
            Console.WriteLine("Deleting {0}", key);
            this.Dictionary.Remove(key);
            this.Dictionary.Flush();
        }
    }
}