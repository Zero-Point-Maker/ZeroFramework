using System;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace ZF.Setup
{
    [Serializable]
    public class Module
    {
#if ODIN_INSPECTOR
        [ValueDropdown("GetConstIds", ExpandAllMenuItems = true)]
#endif
        public int id;
#if ODIN_INSPECTOR
        [GUIColor("orange")]
#endif
        public string name;
        public string version;
        public string description;
        public string footnote;
        public Dictionary<ModuleTypeComponent, ModuleComponent> components = new();
        
#if ODIN_INSPECTOR
        private ValueDropdownList<int> GetConstIds()
        {
            var result = new ValueDropdownList<int>();
            foreach (var (moduleName, moduleId) in ModuleConstId.ConstIds)
            {
                result.Add(moduleName, moduleId);
            }

            return result;
        }
#endif
    }
}