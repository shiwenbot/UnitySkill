using System;
using System.Net;
using System.Threading;
using UnityEngine;

namespace AgentSkill
{
    /// <summary>
    /// HTTP 服务器，用于接收 Agent Skills 相关请求（编辑器模式）
    /// </summary>
    public static class SkillsHttpServer
    {
        private static HttpListener listener;
        private static Thread listenerThread;
        private static bool isRunning;

        /// <summary>
        /// 服务器是否正在运行
        /// </summary>
        public static bool IsRunning => isRunning;

        /// <summary>
        /// 启动 HTTP 服务器
        /// </summary>
        /// <param name="portNumber">端口号</param>
        public static void Start(int portNumber = 8080)
        {
            if (isRunning)
            {
                Debug.LogWarning("[SkillsHttpServer] 服务器已在运行中");
                return;
            }

            try
            {
                listener = new HttpListener();
                listener.Prefixes.Add($"http://localhost:{portNumber}/");
                listener.Start();
                isRunning = true;

                listenerThread = new Thread(Listen);
                listenerThread.IsBackground = true;
                listenerThread.Start();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SkillsHttpServer] 启动失败: {e.Message}");
                isRunning = false;
            }
        }

        /// <summary>
        /// 停止 HTTP 服务器
        /// </summary>
        public static void Stop()
        {
            if (!isRunning)
            {
                return;
            }

            isRunning = false;

            try
            {
                listener?.Stop();
                listener?.Close();
                listener = null;

                if (listenerThread != null && listenerThread.IsAlive)
                {
                    listenerThread.Join(1000);
                    listenerThread = null;
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
                    {
                        Debug.LogError($"[SkillsHttpServer] 监听错误: {e.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 处理 HTTP 请求
        /// </summary>
        private static void HandleRequest(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;

                // 设置响应头
                response.ContentType = "text/plain";
                response.StatusCode = 200;

                // 简单响应
                var responseString = "OK";
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
    }
}
