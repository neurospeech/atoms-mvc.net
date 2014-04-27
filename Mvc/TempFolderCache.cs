using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Caching;

namespace NeuroSpeech.Atoms.Mvc
{

    public class TempCacheContent 
    {
        public DateTime Expiry { get; set; }
        public object Data { get; set; }
    }

    public class TempFolderCache : OutputCacheProvider
    {

        public static TempFolderCache Instance;
        private DictionaryCache Cache;
        private DirectoryInfo Folder;

        public TempFolderCache()
        {
            Instance = this;
            Cache = new DictionaryCache();
            Folder = new DirectoryInfo(Path.GetTempPath() + "\\asp-mvc-cache");

        }

        public override object Add(string key, object entry, DateTime utcExpiry)
        {
            return Cache.GetOrAdd(key, utcExpiry, k =>
            {
                FileInfo f = GetFile(key, true);

                WriteFileData(entry, utcExpiry, f);

                return entry;
            });
        }

        private void WriteFileData(object entry, DateTime utcExpiry, FileInfo f)
        {
            using (FileStream fs = f.OpenWrite())
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fs, utcExpiry);
                bf.Serialize(fs, entry);
            }
        }

        private FileInfo GetFile(string key,bool create = false)
        {
            FileInfo f = new FileInfo(Folder.FullName + "\\" + ToSafePath(key));
            var d = f.Directory;
            if (create && (!d.Exists))
                d.Create();
            return f;
        }

        private string ToSafePath(string key)
        {
            return new string(key.Select(x => (x == '\\' || char.IsLetterOrDigit(x) || x == '-') ? x : '_').ToArray());
        }

        public override object Get(string key)
        {
            return Cache.GetOrAdd(key, DateTime.MaxValue, k =>
            {
                FileInfo f = GetFile(key, false);
                if (!f.Exists)
                    return null;
                DateTime expires;
                object entry;
                using (FileStream fs = f.OpenRead())
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    expires = (DateTime)bf.Deserialize(fs);
                    entry = bf.Deserialize(fs);
                }
                if (expires < DateTime.UtcNow)
                {
                    Remove(key);
                    return null;
                }

                return entry;
            });
        }

        public override void Remove(string key)
        {
            FileInfo f = GetFile(key, false);
            if (!f.Exists)
                return;
            f.Delete();
            Cache.Remove(key);
        }

        public override void Set(string key, object entry, DateTime utcExpiry)
        {
            Cache.Remove(key);
            Cache.GetOrAdd(key, utcExpiry, k =>
            {
                FileInfo f = GetFile(key, true);
                WriteFileData(entry, utcExpiry, f);
                return entry;
            });
        }
    }


    public class DictionaryCacheItem {
        public DateTime UtcExpiry { get; set; }
        public string Key { get; set; }
        public object Value { get; set; }
        public LinkedListNode<DictionaryCacheItem> Node { get; set; }
    }

    public class DictionaryCache
    {
        public Dictionary<string, DictionaryCacheItem> Cache = new Dictionary<string, DictionaryCacheItem>();
        public LinkedList<DictionaryCacheItem> LRU = new LinkedList<DictionaryCacheItem>();
        public int MaxObjects { get; set; }

        public DictionaryCache()
        {
            MaxObjects = 100;
        }

        public object GetOrAdd(string key, DateTime utcExpiry, Func<string,object> f, Action<object> found = null) {
            DictionaryCacheItem item;
            lock (this)
            {
                if (Cache.TryGetValue(key, out item))
                {
                    if (item.UtcExpiry >= utcExpiry)
                    {
                        LRU.Remove(item.Node);
                        LRU.AddLast(item.Node);
                        if (found != null)
                            found(item.Value);
                        return item.Value;
                    }
                }
                object nv = f(key);
                if (nv == null)
                    return null;
                if (LRU.Count >= MaxObjects) {
                    string k = LRU.First.Value.Key;
                    LRU.RemoveFirst();
                    Cache.Remove(k);
                }
                item = new DictionaryCacheItem { Value=nv, Key = key, UtcExpiry = utcExpiry };
                Cache[key] = item;
                item.Node = LRU.AddLast(item);
                if (found != null)
                {
                    found(nv);
                }
            } 
            return item.Value;
        }



        public void Remove(string key)
        {
            lock (this) {
                DictionaryCacheItem c;
                if (Cache.TryGetValue(key, out c)) {
                    LRU.Remove(c.Node);
                    Cache.Remove(key);
                }
            }
        }
    }

}
