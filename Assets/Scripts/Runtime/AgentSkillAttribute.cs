using System;

namespace AgentSkill
{
    /// <summary>
    /// Skill 的操作类型：查询（只读）或修改（写操作）
    /// </summary>
    public enum SkillType
    {
        /// <summary>只读查询，不改变场景状态</summary>
        Query,
        /// <summary>写操作，创建/修改/删除场景对象</summary>
        Mutate
    }

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

        /// <summary>Skill 的操作类型，默认 Mutate</summary>
        public SkillType SkillType { get; }

        /// <summary>Skill 描述，用于编辑器窗口展示</summary>
        public string Description { get; }

        public AgentSkillAttribute(string route, SkillType skillType = SkillType.Mutate, string httpMethod = "POST", string description = "")
        {
            Route = route;
            SkillType = skillType;
            HttpMethod = httpMethod;
            Description = description;
        }
    }
}
