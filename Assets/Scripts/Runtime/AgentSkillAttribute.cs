using System;

namespace AgentSkill
{
    /// <summary>
    /// 标记静态方法为 Agent Skill，由 AgentSkillRegistry 自动发现并注册。
    /// 方法签名约定：static string MethodName(SkillRequest request)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class AgentSkillAttribute : Attribute
    {
        /// <summary>路由名称，如 "create_object"（无需前缀斜杠）</summary>
        public string Route { get; }

        /// <summary>HTTP 方法，默认 "POST"</summary>
        public string HttpMethod { get; }

        /// <summary>Skill 描述，用于编辑器窗口展示</summary>
        public string Description { get; }

        public AgentSkillAttribute(string route, string httpMethod = "POST", string description = "")
        {
            Route = route;
            HttpMethod = httpMethod;
            Description = description;
        }
    }
}
