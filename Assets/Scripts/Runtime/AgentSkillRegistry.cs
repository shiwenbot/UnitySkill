using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace AgentSkill
{
    /// <summary>
    /// 扫描所有用户程序集，收集带 AgentSkillAttribute 的静态方法并按路由注册。
    /// 在 Domain Reload 后需重新调用 Scan()。
    /// </summary>
    public static class AgentSkillRegistry
    {
        // key 格式："HTTPMETHOD:route"，如 "POST:create_object"
        private static readonly Dictionary<string, MethodInfo> registry =
            new Dictionary<string, MethodInfo>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 扫描 AppDomain 中所有用户程序集，注册带 AgentSkillAttribute 的静态方法
        /// </summary>
        public static void Scan()
        {
            registry.Clear();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var name = assembly.GetName().Name;

                // 跳过系统和 Unity 引擎程序集以加快扫描速度
                if (name.StartsWith("System") || name.StartsWith("Unity") ||
                    name.StartsWith("mscorlib") || name.StartsWith("Mono") ||
                    name.StartsWith("Microsoft") || name.StartsWith("netstandard") ||
                    name.StartsWith("nunit") || name.StartsWith("Sirenix"))
                    continue;

                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        foreach (var method in type.GetMethods(
                            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                        {
                            var attr = method.GetCustomAttribute<AgentSkillAttribute>();
                            if (attr == null) continue;

                            var key = $"{attr.HttpMethod.ToUpper()}:{attr.Route.ToLower()}";
                            registry[key] = method;
                            Debug.Log($"[AgentSkillRegistry] 注册 Skill: {key} → {type.Name}.{method.Name}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[AgentSkillRegistry] 扫描程序集 {name} 时出错: {e.Message}");
                }
            }

            Debug.Log($"[AgentSkillRegistry] 扫描完成，共注册 {registry.Count} 个 Skill");
        }

        /// <summary>
        /// 按路由和 HTTP 方法查找已注册的 Skill 方法，未找到返回 null
        /// </summary>
        public static MethodInfo Find(string route, string httpMethod)
        {
            var key = $"{httpMethod.ToUpper()}:{route.ToLower()}";
            registry.TryGetValue(key, out var method);
            return method;
        }
    }
}
