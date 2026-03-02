using System.Net;

namespace AgentSkill
{
    /// <summary>
    /// 封装一次 HTTP 请求的完整上下文，作为 Skill 委托的统一入参。
    /// </summary>
    public class SkillRequest
    {
        /// <summary>HTTP 上下文对象（含 Request / Response）</summary>
        public HttpListenerContext Context;

        /// <summary>HTTP 方法，如 "GET"、"POST"</summary>
        public string HttpMethod;

        /// <summary>请求路径，如 "/create_object"</summary>
        public string Path;

        /// <summary>请求体原始 JSON 字符串</summary>
        public string Body;

        /// <summary>任务加入队列时的 UTC 时间戳（DateTime.UtcNow.Ticks）</summary>
        public long EnqueueTimeTicks;

        /// <summary>唯一请求 ID（Guid.NewGuid().ToString("N")）</summary>
        public string RequestId;
    }
}
