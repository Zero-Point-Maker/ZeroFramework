using System.Collections.Generic;

namespace ZF.Setup
{
    [System.Serializable]
    public class SerializableConfig
    {
        public Dictionary<ModuleType, List<Module>> modules = new();
        public List<BuilderTool> tools = new();
    }
}