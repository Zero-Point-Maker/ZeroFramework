using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;
using System.Threading.Tasks;

namespace ZF.Setup.Installer
{
    public static class Installer
    {
        #region 结构定义
        private struct ModuleTypeInfo
        {
            public long version;
            public int moduleCount;
            public List<ModuleIndex> indexes;
            public Dictionary<string, byte[]> data;
        }
        #endregion

        #region 常量与字段
        private const string TEMP_PATH = "Assets/ZF/Setup/temp";    // 临时文件目录
        private const string FILE_CATALOG = "ModuleCatalog";    // 模块目录
        private static SerializableConfig _config; // 配置
        private static Dictionary<int, Module> _modules;   // 模块id->模块
        private static Dictionary<(string, ModuleTypeComponent), ModuleComponent> _components; // (模块名,组件类型)->组件
        private static Dictionary<int, ModuleType> _moduleTypes;   // 模块id->模块类型
        private static Dictionary<ModuleType, ModuleTypeInfo> _infos;  // 模块类型->模块信息
        private static Dictionary<int, bool> _addedModule; // 模块id->是否已添加
        private static Dictionary<string, bool> _addedUrl; // 模块url->是否已添加
        private static Dictionary<string, bool> _addedRegistry; // unity包->是否已添加
        private static Dictionary<string, bool> _addedScopedRegistry;  // 模块作用域->是否已添加
        private static Dictionary<(string, ModuleTypeComponent), bool> _addedComponent;    // (模块名,组件类型)->是否已添加
        #endregion

        #region 属性
        public static SerializableConfig Config => _config;
        public static Dictionary<int, Module> Modules => _modules;
        public static Dictionary<int, bool> AddedModule => _addedModule;
        public static Dictionary<string, bool> AddedUrl => _addedUrl;
        public static Dictionary<string, bool> AddedRegistry => _addedRegistry;
        public static Dictionary<string, bool> AddedScopedRegistry => _addedScopedRegistry;
        public static Dictionary<(string, ModuleTypeComponent), bool> AddedComponent => _addedComponent;
        #endregion

        #region 初始化与清理
        public static void Initialize()
        {
            _modules = new Dictionary<int, Module>();
            _components = new Dictionary<(string, ModuleTypeComponent), ModuleComponent>();
            _moduleTypes = new Dictionary<int, ModuleType>();
            _infos = new Dictionary<ModuleType, ModuleTypeInfo>();
            
            LoadModules();
            CheckAdded();
        }

        public static void Clear()
        {
            _config = null;
            _modules = null;
            _infos = null;
            _addedModule = null;
            _addedUrl = null;
            _addedRegistry = null;
            _addedScopedRegistry = null;
            _addedComponent = null;
        }

        public static long GetModuleTypeVersion(ModuleType type)
        {
            return _infos.TryGetValue(type, out var info) ? info.version : 0;
        }
        #endregion

