using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using UnityEngine;

namespace AgentSkill
{
    /// <summary>
    /// HTTP 服务器，用于接收 Agent Skills 相关请求（编辑器模式）。
    /// 路由调度通过 AgentSkillRegistry 查表完成，具体 Skill 实现在独立文件中。
    /// </summary>
    public static class SkillsHttpServer
    {
        private static HttpListener listener;
        private static Thread listenLoop;
        private static bool isRunning;
        private static int currentPort;

        // 主线程操作队列（后台线程将操作入队，EditorApplication.update 在主线程消费）
        private static readonly Queue<PendingOperation> mainThreadQueue = new Queue<PendingOperation>();
        private static readonly object queueLock = new object();

        /// <summary>
        /// 服务器是否正在运行
        /// </summary>
        public static bool IsRunning => isRunning;

        /// <summary>
        /// 当前服务器端口号
        /// </summary>
        public static int CurrentPort => currentPort;

        /// <summary>
        /// 服务器完整 URL
        /// </summary>
        public static string ServerUrl => isRunning ? $"http://localhost:{currentPort}/" : null;

        // 封装一次主线程操作：执行函数 + 结果 + 完成信号
        private class PendingOperation
        {
            public Func<string> Execute;
            public ManualResetEventSlim Done = new ManualResetEventSlim(false);
            public string Result;
        }

        /// <summary>
        /// 由 EditorApplication.update 调用，在主线程处理队列中的待执行操作
        /// </summary>
        public static void ProcessMainThreadQueue()
        {
            while (true)
            {
                PendingOperation op;
                lock (queueLock)
                {
                    if (mainThreadQueue.Count == 0) break;
                    op = mainThreadQueue.Dequeue();
                }
                try
                {
                    op.Result = op.Execute();
                }
                catch (Exception e)
                {
                    op.Result = $"{{\"success\":false,\"error\":\"{EscapeJson(e.Message)}\"}}";
                }
                op.Done.Set();
            }
        }

        /// <summary>
        /// 将操作调度到主线程执行并等待结果（由后台线程调用）
        /// </summary>
        private static string ExecuteOnMainThread(Func<string> action, int timeoutMs = 5000)
        {
            var op = new PendingOperation { Execute = action };
            lock (queueLock) { mainThreadQueue.Enqueue(op); }
            return op.Done.Wait(timeoutMs)
                ? op.Result
                : "{\"success\":false,\"error\":\"主线程操作超时\"}";
        }

        /// <summary>
        /// 启动 HTTP 服务器，若端口被占用则自动递增尝试（范围 8090~8100）
        /// </summary>
        /// <param name="startPort">起始端口号</param>
        public static void Start(int startPort = 8090)
        {
            if (isRunning)
            {
                Debug.LogWarning("[SkillsHttpServer] 服务器已在运行中");
                return;
            }

            const int maxPort = 8100;

            for (int port = startPort; port <= maxPort; port++)
            {
                try
                {
                    listener = new HttpListener();
                    listener.Prefixes.Add($"http://localhost:{port}/");
                    listener.Start();
                    isRunning = true;
                    currentPort = port;

                    listenLoop = new Thread(Listen);
                    listenLoop.IsBackground = true;
                    listenLoop.Start();

                    return;
                }
                catch (Exception)
                {
                    listener?.Close();
                    listener = null;

                    if (port < maxPort)
                        Debug.LogWarning($"[SkillsHttpServer] 端口 {port} 被占用，尝试下一个端口...");
                    else
                        Debug.LogError($"[SkillsHttpServer] 端口 {startPort}~{maxPort} 均不可用，服务器启动失败");
                }
            }
        }

        /// <summary>
        /// 停止 HTTP 服务器
        /// </summary>
        public static void Stop()
        {
            if (!isRunning) return;

            isRunning = false;
            currentPort = 0;

            // 清空队列，通知所有等待中的操作失败
            lock (queueLock)
            {
                while (mainThreadQueue.Count > 0)
                {
                    var op = mainThreadQueue.Dequeue();
                    op.Result = "{\"success\":false,\"error\":\"服务器已停止\"}";
                    op.Done.Set();
                }
            }

            try
            {
                listener?.Stop();
                listener?.Close();
                listener = null;

                if (listenLoop != null && listenLoop.IsAlive)
                {
                    listenLoop.Join(1000);
                    listenLoop = null;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SkillsHttpServer] 停止时出错: {e.Message}");
            }
        }

        /// <summary>
        /// 监听 HTTP 请求的后台线程
        /// </summary>
        private static void Listen()
        {
            while (isRunning && listener != null && listener.IsListening)
            {
                try
                {
                    var context = listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => HandleRequest(context));
                }
                catch (Exception e)
                {
                    if (isRunning)
                        Debug.LogError($"[SkillsHttpServer] 监听错误: {e.Message}");
                }
            }
        }

        /// <summary>
        /// 处理 HTTP 请求：/ping 直接响应，其余路由通过 AgentSkillRegistry 查表分发
        /// </summary>
        private static void HandleRequest(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;
                response.ContentType = "application/json; charset=utf-8";
                response.StatusCode = 200;

                var path = request.Url.AbsolutePath.TrimEnd('/').ToLower();
                string responseString;

                if (path == "/ping" && request.HttpMethod == "GET")
                {
                    responseString = $"{{\"status\":\"ok\",\"port\":{currentPort}}}";
                }
                else
                {
                    var route = path.TrimStart('/');
                    MethodInfo skillMethod = AgentSkillRegistry.Find(route, request.HttpMethod);

                    if (skillMethod != null)
                    {
                        string body;
                        using (var reader = new StreamReader(request.InputStream, System.Text.Encoding.UTF8))
                            body = reader.ReadToEnd();

                        var capturedBody = body;
                        responseString = ExecuteOnMainThread(() =>
                            (string)skillMethod.Invoke(null, new object[] { capturedBody }));
                    }
                    else
                    {
                        response.StatusCode = 404;
                        responseString = $"{{\"success\":false,\"error\":\"未知端点: {EscapeJson(path)}\"}}";
                    }
                }

                var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SkillsHttpServer] 处理请求错误: {e.Message}");
            }
        }

        /// <summary>
        /// 对 JSON 字符串值中的特殊字符进行转义
        /// </summary>
        private static string EscapeJson(string s)
        {
            return s?.Replace("\\", "\\\\").Replace("\"", "\\\"")
                     .Replace("\n", "\\n").Replace("\r", "\\r") ?? "";
        }
    }
}
