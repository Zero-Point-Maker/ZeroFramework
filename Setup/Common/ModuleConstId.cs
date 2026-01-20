using System.Collections;
using UnityEngine;

namespace ZF.Setup
{
    public static class ModuleConstId
    {
        public static readonly (string, int)[] ConstIds =
        {
            // 0-9 核心模块
            ("Runtime/Core", 1),
            // 10-99 拓展模块
            ("Runtime/Extension", 11),
            ("Extension/Atlas", 12),
            //("Extension/Build", 13),
            ("Extension/Hierarchy Inspector", 14),
            ("Extension/ToolbarExtender", 15),
            ("Extension/YKToolkit", 16),
            ("Launcher", 17),
            // 100-199 可选模块(一级)
            ("Runtime/Primary/Architecture", 101),
            ("Runtime/Primary/Event", 102),
            ("Runtime/Primary/Fsm", 103),
            ("Runtime/Primary/ObjectPool", 104),
            ("Runtime/Primary/Timer", 105),
            ("Runtime/Primary/UpdateDriver", 106),
            ("Runtime/Primary/WAB", 107),
            // 200-299 可选模块(二级)
            ("Runtime/Secondary/Debuger", 201),
            ("Runtime/Secondary/Procedure", 202),
            ("Runtime/Secondary/Resource", 203),
            ("Runtime/Secondary/Singleton", 204),
            // 300-399 可选模块(三级)
            ("Runtime/Tertiary/Audio", 301),
            ("Runtime/Tertiary/Config", 302),
            ("Runtime/Tertiary/GameObjectPool", 303),
            ("Runtime/Tertiary/Localization", 304),
            ("Runtime/Tertiary/Scene", 305),
            ("Runtime/Tertiary/UI", 306),
            // 400-999 第三方模块
            ("ThirdParty/DoTweenPro", 401),
            ("ThirdParty/LubanUnity", 402),
            ("ThirdParty/SirenixOdin", 403),
        };
    }
}