        #region 模块管理
        private static void LoadModules()
        {
            /*// 读取目录文件
            string binPath = Path.Combine(Application.dataPath, "ZF/Setup/bin/ModuleCatalog.bin");
            if (File.Exists(binPath))
            {
                using FileStream fileStream = File.OpenRead(binPath);
                BinaryFormatter formatter = new BinaryFormatter();
                _config = (SerializableConfig)formatter.Deserialize(fileStream);
            }*/
            
            // 读取目录文件
            TextAsset catalogAsset = Resources.Load<TextAsset>(FILE_CATALOG);
            if (catalogAsset != null)
            {
                using MemoryStream memoryStream = new MemoryStream(catalogAsset.bytes);
                BinaryFormatter formatter = new BinaryFormatter();
                _config = (SerializableConfig)formatter.Deserialize(memoryStream);
            }
            
            if (_config == null) 
            {
                LogError("配置文件加载失败");
                return;
            }

            // 读取配置文件
            foreach (var (type, list) in _config.modules)
            {
                //string binFilePath = Path.Combine(Application.dataPath, $"ZF/Setup/bin/{type}.bin");
                /*if (!File.Exists(binFilePath))
                {
                    LogError($"未找到 {type} 类型的 bin 文件：{binFilePath}");
                    continue;
                }*/

                TextAsset moduleAsset = Resources.Load<TextAsset>($"{type}");
                if (moduleAsset == null)
                {
                    LogWarning($"未找到 {type} 类型的资源文件");
                    continue;
                }
                
                try
                {
                    /*using FileStream fileStream = File.OpenRead(binFilePath);
                    using BinaryReader reader = new BinaryReader(fileStream);*/
                    
                    using MemoryStream memoryStream = new MemoryStream(moduleAsset.bytes);
                    using BinaryReader reader = new BinaryReader(memoryStream);                    
                    
                    ModuleTypeInfo info = default;
                    info.version = reader.ReadInt64();
                    info.moduleCount = reader.ReadInt32();
                    info.indexes = new List<ModuleIndex>();

                    for (int i = 0; i < info.moduleCount; i++)
                    {
                        // 读取名称长度
                        int nameLength = reader.ReadInt32();
                        // 读取名称
                        byte[] nameBytes = reader.ReadBytes(nameLength);
                        string moduleName = System.Text.Encoding.UTF8.GetString(nameBytes);
                        // 读取偏移量
                        long offset = reader.ReadInt64();
                        // 读取大小
                        int size = reader.ReadInt32();
                        
                        // 添加到索引列表
                        info.indexes.Add(new ModuleIndex
                        {
                            name = moduleName,
                            offset = offset,
                            size = size
                        });
                    }

                    info.data = new Dictionary<string, byte[]>();
                    foreach (ModuleIndex index in info.indexes)
                    {
                        // 定位到数据位置
                        //fileStream.Seek(index.offset, SeekOrigin.Begin);
                        memoryStream.Seek(index.offset, SeekOrigin.Begin);
                        // 读取数据
                        byte[] data = reader.ReadBytes(index.size);
                        info.data.Add(index.name, data);
                        LogInfo($"读取到模块 {index.name}，数据大小：{data.Length} 字节");
                    }
                    
                    _infos.Add(type, info);
                }
                catch (Exception ex)
                {
                    LogError($"读取 {type} 类型的 bin 文件时发生错误：{ex.Message}");
                    LogError(ex.StackTrace);
                }

                foreach (var module in list)
                {
                    _modules.Add(module.id, module);
                    _moduleTypes.Add(module.id, type);
                    foreach (var (typeComponent,component) in module.components)
                    {
                        _components.Add((module.name, typeComponent), component);
                    }
                }
            }
        }

