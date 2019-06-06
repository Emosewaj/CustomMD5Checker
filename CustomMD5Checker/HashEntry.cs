using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomMD5Checker
{

    class HashEntry
    {
        public string Hash { get; set; }
        public string Path { get; set; }

        public HashEntry(string path, string hash)
        {
            this.Path = path;
            this.Hash = hash;
        }
    }
}
