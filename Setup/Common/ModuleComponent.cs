using System;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace ZF.Setup
{
    [Serializable]
    public class ModuleComponent
    {
        [Serializable]
        public struct DependencyModule
        {
#if ODIN_INSPECTOR
           [ValueDropdown("GetConstIds", ExpandAllMenuItems = true)]
#endif      
            public int moduleId;
            public ModuleTypeComponent typeComponent;
            
#if ODIN_INSPECTOR
            private ValueDropdownList<int> GetConstIds()
            {
                var result = new ValueDropdownList<int>();
                foreach (var (name,id) in ModuleConstId.ConstIds)
                {
                    result.Add(name, id);
                }
                return result;
            }
#endif
        }
            
        public bool dependOnModule;
#if ODIN_INSPECTOR
        [ShowIf("dependOnModule")]
#endif
        public List<DependencyModule> dependencyModules;
        
        public bool dependOnURL;
#if ODIN_INSPECTOR
        [ShowIf("dependOnURL")]
#endif
        public List<string> dependencyURL;

        public bool dependOnRegistry;
#if ODIN_INSPECTOR
        [ShowIf("dependOnRegistry")]
#endif
        public List<string> dependencyRegistries;
        
        public bool dependOnScopes;
#if ODIN_INSPECTOR
        [ShowIf("dependOnScopes")]
#endif
        public List<ScopedRegistry> scopedRegistries;
            
#if ODIN_INSPECTOR
        [FolderPath]
        [GUIColor(0.3f, 0.8f, 0.8f, 1f)]
#endif
        public List<string> path;

        public bool hasSingleFolder;
#if ODIN_INSPECTOR
        [ShowIf("hasSingleFolder")]
        [FolderPath]
        [GUIColor("#FF0000")]
#endif
        public List<string> singleFolders;

        public bool symbolsOnRemove;
#if ODIN_INSPECTOR
        [ShowIf("symbolsOnRemove")]
#endif
        public List<string> deleteSymbols;
    }
}