        private static void CheckAdded()
        {
            _addedModule = new Dictionary<int, bool>();
            _addedUrl = new Dictionary<string, bool>();
            _addedRegistry = new Dictionary<string, bool>();
            _addedScopedRegistry = new Dictionary<string, bool>();
            _addedComponent = new Dictionary<(string, ModuleTypeComponent), bool>();
            
            foreach (var moduleList in _config.modules.Values)
            {
                foreach (var module in moduleList)
                {
                    foreach (var (componentType,component) in module.components)
                    {
                        if (component.dependencyURL is { Count: > 0 })
                        {
                            foreach (var url in component.dependencyURL)
                            {
                                _addedUrl[url] = InstallerHelper.CheckGitPackageAdded(url);
                            }
                        }

                        if (component.dependencyRegistries is { Count: > 0 })
                        {
                            foreach (var registry in component.dependencyRegistries)
                            {
                                _addedRegistry[registry] = InstallerHelper.CheckRegistryAdded(registry);
                            }
                        }

                        if (component.scopedRegistries is { Count: > 0 })
                        {
                            foreach (var registry in component.scopedRegistries)
                            {
                                _addedScopedRegistry[registry.name] = 
                                    InstallerHelper.CheckScopedRegistryAdded(registry.name, registry.url);
                            }
                        }

                        
                        if (component.path is { Count: > 0 })
                        {
                            bool allPathsExist = true;
                            foreach (var path in component.path)
                            {
                                // 修复路径解析问题，正确处理../开头的相对路径
                                string fullPath;
                                string projectPath = Path.GetDirectoryName(Application.dataPath);
                                
                                if (Path.IsPathRooted(path))
                                {
                                    fullPath = path;
                                }
                                else if (path.StartsWith("../"))
                                {
                                    // ../开头的路径，相对于项目根目录解析
                                    fullPath = Path.GetFullPath(Path.Combine(projectPath, path));
                                }
                                else if (path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                                {
                                    // 如果路径以Assets/开头，使用项目根目录结合路径
                                    fullPath = Path.Combine(projectPath, path);
                                }
                                else if (path.StartsWith("./"))
                                {
                                    // ./开头的路径，相对于Assets目录解析
                                    fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, path));
                                }
                                else
                                {
                                    // 其他相对路径直接结合Application.dataPath
                                    fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, path));
                                }
                                if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
                                {
                                    allPathsExist = false;
                                    break;
                                }
                            }

                            _addedComponent.Add((module.name, componentType), allPathsExist);
                            _addedModule.TryAdd(module.id, allPathsExist);
                        }
                    }
                }
            }
        }
        #endregion

        #region 安装与卸载
        
        public static async Task Install(ModuleType type, string moduleName, ModuleTypeComponent typeComponent,
            ModuleComponent component, Action onComplete = null)
        {
            LogInfo($"开始安装模块：{moduleName} - 类型：{type} - 组件：{typeComponent}");
            if (_infos.TryGetValue(type, out var info))
            {
                if(component.dependOnModule) await InstallDependencyModules(component);
                if(component.dependOnURL) await InstallDependencyURL(component);
                if(component.dependOnRegistry) await InstallRegistries(component);
                if(component.dependOnScopedRegistries) await InstallScopes(component);
                await InstallPreComponent(type, moduleName, typeComponent);

                // 安装组件
                var prefix = $"{moduleName}_{typeComponent}_";
                foreach (var kv in info.data)
                {
                    if (kv.Key.StartsWith(prefix))
                    {
                        var path = kv.Key.Replace(prefix, "");
                        DeserializeBytesToPath(kv.Value, path);
                    }
                }

                CheckAdded();
                onComplete?.Invoke();
            }
        }

        // 安装依赖模块
        private static async Task InstallDependencyModules(ModuleComponent component)
        {
            if (component.dependencyModules is { Count: > 0 })
            {
                foreach (var dependencyModule in component.dependencyModules)
                {
                    var module = _modules[dependencyModule.moduleId];
                    bool hasComponent =
                        _addedComponent.GetValueOrDefault((module.name, dependencyModule.typeComponent));
                    if (hasComponent) continue;
                    
                    LogWarning($"\t缺少依赖模块：{module.name}/{dependencyModule.typeComponent}");
                    if (!module.components.TryGetValue(dependencyModule.typeComponent,
                            out var dependencyComponent)) continue;
                    
                    var moduleType = _moduleTypes.GetValueOrDefault(module.id);
                    await Install(moduleType, module.name, dependencyModule.typeComponent,
                        dependencyComponent);
                }
            }
        }

        // 安装依赖URL
        private static async Task InstallDependencyURL(ModuleComponent component)
        {
            if (component.dependencyURL is { Count: > 0 })
            {
                foreach (var url in from url in component.dependencyURL let dependencyUrlAdded = _addedUrl.GetValueOrDefault(url) where !dependencyUrlAdded select url)
                {
                    LogInfo($"\t添加依赖的URL：{url}");
                    await InstallerHelper.AddGitPackage(url);
                }
            }
        }

        // 安装依赖包
        private static async Task InstallRegistries(ModuleComponent component)
        {
            if (component.dependencyRegistries is { Count: > 0 })
            {
                foreach (var registry in from registry in component.dependencyRegistries let addedRegistry = _addedRegistry.GetValueOrDefault(registry) where !addedRegistry select registry)
                {
                    LogInfo($"\t添加依赖的注册表：{registry}");
                    await InstallerHelper.AddRegistry(registry);
                }
            }
        }

        // 安装作用域注册表
        private static async Task InstallScopes(ModuleComponent component)
        {
            if (component.scopedRegistries is { Count: > 0 })
            {
                foreach (var scopedRegistry in from scopedRegistry in component.scopedRegistries let addedScopedRegistry = _addedScopedRegistry.GetValueOrDefault(scopedRegistry.name) where !addedScopedRegistry select scopedRegistry)
                {
                    LogInfo($"\t添加作用域注册表：{scopedRegistry.name}");
                    await InstallerHelper.AddScopedRegistry(scopedRegistry.name, scopedRegistry.url,
                        scopedRegistry.scopes);
                }
            }
        }

        // 安装前置组件
        private static async Task InstallPreComponent(ModuleType type, string moduleName,
            ModuleTypeComponent typeComponent)
        {
            foreach (ModuleTypeComponent addTypeComponent in Enum.GetValues(typeof(ModuleTypeComponent)))
            {
                if (addTypeComponent < typeComponent)
                {
                    bool hasComponent = _addedComponent.GetValueOrDefault((moduleName, addTypeComponent));
                    if (!hasComponent)
                    {
                        if (_components.TryGetValue((moduleName, addTypeComponent), out var addComponent))
                        {
                            await Install(type, moduleName, addTypeComponent, addComponent);
                        }
                    }
                }
                else break;
            }
        }

        public static async Task InstallAll(Action<int, int> onProgress = null, Action onComplete = null)
        {
            if (_config == null || _config.modules == null)
            {
                onComplete?.Invoke();
                return;
            }

            int totalModules = 0;
            int installedModules = 0;

            foreach (var moduleList in _config.modules.Values)
            {
                foreach (var module in moduleList)
                {
                    foreach (var (typeComponent, component) in module.components)
                    {
                        totalModules++;
                    }
                }
            }

            foreach (var (type, moduleList) in _config.modules)
            {
                foreach (var module in moduleList)
                {
                    foreach (var (typeComponent, component) in module.components)
                    {
                        bool isAdded = _addedComponent.GetValueOrDefault((module.name, typeComponent));
                        if (!isAdded)
                        {
                            await Install(type, module.name, typeComponent, component);
                            installedModules++;
                            onProgress?.Invoke(installedModules, totalModules);
                        }
                    }
                }
            }
            
            onComplete?.Invoke();
        }

        public static void Uninstall(ModuleType type, string moduleName, ModuleTypeComponent typeComponent)
        {
            if (_infos.TryGetValue(type, out var info))
            {
                foreach (ModuleTypeComponent removeTypeComponent in Enum.GetValues(typeof(ModuleTypeComponent)))
                {
                    if (removeTypeComponent > typeComponent)
                    {
                        bool hasComponent = _addedComponent.GetValueOrDefault((moduleName, removeTypeComponent));
                        if (hasComponent)
                        {
                            Uninstall(type, moduleName, removeTypeComponent);
                        }
                    }
                }

                var prefix = $"{moduleName}_{typeComponent}_";
                foreach (var kv in info.data)
                {
                    if (kv.Key.StartsWith(prefix))
                    {
                        var path = kv.Key.Replace(prefix, "");
                        string fullPath = ResolvePath(path);
                        
                        try
                        {
                            if (File.Exists(fullPath))
                            {
                                // 如果是文件，直接删除
                                File.Delete(fullPath);
                                LogInfo($"已删除文件: {fullPath}");
                            }
                            else if (Directory.Exists(fullPath))
                            {
                                // 如果是文件夹，删除文件夹及其所有内容
                                Directory.Delete(fullPath, true);
                                LogInfo($"已删除文件夹及其内容: {fullPath}");
                                DeleteFolderMeta(fullPath);
                            }
                            
                            DeleteEmptyParentDirectories(fullPath);
                        }
                        catch (Exception ex)
                        {
                            LogError($"卸载时删除 {fullPath} 失败: {ex.Message}");
                        }
                    }
                }
                
                //删除宏定义
                var component = _components.GetValueOrDefault((moduleName, typeComponent));
                if (component is { symbolsOnRemove: true, deleteSymbols: { Count: > 0 } })
                {
                    foreach (var symbol in component.deleteSymbols)
                    {
#if UNITY_EDITOR
                        // 处理Standalone平台
                        var standaloneSymbols = UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(UnityEditor.BuildTargetGroup.Standalone);
                        var standaloneSymbolList = standaloneSymbols.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
                        standaloneSymbolList.Remove(symbol);
                        UnityEditor.PlayerSettings.SetScriptingDefineSymbolsForGroup(UnityEditor.BuildTargetGroup.Standalone, string.Join(";", standaloneSymbolList));
                        
                        // 处理Android平台
                        var androidSymbols = UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(UnityEditor.BuildTargetGroup.Android);
                        var androidSymbolList = androidSymbols.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
                        androidSymbolList.Remove(symbol);
                        UnityEditor.PlayerSettings.SetScriptingDefineSymbolsForGroup(UnityEditor.BuildTargetGroup.Android, string.Join(";", androidSymbolList));
                        
                        // 处理iOS平台
                        var iosSymbols = UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(UnityEditor.BuildTargetGroup.iOS);
                        var iosSymbolList = iosSymbols.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
                        iosSymbolList.Remove(symbol);
                        UnityEditor.PlayerSettings.SetScriptingDefineSymbolsForGroup(UnityEditor.BuildTargetGroup.iOS, string.Join(";", iosSymbolList));
#endif
                        // 后面这里要为unity6考虑修改
                    }
                }

                CheckAdded();
            }
        }
        #endregion

        #region 文件操作

        private static void DeleteFolderMeta(string folderPath)
        {
            // Unity中还要删除.meta文件，否则文件夹会被重新创建出来
            var metaPath = folderPath + ".meta";
            if (File.Exists(metaPath))
            {
                File.Delete(metaPath);
                LogInfo($"已删除.meta文件: {metaPath}");
            }
        }

        /// <summary>
        /// 递归删除所有空的父目录
        /// </summary>
        /// <param name="filePath">文件路径</param>
        private static void DeleteEmptyParentDirectories(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                return;
            }
            
            // 检查目录是否为空
            if (Directory.GetFileSystemEntries(directory).Length == 0)
            {
                Directory.Delete(directory, true);
                LogInfo($"已删除空文件夹: {directory}");
                DeleteFolderMeta(directory);
                // 递归检查父目录
                DeleteEmptyParentDirectories(directory);
            }
        }

        /// <summary>
        /// 将字节数组反序列为文件或目录
        /// </summary>
        /// <param name="data">字节数组</param>
        /// <param name="targetPath">目标路径</param>
        private static void DeserializeBytesToPath(byte[] data, string targetPath)
        {
            // 确保临时目录存在
            EnsureDirectoryExists(TEMP_PATH);

            // 生成唯一的临时文件路径（使用 Guid 避免冲突）
            string tempZipFilePath = Path.Combine(TEMP_PATH, $"temp_{Guid.NewGuid()}.zip");

            // 将字节数组写入临时文件
            File.WriteAllBytes(tempZipFilePath, data);

            // 解析目标路径，处理../开头的相对路径
            string resolvedTargetPath = ResolvePath(targetPath);
            string resolvedTargetDirectory = Path.GetDirectoryName(resolvedTargetPath);
            if (!string.IsNullOrEmpty(resolvedTargetDirectory))
            {
                EnsureDirectoryExists(resolvedTargetDirectory);
            }

            string tempExtractDir = Path.Combine(TEMP_PATH, $"temp_{Guid.NewGuid()}");
            try
            {
                // 1. 创建临时解压目录
                EnsureDirectoryExists(tempExtractDir);

                // 2. 解压ZIP文件到临时目录
                ZipFile.ExtractToDirectory(tempZipFilePath, tempExtractDir);

                // 3. 获取解压后的内容
                string[] extractedItems = Directory.GetFileSystemEntries(tempExtractDir);

                // 4. 处理解压后的内容
                foreach (string extractedItem in extractedItems)
                {
                    // 目标路径
                    string itemName = Path.GetFileName(extractedItem);
                    string finalTargetPath = Path.Combine(resolvedTargetPath, itemName);

                    // 确保目标目录存在
                    EnsureDirectoryExists(Path.GetDirectoryName(finalTargetPath));

                    if (Directory.Exists(extractedItem))
                    {
                        // 如果是目录，先删除旧目录（如果存在），再移动
                        if (Directory.Exists(finalTargetPath))
                        {
                            Directory.Delete(finalTargetPath, true);
                        }

                        Directory.Move(extractedItem, finalTargetPath);
                    }
                    else if (File.Exists(extractedItem))
                    {
                        // 如果是文件，先删除旧文件（如果存在），再移动
                        if (File.Exists(finalTargetPath))
                        {
                            File.Delete(finalTargetPath);
                        }

                        File.Move(extractedItem, finalTargetPath);
                    }
                }

                // 5. 检查并处理特殊情况：如果目标路径是目录且只有一个文件，则提取该文件到同级目录并删除原目录
                if (Directory.Exists(resolvedTargetPath))
                {
                    string[] itemsInTarget = Directory.GetFileSystemEntries(resolvedTargetPath);
                    if (itemsInTarget.Length == 1)
                    {
                        var singleFile = itemsInTarget[0];
                        if (File.Exists(singleFile))
                        {
                            // 提取文件到目标路径的同级目录
                            string fileName = Path.GetFileName(singleFile);
                            string parentDir = Path.GetDirectoryName(resolvedTargetPath);
                            if (parentDir != null)
                            {
                                string newFilePath = Path.Combine(parentDir, fileName);

                                // 处理文件名与文件夹名相同的情况
                                // 1. 先将文件移动到临时位置
                                string tempFilePath = Path.Combine(parentDir, $"temp_{Guid.NewGuid()}_{fileName}");
                                File.Move(singleFile, tempFilePath);

                                // 2. 删除原目录
                                Directory.Delete(resolvedTargetPath, true);

                                // 3. 删除已存在的同名文件（如果有）
                                if (File.Exists(newFilePath))
                                {
                                    File.Delete(newFilePath);
                                }

                                // 4. 将临时文件重命名为目标文件名
                                File.Move(tempFilePath, newFilePath);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"解压失败: {ex.Message}");
            }
            finally
            {
                // 清理临时ZIP文件
                if (File.Exists(tempZipFilePath))
                {
                    File.Delete(tempZipFilePath);
                }

                // 清理临时解压目录
                if (Directory.Exists(tempExtractDir))
                {
                    Directory.Delete(tempExtractDir, true);
                }

                // 清理临时目录
                if (Directory.Exists(TEMP_PATH))
                {
                    Directory.Delete(TEMP_PATH, true);
                }
            }
        }
        
        /// <summary>
        /// 解析路径：根据路径格式选择不同的解析基础
        /// </summary>
        /// <param name="path">要解析的路径</param>
        /// <returns>解析后的路径</returns>
        private static string ResolvePath(string path)
        {
            if (Path.IsPathRooted(path)) return path;
            
            string projectPath = Path.GetDirectoryName(Application.dataPath);
            
            // 特殊处理：../开头的路径相对于项目根目录解析
            if (path.StartsWith("../"))
            {
                if (projectPath != null) return Path.GetFullPath(Path.Combine(projectPath, path));
            }
            // 处理Assets/开头的路径
            else if (path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                if (projectPath != null) return Path.Combine(projectPath, path);
            }
            // 处理./开头或其他相对路径
            else
            {
                return Path.GetFullPath(Path.Combine(Application.dataPath, path));
            }
            return string.Empty;
        }

        /// <summary>
        /// 确保目录存在
        /// 如果不存在则创建
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        private static void EnsureDirectoryExists(string directoryPath)
        {
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }
        #endregion

        #region 日志
        private static void LogInfo(string message)
        {
            Debug.Log($"[Installer] {message}");
        }

        private static void LogWarning(string message)
        {
            Debug.LogWarning($"[Installer] {message}");
        }

        private static void LogError(string message)
        {
            Debug.LogError($"[Installer] {message}");
        }
        #endregion
    }
}