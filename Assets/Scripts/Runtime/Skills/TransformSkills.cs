using Newtonsoft.Json;
using UnityEngine;

namespace AgentSkill
{
    /// <summary>
    /// Transform 相关 Skills。
    /// 所有方法由 Server 保证在主线程调用，无需自行处理线程调度。
    /// </summary>
    public static class TransformSkills
    {
        private class MoveObjectParams
        {
            public string name = "";
            public int instanceId = -1;
            // 世界坐标位置，null 表示不修改
            public float? x;
            public float? y;
            public float? z;
            // 世界旋转（欧拉角），null 表示不修改
            public float? rotX;
            public float? rotY;
            public float? rotZ;
            // 缩放，null 表示不修改
            public float? scaleX;
            public float? scaleY;
            public float? scaleZ;
        }

        /// <summary>
        /// 修改指定 GameObject 的 Transform（位置、旋转、缩放）
        /// </summary>
        [AgentSkill("move_object", SkillType.Mutate, description: "修改指定 GameObject 的 Transform，支持按名称或 instanceId 定位，可单独修改位置、旋转或缩放，未提供的字段保持不变")]
        public static SkillResponse MoveObject(SkillRequest request)
        {
            var body = request.Body;
            MoveObjectParams p;
            try
            {
                p = string.IsNullOrEmpty(body)
                    ? new MoveObjectParams()
                    : JsonConvert.DeserializeObject<MoveObjectParams>(body);
            }
            catch
            {
                p = new MoveObjectParams();
            }

            // 查找 GameObject：instanceId 优先，否则按名称查找
            GameObject go = null;
            if (p.instanceId >= 0)
            {
                foreach (var obj in UnityEngine.Object.FindObjectsOfType<GameObject>(true))
                {
                    if (obj.GetInstanceID() == p.instanceId)
                    {
                        go = obj;
                        break;
                    }
                }
            }
            else
            {
                go = GameObject.Find(p.name);
            }

            if (go == null)
            {
                var identifier = p.instanceId >= 0
                    ? $"instanceId={p.instanceId}"
                    : $"name=\"{p.name}\"";
                return SkillResponse.Fail($"找不到 GameObject: {identifier}");
            }

            // 更新位置（仅修改非 null 的分量）
            if (p.x.HasValue || p.y.HasValue || p.z.HasValue)
            {
                var pos = go.transform.position;
                go.transform.position = new Vector3(
                    p.x ?? pos.x,
                    p.y ?? pos.y,
                    p.z ?? pos.z
                );
            }

            // 更新旋转（欧拉角，仅修改非 null 的分量）
            if (p.rotX.HasValue || p.rotY.HasValue || p.rotZ.HasValue)
            {
                var rot = go.transform.eulerAngles;
                go.transform.eulerAngles = new Vector3(
                    p.rotX ?? rot.x,
                    p.rotY ?? rot.y,
                    p.rotZ ?? rot.z
                );
            }

            // 更新缩放（仅修改非 null 的分量）
            if (p.scaleX.HasValue || p.scaleY.HasValue || p.scaleZ.HasValue)
            {
                var scale = go.transform.localScale;
                go.transform.localScale = new Vector3(
                    p.scaleX ?? scale.x,
                    p.scaleY ?? scale.y,
                    p.scaleZ ?? scale.z
                );
            }

            // 读取最终状态返回
            var finalPos   = go.transform.position;
            var finalRot   = go.transform.eulerAngles;
            var finalScale = go.transform.localScale;

            return SkillResponse.Ok(JsonConvert.SerializeObject(new
            {
                success  = true,
                name     = go.name,
                position = new { x = finalPos.x,   y = finalPos.y,   z = finalPos.z },
                rotation = new { x = finalRot.x,   y = finalRot.y,   z = finalRot.z },
                scale    = new { x = finalScale.x, y = finalScale.y, z = finalScale.z }
            }));
        }
    }
}
