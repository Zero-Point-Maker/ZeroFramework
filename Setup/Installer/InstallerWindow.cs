using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace ZF.Setup.Installer
{
    public class InstallerWindow : EditorWindow
    {
        #region 常量与字段

        private Vector2 _scrollPosition; // 滚动条位置
        private Vector2 _toolsScrollPosition; // Tools列滚动条位置
        private GUIContent _refreshBtn; // 刷新按钮样式
        private GUIStyle _refreshBtnStyle; // 刷新按钮样式
        private readonly Dictionary<int, bool> _showModules = new(); // 模块id->是否显示
        private readonly Dictionary<ModuleType, bool> _showTypes = new(); // 模块类型->是否显示

        #endregion

        #region 窗口管理

        [MenuItem("ZF/Setup/Installer", false, 2)]
        public static void ShowWindow()
        {
            GetWindow<InstallerWindow>("ZeroFramework Installer");
        }

        private void OnEnable()
        {
            Installer.Initialize();

            // 只初始化尚未添加的模块类型，保留已有类型的折叠状态
            foreach (ModuleType type in Enum.GetValues(typeof(ModuleType)))
            {
                _showTypes.TryAdd(type, true);
            }
        }

        private void OnDisable()
        {
            Installer.Clear();
            // 移除对折叠状态字典的清空，这样折叠状态在窗口刷新时不会被重置
            // _showModules.Clear();
            // _showTypes.Clear();
            GC.Collect();
        }

        private string GetModuleTypeName(ModuleType type)
        {
            switch (type)
            {
                case ModuleType.Core:
                    return "核心";
                case ModuleType.Extension:
                    return "扩展";
                case ModuleType.Primary:
                    return "一级模块";
                case ModuleType.Secondary:
                    return "二级模块";
                case ModuleType.Tertiary:
                    return "三级模块";
                case ModuleType.ThirdParty:
                    return "第三方插件";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        #endregion

        #region GUI绘制

        private void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
            {
                DrawTitle();
                // 创建水平布局，分为主内容区和右侧tools列
                using (new EditorGUILayout.HorizontalScope())
                {
                    // 主内容区
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.ExpandWidth(true)))
                    {
                        DrawModules();
                    }

                    // 右侧Tools列
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.ExpandHeight(true),
                               GUILayout.Width(300)))
                    {
                        DrawTools();
                    }
                }
            }
        }

        private void DrawTitle()
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("ZeroFramework Installer", new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 20,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                }, GUILayout.Height(60));
                GUILayout.FlexibleSpace();
            }
        }

        private void DrawModules()
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Modules", new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 });

                if (GUILayout.Button("全部安装", GUILayout.Height(23), GUILayout.Width(115)))
                {
                    if (EditorUtility.DisplayDialog("确认", "确定要安装所有未安装的模块吗？", "确定", "取消"))
                    {
                        _ = Installer.InstallAll(
                            (current, total) =>
                            {
                                EditorUtility.DisplayProgressBar("正在安装模块", $"正在安装: {current}/{total}",
                                    (float)current / total);
                            },
                            () =>
                            {
                                EditorUtility.ClearProgressBar();
                                AssetDatabase.Refresh();
                            });
                    }
                }

                GUILayout.Space(10);

                _refreshBtn ??= new GUIContent(EditorGUIUtility.IconContent("Refresh"))
                {
                    tooltip = "Refresh modules and tools."
                };

                _refreshBtnStyle ??= new GUIStyle("Command")
                {
                    fontSize = 16,
                    alignment = TextAnchor.MiddleCenter,
                    imagePosition = ImagePosition.ImageAbove,
                    fontStyle = FontStyle.Bold
                };

                if (GUILayout.Button(_refreshBtn, _refreshBtnStyle))
                {
                    Installer.Clear();
                    Installer.Initialize();
                }
            }

            GUILayout.Space(10);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));

            if (Installer.Config != null)
            {
                foreach (ModuleType type in Enum.GetValues(typeof(ModuleType)))
                {
                    bool showType = _showTypes.GetValueOrDefault(type);
                    var list = Installer.Config.modules.GetValueOrDefault(type);

                    using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        _showTypes[type] = EditorGUILayout.Foldout(showType,
                            $"{GetModuleTypeName(type),-15}\t({list.Count})\t{Installer.GetModuleTypeVersion(type)}",
                            new GUIStyle(EditorStyles.foldout) { fontSize = 15 });

                        int originalIndent = EditorGUI.indentLevel;
                        EditorGUI.indentLevel++;

                        if (showType)
                        {
                            DrawSubModulesByType(type);
                        }

                        EditorGUI.indentLevel = originalIndent;
                    }

                    GUILayout.Space(10);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSubModulesByType(ModuleType type)
        {
            var list = Installer.Config.modules.GetValueOrDefault(type);

            foreach (var module in list)
            {
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    bool showModule = _showModules.GetValueOrDefault(module.id);
                    _showModules[module.id] =
                        EditorGUILayout.Foldout(showModule, $"{module.name}",
                            new GUIStyle(EditorStyles.foldout)
                                { normal = { textColor = new Color(1, .5f, 0) } });
                    if (showModule)
                    {
                        EditorGUILayout.LabelField($"版本：{module.version}");
                        EditorGUILayout.LabelField($"简介：{module.description}");
                        if (!string.IsNullOrEmpty(module.footnote))
                        {
                            EditorGUILayout.LabelField($"备注：{module.footnote}");
                        }

                        EditorGUILayout.LabelField("组件");
                        DrawSubModuleComponents(type, module);
                    }
                }

                GUILayout.Space(5);
            }
        }

        private void DrawSubModuleComponents(ModuleType type, Module module)
        {
            foreach (var (typeComponent, component) in module.components)
            {
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"-{typeComponent}",
                            new GUIStyle(EditorStyles.label)
                                { normal = { textColor = Color.yellow } }, GUILayout.Width(120));

                        bool hasComponent = Installer.AddedComponent.GetValueOrDefault((module.name, typeComponent));
                        if (GUILayout.Button(hasComponent ? "卸载" : "安装", GUILayout.Width(60)))
                        {
                            if (hasComponent)
                            {
                                Installer.Uninstall(type, module.name, typeComponent);
                                AssetDatabase.Refresh();
                            }
                            else
                            {
                                _ = Installer.Install(type, module.name, typeComponent, component,
                                    AssetDatabase.Refresh);
                            }
                        }
                    }

                    EditorGUI.indentLevel++;

                    if (component.dependOnModule) DrawSubDependencyModule(component.dependencyModules);
                    if (component.dependOnURL) DrawSubDependencyURL(component.dependencyURL);
                    if (component.dependOnRegistry) DrawSubRegistries(component.dependencyRegistries);
                    if (component.dependOnScopedRegistries) DrawSubScopedRegistries(component.scopedRegistries);
                    DrawSubPath(component.path);

                    EditorGUI.indentLevel--;
                }
            }
        }

        private void DrawSubDependencyModule(List<ModuleComponent.DependencyModule> dependencyModules)
        {
            if (dependencyModules is not { Count: > 0 }) return;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("依赖模块", GUILayout.Width(120));
                foreach (var module in dependencyModules)
                {
                    var dependencyModule = Installer.Modules.GetValueOrDefault(module.moduleId);
                    if (dependencyModule == null)
                    {
                        Debug.LogError($"依赖模块不存在：{module.moduleId},请检查BuilderConfig配置是否正确");
                        continue;
                    }

                    bool hasModule = Installer.AddedComponent.GetValueOrDefault((dependencyModule.name, module.typeComponent));
                    GUILayout.Box(
                        $"{dependencyModule.name}\n[{module.typeComponent.ToString().Substring(0, 3).ToUpper()}]",
                        new GUIStyle(GUI.skin.box)
                            { normal = { textColor = hasModule ? Color.green : Color.red } });
                }
            }
        }

        private void DrawSubDependencyURL(List<string> dependencyUrls)
        {
            if (dependencyUrls is not { Count: > 0 }) return;
            EditorGUILayout.LabelField("依赖URL", GUILayout.Width(120));
            EditorGUI.indentLevel++;
            foreach (var url in dependencyUrls)
            {
                bool hasUrl = Installer.AddedUrl.GetValueOrDefault(url);
                EditorGUILayout.LabelField(url,
                    new GUIStyle(EditorStyles.label)
                        { normal = { textColor = hasUrl ? Color.green : Color.red } });
            }

            EditorGUI.indentLevel--;
        }

        private void DrawSubRegistries(List<string> dependencyRegistries)
        {
            if (dependencyRegistries is not { Count: > 0 }) return;
            EditorGUILayout.LabelField("依赖Unity包", GUILayout.Width(120));
            EditorGUI.indentLevel++;
            foreach (var registry in dependencyRegistries)
            {
                bool hasRegistry = Installer.AddedRegistry.GetValueOrDefault(registry);
                EditorGUILayout.LabelField(registry,
                    new GUIStyle(EditorStyles.label)
                        { normal = { textColor = hasRegistry ? Color.green : Color.red } });
            }

            EditorGUI.indentLevel--;
        }

        private void DrawSubScopedRegistries(List<ScopedRegistry> scopedRegistries)
        {
            if (scopedRegistries is not { Count: > 0 }) return;
            EditorGUILayout.LabelField("作用域注册表", GUILayout.Width(120));
            EditorGUI.indentLevel++;
            foreach (var registry in scopedRegistries)
            {
                bool hasRegistry = Installer.AddedScopedRegistry.GetValueOrDefault(registry.name);
                var message = $"name\t{registry.name}\n" + $"url\t{registry.url}\n";
                foreach (var scope in registry.scopes)
                {
                    bool last = scope == registry.scopes.Last();
                    message += $"scope\t{scope}";
                    if (!last) message += "\n";
                }

                EditorGUILayout.HelpBox(message, hasRegistry ? MessageType.Info : MessageType.Warning);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawSubPath(List<string> paths)
        {
            if (paths is not { Count: > 0 }) return;
            EditorGUILayout.LabelField("模块路径", GUILayout.Width(120));
            EditorGUI.indentLevel++;
            foreach (var path in paths)
            {
                EditorGUILayout.LabelField(path);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawTools()
        {
            GUILayout.Label("Tools", new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 });
            GUILayout.Space(10);

            _toolsScrollPosition = EditorGUILayout.BeginScrollView(_toolsScrollPosition, GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));

            if (Installer.Config == null || Installer.Config.tools == null || Installer.Config.tools.Count == 0)
            {
                GUILayout.Label("No tools available");
            }
            else
            {
                foreach (var tool in Installer.Config.tools)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        GUILayout.Label(tool.name,
                            new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.cyan } });

                        // 允许用户复制URL
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            // 使用TextField显示URL，允许用户选中复制
                            EditorGUILayout.TextField(tool.url, EditorStyles.label, GUILayout.ExpandWidth(true),
                                GUILayout.Height(20));

                            // 添加复制按钮
                            if (GUILayout.Button("复制", GUILayout.Width(50)))
                            {
                                EditorGUIUtility.systemCopyBuffer = tool.url;
                                Debug.Log("URL已复制到剪贴板: " + tool.url);
                            }
                        }
                    }

                    GUILayout.Space(5);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        #endregion
    }
}