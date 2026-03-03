namespace AgentSkill
{
    /// <summary>
    /// 封装一次 Skill 执行的响应结果，包含 HTTP 状态码和 JSON 响应体。
    /// </summary>
    public class SkillResponse
    {
        /// <summary>JSON 格式的响应内容</summary>
        public string ResponseJson { get; }

        /// <summary>HTTP 状态码，默认 200</summary>
        public int StatusCode { get; }

        private SkillResponse(string responseJson, int statusCode)
        {
            ResponseJson = responseJson;
            StatusCode = statusCode;
        }

        /// <summary>
        /// 构建成功响应（HTTP 200）
        /// </summary>
        /// <param name="json">响应 JSON 字符串</param>
        public static SkillResponse Ok(string json)
        {
            return new SkillResponse(json, 200);
        }

        /// <summary>
        /// 构建失败响应，自动生成 {"success":false,"error":"..."} 格式的 JSON
        /// </summary>
        /// <param name="error">错误信息</param>
        /// <param name="statusCode">HTTP 状态码</param>
        public static SkillResponse Fail(string error, int statusCode = 400)
        {
            var json = $"{{\"success\":false,\"error\":\"{EscapeJson(error)}\"}}";
            return new SkillResponse(json, statusCode);
        }

        /// <summary>
        /// 对 JSON 字符串值中的特殊字符进行转义
        /// </summary>
        internal static string EscapeJson(string s)
        {
            return s?.Replace("\\", "\\\\").Replace("\"", "\\\"")
                     .Replace("\n", "\\n").Replace("\r", "\\r") ?? "";
        }
    }
}
