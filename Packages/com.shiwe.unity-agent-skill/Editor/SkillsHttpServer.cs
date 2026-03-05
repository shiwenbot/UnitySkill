using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
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
            public Func<SkillResponse> Execute;
            public ManualResetEventSlim Done = new ManualResetEventSlim(false);
            public SkillResponse Result;
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
                    op.Result = SkillResponse.Fail(e.Message, 500);
                }
                op.Done.Set();
            }
        }

        /// <summary>
        /// 将操作调度到主线程执行并等待结果（由后台线程调用）
        /// </summary>
        private static SkillResponse ExecuteOnMainThread(Func<SkillResponse> action, int timeoutMs = 5000)
        {
            var op = new PendingOperation { Execute = action };
            lock (queueLock) { mainThreadQueue.Enqueue(op); }
            return op.Done.Wait(timeoutMs)
                ? op.Result
                : SkillResponse.Fail("主线程操作超时", 504);
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
                    op.Result = SkillResponse.Fail("服务器已停止", 503);
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
        /// 处理 HTTP 请求：/ping 直接响应，内置事务路由优先处理，其余通过 AgentSkillRegistry 查表分发
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
                else if (IsTransactionRoute(path) && request.HttpMethod == "POST")
                {
                    string txBody;
                    using (var reader = new StreamReader(request.InputStream, System.Text.Encoding.UTF8))
                        txBody = reader.ReadToEnd();

                    var txResponse = ExecuteOnMainThread(() => HandleTransactionRoute(path.TrimStart('/'), txBody));
                    response.StatusCode = txResponse.StatusCode;
                    responseString = txResponse.ResponseJson;
                }
                else
                {
                    var route = path.TrimStart('/');
                    Func<SkillRequest, SkillResponse> skillDelegate = AgentSkillRegistry.Find(route, request.HttpMethod);

                    if (skillDelegate != null)
                    {
                        string body;
                        using (var reader = new StreamReader(request.InputStream, System.Text.Encoding.UTF8))
                            body = reader.ReadToEnd();

                        var skillRequest = new SkillRequest
                        {
                            Context = context,
                            HttpMethod = request.HttpMethod,
                            Path = path,
                            Body = body,
                            EnqueueTimeTicks = DateTime.UtcNow.Ticks,
                            RequestId = Guid.NewGuid().ToString("N")
                        };
                        var skillResponse = ExecuteOnMainThread(() => skillDelegate(skillRequest));
                        response.StatusCode = skillResponse.StatusCode;
                        responseString = skillResponse.ResponseJson;
                    }
                    else
                    {
                        var failResponse = SkillResponse.Fail("未知端点: " + path, 404);
                        response.StatusCode = failResponse.StatusCode;
                        responseString = failResponse.ResponseJson;
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
        /// 判断路径是否为内置事务路由
        /// </summary>
        private static bool IsTransactionRoute(string path)
        {
            return path == "/begin_transaction" ||
                   path == "/commit_transaction" ||
                   path == "/rollback_transaction" ||
                   path == "/undo";
        }

        /// <summary>
        /// 处理内置事务路由（在主线程执行）
        /// </summary>
        private static SkillResponse HandleTransactionRoute(string route, string body)
        {
#if UNITY_EDITOR
            switch (route)
            {
                case "begin_transaction":
                {
                    string name = "Agent Transaction";
                    if (!string.IsNullOrEmpty(body))
                    {
                        try
                        {
                            var p = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, string>>(body);
                            if (p != null && p.TryGetValue("name", out var n) && !string.IsNullOrEmpty(n))
                                name = n;
                        }
                        catch { }
                    }
                    AgentTransactionManager.BeginTransaction(name);
                    return SkillResponse.Ok(JsonConvert.SerializeObject(new { success = true, transactionName = name }));
                }
                case "commit_transaction":
                    AgentTransactionManager.CommitTransaction();
                    return SkillResponse.Ok(JsonConvert.SerializeObject(new { success = true }));

                case "rollback_transaction":
                {
                    var result = AgentTransactionManager.RollbackTransaction();
                    if (result == "ok")
                        return SkillResponse.Ok(JsonConvert.SerializeObject(new { success = true }));
                    return SkillResponse.Fail(result);
                }
                case "undo":
                    AgentTransactionManager.PerformUndo();
                    return SkillResponse.Ok(JsonConvert.SerializeObject(new { success = true }));

                default:
                    return SkillResponse.Fail("未知事务路由: " + route, 404);
            }
#else
            return SkillResponse.Fail("事务系统仅在 Unity 编辑器中可用");
#endif
        }
    }
}
