using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AgentSkill
{
    /// <summary>
    /// 管理 Agent 操作的 Undo 事务，基于 Unity 内置 Undo 分组机制。
    /// BeginTransaction 开启分组，CommitTransaction 封存，RollbackTransaction 撤销整组。
    /// 未开启事务时，每个 Mutate 操作也会自动注册 Undo，支持编辑器 Ctrl+Z。
    /// </summary>
    public static class AgentTransactionManager
    {
#if UNITY_EDITOR
        private static int _activeGroupIndex = -1;
        private static bool _isActive = false;

        /// <summary>当前是否有活跃事务</summary>
        public static bool IsActive => _isActive;

        /// <summary>
        /// 开启一个新的 Undo 事务分组，后续所有 Mutate 操作归入同一分组
        /// </summary>
        public static void BeginTransaction(string name = "Agent Transaction")
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(name);
            _activeGroupIndex = Undo.GetCurrentGroup();
            _isActive = true;
        }

        /// <summary>
        /// 提交事务：封存当前 Undo Group，后续操作不再混入该分组
        /// </summary>
        public static void CommitTransaction()
        {
            Undo.IncrementCurrentGroup();
            _isActive = false;
        }

        /// <summary>
        /// 回滚事务：撤销整组操作。未开启事务时返回错误信息，否则返回 "ok"
        /// </summary>
        public static string RollbackTransaction()
        {
            if (!_isActive)
                return "没有活跃事务";

            Undo.RevertAllDownToGroup(_activeGroupIndex);
            _isActive = false;
            return "ok";
        }

        /// <summary>
        /// 执行一步 Undo（等同于 Ctrl+Z）
        /// </summary>
        public static void PerformUndo()
        {
            Undo.PerformUndo();
        }

        // ---- 供 Mutate Skill 调用的 Undo 辅助方法 ----

        /// <summary>
        /// 在修改 Object 前记录其当前状态（用于 move/rotate/scale 等写操作）
        /// </summary>
        public static void RecordObject(Object obj, string label)
        {
            Undo.RecordObject(obj, label);
        }

        /// <summary>
        /// 注册新创建的 GameObject 到 Undo 系统，使其创建可被撤销
        /// </summary>
        public static void RegisterCreatedObject(GameObject go, string label)
        {
            Undo.RegisterCreatedObjectUndo(go, label);
        }

        /// <summary>
        /// 销毁 GameObject 并自动注册到 Undo 系统（可撤销的删除）
        /// </summary>
        public static void DestroyObject(GameObject go)
        {
            Undo.DestroyObjectImmediate(go);
        }
#endif
    }
}
