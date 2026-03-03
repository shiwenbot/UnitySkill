using Newtonsoft.Json;
using UnityEngine;

namespace AgentSkill
{
    /// <summary>
    /// GameObject 相关 Skills。
    /// 所有方法由 Server 保证在主线程调用，无需自行处理线程调度。
    /// </summary>
    public static class GameObjectSkills
    {
        private class CreateObjectParams
        {
            public string name = "NewObject";
            public string primitiveType = "Cube";
            public float x;
            public float y;
            public float z;
            // 缩放，默认 1
            public float scaleX = 1f;
            public float scaleY = 1f;
            public float scaleZ = 1f;
            // 颜色（0~1 范围），负值表示不设置颜色
            public float colorR = -1f;
            public float colorG = -1f;
            public float colorB = -1f;
        }

        private class DeleteObjectParams
        {
            public string name = "";
            public int instanceId = -1;
        }

        /// <summary>
        /// 在场景中创建一个基础几何体 GameObject
        /// </summary>
        [AgentSkill("create_object", description: "在场景中创建一个基础几何体，支持指定名称、类型（Cube/Sphere/Capsule/Cylinder/Plane）、位置、缩放和颜色")]
        public static SkillResponse CreateObject(SkillRequest request)
        {
            var body = request.Body;
            CreateObjectParams p;
            try
            {
                p = string.IsNullOrEmpty(body)
                    ? new CreateObjectParams()
                    : JsonConvert.DeserializeObject<CreateObjectParams>(body);
            }
            catch
            {
                p = new CreateObjectParams();
            }

            PrimitiveType type;
            switch (p.primitiveType.ToLower())
            {
                case "sphere":   type = PrimitiveType.Sphere;   break;
                case "capsule":  type = PrimitiveType.Capsule;  break;
                case "cylinder": type = PrimitiveType.Cylinder; break;
                case "plane":    type = PrimitiveType.Plane;    break;
                default:         type = PrimitiveType.Cube;     break;
            }

            var go = GameObject.CreatePrimitive(type);
            go.name = p.name;
            go.transform.position = new Vector3(p.x, p.y, p.z);
            go.transform.localScale = new Vector3(p.scaleX, p.scaleY, p.scaleZ);

            // 设置颜色（colorR >= 0 时生效）
            if (p.colorR >= 0f)
            {
                var renderer = go.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // 创建独立材质实例，避免共享材质互相影响
                    var mat = new Material(renderer.sharedMaterial);
                    mat.color = new Color(p.colorR, p.colorG, p.colorB);
                    renderer.material = mat;
                }
            }

#if UNITY_EDITOR
            AgentTransactionManager.RegisterCreatedObject(go, "create_object");
#endif

            return SkillResponse.Ok(JsonConvert.SerializeObject(new { success = true, name = go.name }));
        }

        /// <summary>
        /// 删除场景中指定的 GameObject，支持按名称或 instanceId 定位
        /// </summary>
        [AgentSkill("delete_object", SkillType.Mutate, description: "删除场景中指定的 GameObject，支持按名称（name）或 instanceId 定位，操作可通过 undo/rollback_transaction 撤销")]
        public static SkillResponse DeleteObject(SkillRequest request)
        {
            var body = request.Body;
            DeleteObjectParams p;
            try
            {
                p = string.IsNullOrEmpty(body)
                    ? new DeleteObjectParams()
                    : JsonConvert.DeserializeObject<DeleteObjectParams>(body);
            }
            catch
            {
                p = new DeleteObjectParams();
            }

            // 查找 GameObject：instanceId 优先，否则按名称查找
            GameObject go = null;
            if (p.instanceId >= 0)
            {
                foreach (var obj in Object.FindObjectsOfType<GameObject>(true))
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

            var deletedName = go.name;

#if UNITY_EDITOR
            AgentTransactionManager.DestroyObject(go);
#else
            Object.Destroy(go);
#endif

            return SkillResponse.Ok(JsonConvert.SerializeObject(new { success = true, name = deletedName }));
        }
    }
}
