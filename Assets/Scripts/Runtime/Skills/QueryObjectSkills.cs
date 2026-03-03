using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AgentSkill
{
    /// <summary>
    /// 场景查询相关 Skills。
    /// 所有方法由 Server 保证在主线程调用，无需自行处理线程调度。
    /// </summary>
    public static class QueryObjectSkills
    {
        [Serializable]
        private class FindObjectsParams
        {
            public string name = "";
            public string tag = "";
            public string componentType = "";
            public bool includeInactive = false;
            public int limit = 100;
        }

        /// <summary>
        /// 根据名称、Tag、组件类型查询场景中的 GameObject
        /// </summary>
        [AgentSkill("find_objects", SkillType.Query, description: "根据名称、Tag 或组件类型查询场景中的 GameObject，支持组合过滤、是否包含未激活物体")]
        public static SkillResponse FindObjects(SkillRequest request)
        {
            var body = request.Body;
            FindObjectsParams p;
            try
            {
                p = string.IsNullOrEmpty(body)
                    ? new FindObjectsParams()
                    : JsonUtility.FromJson<FindObjectsParams>(body);
            }
            catch
            {
                p = new FindObjectsParams();
            }

            bool hasName      = !string.IsNullOrEmpty(p.name);
            bool hasTag       = !string.IsNullOrEmpty(p.tag);
            bool hasComponent = !string.IsNullOrEmpty(p.componentType);

            if (!hasName && !hasTag && !hasComponent)
                return SkillResponse.Fail("至少需要提供一个过滤条件：name、tag 或 componentType");

            // 获取候选集
            GameObject[] candidates;
            if (hasTag)
            {
                try
                {
                    candidates = GameObject.FindGameObjectsWithTag(p.tag);
                }
                catch (UnityException)
                {
                    return SkillResponse.Fail($"Tag \"{SkillResponse.EscapeJson(p.tag)}\" 不存在，请检查 Tag 名称是否已在 Project Settings 中注册");
                }
            }
            else
            {
                candidates = UnityEngine.Object.FindObjectsOfType<GameObject>(p.includeInactive);
            }

            // 依次过滤 name 和 componentType，取前 limit 条
            var results = new List<GameObject>();
            foreach (var go in candidates)
            {
                if (hasName && go.name.IndexOf(p.name, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                if (hasComponent)
                {
                    bool matched = false;
                    foreach (var comp in go.GetComponents<Component>())
                    {
                        if (comp != null && comp.GetType().Name.Equals(p.componentType, StringComparison.OrdinalIgnoreCase))
                        {
                            matched = true;
                            break;
                        }
                    }
                    if (!matched) continue;
                }

                results.Add(go);
                if (results.Count >= p.limit)
                    break;
            }

            // 手动拼接 JSON 响应
            var sb = new StringBuilder();
            sb.Append("{\"success\":true,\"count\":");
            sb.Append(results.Count);
            sb.Append(",\"objects\":[");

            for (int i = 0; i < results.Count; i++)
            {
                if (i > 0) sb.Append(",");

                var go  = results[i];
                var pos = go.transform.position;

                // 拼接组件名称数组
                var compSb    = new StringBuilder();
                bool firstComp = true;
                foreach (var comp in go.GetComponents<Component>())
                {
                    if (comp == null) continue;
                    if (!firstComp) compSb.Append(",");
                    compSb.Append("\"");
                    compSb.Append(SkillResponse.EscapeJson(comp.GetType().Name));
                    compSb.Append("\"");
                    firstComp = false;
                }

                sb.Append("{");
                sb.Append("\"name\":\"");    sb.Append(SkillResponse.EscapeJson(go.name));    sb.Append("\",");
                sb.Append("\"tag\":\"");     sb.Append(SkillResponse.EscapeJson(go.tag));     sb.Append("\",");
                sb.Append("\"active\":");    sb.Append(go.activeInHierarchy ? "true" : "false");            sb.Append(",");
                sb.Append("\"instanceId\":"); sb.Append(go.GetInstanceID());                               sb.Append(",");
                sb.Append("\"position\":{\"x\":");
                sb.Append(pos.x); sb.Append(",\"y\":"); sb.Append(pos.y); sb.Append(",\"z\":"); sb.Append(pos.z);
                sb.Append("},");
                sb.Append("\"components\":["); sb.Append(compSb); sb.Append("]");
                sb.Append("}");
            }

            sb.Append("]}");
            return SkillResponse.Ok(sb.ToString());
        }
    }
}
