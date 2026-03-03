using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace AgentSkill
{
    /// <summary>
    /// 场景查询相关 Skills。
    /// 所有方法由 Server 保证在主线程调用，无需自行处理线程调度。
    /// </summary>
    public static class QueryObjectSkills
    {
        private class FindObjectsParams
        {
            public string name = "";
            public string tag = "";
            public string componentType = "";
            public bool includeInactive = false;
            public int limit = 100;
        }

        private class PositionInfo
        {
            public float x;
            public float y;
            public float z;
        }

        private class ObjectInfo
        {
            public string name;
            public string tag;
            public bool active;
            public int instanceId;
            public PositionInfo position;
            public List<string> components;
        }

        private class FindObjectsResult
        {
            public bool success;
            public int count;
            public List<ObjectInfo> objects;
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
                    : JsonConvert.DeserializeObject<FindObjectsParams>(body);
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
                    return SkillResponse.Fail($"Tag \"{p.tag}\" 不存在，请检查 Tag 名称是否已在 Project Settings 中注册");
                }
            }
            else
            {
                candidates = UnityEngine.Object.FindObjectsOfType<GameObject>(p.includeInactive);
            }

            // 依次过滤 name 和 componentType，取前 limit 条
            var objectInfos = new List<ObjectInfo>();
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

                var pos = go.transform.position;
                var compNames = new List<string>();
                foreach (var comp in go.GetComponents<Component>())
                {
                    if (comp != null)
                        compNames.Add(comp.GetType().Name);
                }

                objectInfos.Add(new ObjectInfo
                {
                    name       = go.name,
                    tag        = go.tag,
                    active     = go.activeInHierarchy,
                    instanceId = go.GetInstanceID(),
                    position   = new PositionInfo { x = pos.x, y = pos.y, z = pos.z },
                    components = compNames
                });

                if (objectInfos.Count >= p.limit)
                    break;
            }

            var result = new FindObjectsResult
            {
                success = true,
                count   = objectInfos.Count,
                objects = objectInfos
            };

            return SkillResponse.Ok(JsonConvert.SerializeObject(result));
        }
    }
}
