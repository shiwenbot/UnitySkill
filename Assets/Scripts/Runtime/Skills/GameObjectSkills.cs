using System;
using UnityEngine;

namespace AgentSkill
{
    /// <summary>
    /// GameObject 相关 Skills。
    /// 所有方法由 Server 保证在主线程调用，无需自行处理线程调度。
    /// </summary>
    public static class GameObjectSkills
    {
        [Serializable]
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
                    : JsonUtility.FromJson<CreateObjectParams>(body);
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

            return SkillResponse.Ok($"{{\"success\":true,\"name\":\"{SkillResponse.EscapeJson(go.name)}\"}}");
        }
    }
}
