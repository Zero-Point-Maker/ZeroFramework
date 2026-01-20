using System;
using System.Collections.Generic;

namespace ZF.Setup
{
    [Serializable]
    public struct ScopedRegistry
    {
        public string name;
        public string url;
        public List<string> scopes;
    }
}