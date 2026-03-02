using UnityEditor;

namespace AgentSkill
{
    /// <summary>
    /// Domain Reload 前后自动保存并恢复 HTTP 服务器状态
    /// </summary>
    [InitializeOnLoad]
    public static class SkillsHttpServerReloadHandler
    {
        private const string KeyShouldRun = "AgentSkill.ServerShouldRun";
        private const string KeyPort      = "AgentSkill.ServerPort";

        /// <summary>
        /// Domain Reload 完成后由 Unity 自动调用
        /// </summary>
        static SkillsHttpServerReloadHandler()
        {
            // 读取 Reload 前保存的状态
            bool wasRunning = SessionState.GetBool(KeyShouldRun, false);
            int  savedPort  = SessionState.GetInt(KeyPort, 8090);

            // 扫描并注册所有带 AgentSkillAttribute 的 Skill
            AgentSkillRegistry.Scan();

            if (wasRunning)
            {
                SkillsHttpServer.Start(savedPort);
            }

            // 注册主线程操作队列处理（HTTP 后台线程创建 GameObject 需要主线程执行）
            EditorApplication.update += SkillsHttpServer.ProcessMainThreadQueue;

            // 订阅下一次 Reload 前的回调
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        /// <summary>
        /// 在 Domain Reload 发生前保存服务器状态并优雅关闭
        /// </summary>
        private static void OnBeforeAssemblyReload()
        {
            EditorApplication.update -= SkillsHttpServer.ProcessMainThreadQueue;

            SessionState.SetBool(KeyShouldRun, SkillsHttpServer.IsRunning);
            SessionState.SetInt(KeyPort,       SkillsHttpServer.CurrentPort);

            SkillsHttpServer.Stop();
        }
    }
}
