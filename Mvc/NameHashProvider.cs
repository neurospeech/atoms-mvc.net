using System;
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
    public class NameHashProvider
    {
        MD5 md5;
        private NameHashProvider()
        {
            md5 = MD5.Create();
        }

        public string GetHashCode(string text)
        {

            byte[] buffer = Encoding.Default.GetBytes(text);
            buffer = md5.ComputeHash(buffer);
            return string.Join("\\", buffer.Take(5).Select(x => x.ToString("x")));
        }


        public static NameHashProvider Instance = new NameHashProvider();

    }
}
