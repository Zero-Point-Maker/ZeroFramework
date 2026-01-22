using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace ZF.Setup.Installer
{
    public static class InstallerHelper
    {
        private static string ManifestPath => Path.Combine(Application.dataPath, "../Packages/manifest.json");
        
        public static bool CheckGitPackageAdded(string gitUrl)
        {
            // 同步获取所有包列表
            ListRequest request = Client.List(true); // true = 包含依赖项
        
            // 轮询等待结果
            while (!request.IsCompleted) {}
        
            if (request.Status == StatusCode.Success)
            {
                foreach (var package in request.Result)
                {
                    // 检查包的来源是否为 Git URL
                    if (package.source == PackageSource.Git && 
                        package.packageId.Contains(gitUrl))
                    {
                        return true;
                    }
                }
            }
        
            return false;
        }

        public static IEnumerator AddGitPackage(string gitUrl)
        {
            if (string.IsNullOrEmpty(gitUrl) ||
                (!gitUrl.StartsWith("https://") && !gitUrl.StartsWith("git@") && !gitUrl.StartsWith("git+")))
            {
                Debug.LogError("无效的 Git URL 格式");
                yield break;
            }
            
            yield return AddPackageCoroutine(gitUrl);
        }

        public static bool CheckRegistryAdded(string name)
        {
            if (!File.Exists(ManifestPath))
                return false;
            string content = File.ReadAllText(ManifestPath);
            // 检查manifest.json中是否存在指定名称的注册表
            return content.Contains(name);
        }

        public static IEnumerator AddRegistry(string name)
        {
            yield return AddPackageCoroutine(name);
        }

        public static bool CheckScopedRegistryAdded(string name, string url)
        {
            if (!File.Exists(ManifestPath))
                return false;
            string content = File.ReadAllText(ManifestPath);
            // 简单字符串匹配：查找包含名称或 URL 的 Registry 条目
            return content.Contains(name) && content.Contains(url);
        }

        public static IEnumerator AddScopedRegistry(string name, string url, List<string> scopes)
        {
            if (!File.Exists(ManifestPath))
            {
                Debug.LogError("未找到 Packages/manifest.json 文件");
                yield break;
            }

            // 检查是否已存在
            if (CheckScopedRegistryAdded(name, url))
            {
                Debug.LogWarning($"ScopedRegistry 已存在：{name}");
                yield break;
            }

            string content = File.ReadAllText(ManifestPath);
            string updatedContent = "";

            // 1. 生成新 Registry 的 JSON 字符串
            List<string> quotedScopes = new List<string>();
            foreach (string scope in scopes)
            {
                quotedScopes.Add($"\"{scope}\"");
            }

            string scopeArray = "[" + string.Join(",", quotedScopes) + "]";
            string newRegistryStr = $"\n    {{\"name\":\"{name}\",\"url\":\"{url}\",\"scopes\":{scopeArray}}}";

            // 2. 查找 scopedRegistries 位置
            int registriesIndex = content.IndexOf("\"scopedRegistries\"", StringComparison.Ordinal);

            if (registriesIndex == -1)
            {
                // 不存在 scopedRegistries 字段，添加到 dependencies 之后
                int dependenciesEnd = content.IndexOf("}",
                    content.IndexOf("\"dependencies\"", StringComparison.Ordinal), StringComparison.Ordinal);
                if (dependenciesEnd == -1)
                {
                    Debug.LogError("无法找到 dependencies 字段");
                    yield break;
                }

                // 构建完整的 scopedRegistries 字段
                string registriesSection = $",\n  \"scopedRegistries\": [{newRegistryStr.Trim()}\n  ]";
                updatedContent = content.Insert(dependenciesEnd + 1, registriesSection);
            }
            else
            {
                // 已存在 scopedRegistries 字段，添加到数组中
                int arrayEnd = FindMatchingBracket(content,
                    content.IndexOf("[", registriesIndex, StringComparison.Ordinal));
                if (arrayEnd == -1)
                {
                    Debug.LogError("无法解析 scopedRegistries 数组");
                    yield break;
                }

                // 检查数组是否为空
                int arrayStart = content.IndexOf("[", registriesIndex, StringComparison.Ordinal);
                string arrayContent = content.Substring(arrayStart + 1, arrayEnd - arrayStart - 1).Trim();

                if (string.IsNullOrEmpty(arrayContent))
                {
                    // 空数组，直接添加
                    updatedContent = content.Replace("[]", $"[{newRegistryStr.Trim()}\n  ]");
                }
                else
                {
                    // 非空数组，添加到末尾
                    updatedContent = content.Insert(arrayEnd, $",{newRegistryStr}");
                }
            }

            // 3. 保存文件
            File.WriteAllText(ManifestPath, updatedContent);
            Debug.Log($"成功添加 ScopedRegistry：{name}");

            foreach (var scope in scopes)
            {
                yield return AddPackageCoroutine(scope);
            }

            Client.Resolve();
        }

        #region 辅助方法

        private static IEnumerator AddPackageCoroutine(string address)
        {
            // 异步添加包
            AddRequest request = Client.Add(address);
            
            float progress = 0f;
            float simulatedProgressIncrement = 0.01f;
            
            while (!request.IsCompleted)
            {
                progress = Mathf.Min(progress + simulatedProgressIncrement, 0.99f);
                EditorUtility.DisplayProgressBar("添加包", $"正在添加: {address}", progress);
                yield return null; // 暂停一帧，等待下一帧继续执行
            }
            
            EditorUtility.ClearProgressBar();
            
            if (request.Status == StatusCode.Success)
            {
                Debug.Log($"Package added: {request.Result.packageId}");
            }
            else if (request.Status >= StatusCode.Failure)
            {
                string errorMsg = $"Failed to add package {address}: {request.Error.message}";
                Debug.LogError(errorMsg);
                throw new Exception(errorMsg);
            }
        }

        /*private static async Task AddPackage(string address)
        {
            // 异步添加包
            AddRequest request = Client.Add(address);
            var tcs = new TaskCompletionSource<bool>();
            
            float progress = 0f;
            float simulatedProgressIncrement = 0.01f;
            EditorApplication.update += CheckProgress;
            await tcs.Task;
            
            
            void CheckProgress()
            {
                if (request.IsCompleted)
                {
                    EditorUtility.ClearProgressBar();
                    EditorApplication.update -= CheckProgress;
                    if (request.Status == StatusCode.Success)
                    {
                        Debug.Log($"Package added: {request.Result.packageId}");
                        tcs.SetResult(true);
                    }
                    else if (request.Status >= StatusCode.Failure)
                    {
                        tcs.SetException(new System.Exception(request.Error.message));
                    }
                }
                else
                {
                    progress = Mathf.Min(progress + simulatedProgressIncrement, 0.99f); 
                    EditorUtility.DisplayProgressBar("添加包", $"正在添加: {address}", progress);
                }
            }
        }*/

        /// <summary>
        /// 查找匹配的闭合方括号
        /// </summary>
        private static int FindMatchingBracket(string content, int openBracketIndex)
        {
            int count = 1;
            for (int i = openBracketIndex + 1; i < content.Length; i++)
            {
                if (content[i] == '[') count++;
                else if (content[i] == ']') count--;

                if (count == 0) return i;
            }

            return -1;
        }

        #endregion
    }
}