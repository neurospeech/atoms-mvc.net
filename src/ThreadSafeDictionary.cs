using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech.Atoms
{
    public class ThreadSafeDictionary<TKey,TValue> : Dictionary<TKey,TValue>
    {

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory) {

            lock (this) {

                TValue v;
                if (this.TryGetValue(key, out v))
                    return v;
                v = factory(key);
                this[key] = v;
                return v;
            }

        }

    }
}
