using System;
using System.IO;
using UnityEditor;

namespace AgentSkill
{
    /// <summary>
    /// 负责将 unity-agent-tools skill 安装到用户的 Claude Code skill 目录
    /// </summary>
    public static class SkillSetupManager
    {
        private const string SkillName = "unity-agent-tools";
        private const string ScriptsDirPlaceholder = "{SCRIPTS_DIR}";

        /// <summary>
        /// 返回 skill 安装目标目录：~/.claude/skills/unity-agent-tools
        /// </summary>
        public static string GetInstallDir()
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".claude", "skills", SkillName);
        }

        /// <summary>
        /// 检查 skill 是否已安装（安装目录下的 SKILL.md 存在）
        /// </summary>
        public static bool IsInstalled()
        {
            return File.Exists(Path.Combine(GetInstallDir(), "SKILL.md"));
        }

        /// <summary>
        /// 返回包内 ClaudeSkill 根目录，通过 PackageInfo 定位包的实际路径
        /// </summary>
        public static string GetProjectSkillRoot()
        {
            // 通过当前程序集定位本包的安装路径，兼容嵌入包和 git URL 安装两种场景
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(SkillSetupManager).Assembly);
            return Path.Combine(packageInfo.resolvedPath, "ClaudeSkill");
        }

        /// <summary>
        /// 执行安装：从模板生成 SKILL.md，并复制 docs 目录
        /// </summary>
        /// <returns>(success, message)</returns>
        public static (bool success, string message) Install()
        {
            try
            {
                var skillRoot = GetProjectSkillRoot();
                var templatePath = Path.Combine(skillRoot, "SKILL.md.template");

                if (!File.Exists(templatePath))
                {
                    return (false, $"找不到模板文件：{templatePath}");
                }

                // 构建脚本目录的绝对路径，并统一使用反斜杠（Windows 路径）
                var scriptsDir = Path.GetFullPath(Path.Combine(skillRoot, "scripts"));

                // 读取模板并替换占位符
                var templateContent = File.ReadAllText(templatePath);
                var skillMdContent = templateContent.Replace(ScriptsDirPlaceholder, scriptsDir);

                // 确保安装目录存在
                var installDir = GetInstallDir();
                Directory.CreateDirectory(installDir);

                // 写入 SKILL.md
                var skillMdPath = Path.Combine(installDir, "SKILL.md");
                File.WriteAllText(skillMdPath, skillMdContent);

                // 复制 docs 目录
                var srcDocs = Path.Combine(skillRoot, "docs");
                if (Directory.Exists(srcDocs))
                {
                    var dstDocs = Path.Combine(installDir, "docs");
                    CopyDirectory(srcDocs, dstDocs);
                }

                return (true, $"Skill 已安装到：{installDir}");
            }
            catch (Exception e)
            {
                return (false, $"安装失败：{e.Message}");
            }
        }

        private static void CopyDirectory(string srcDir, string dstDir)
        {
            Directory.CreateDirectory(dstDir);
            foreach (var file in Directory.GetFiles(srcDir))
            {
                File.Copy(file, Path.Combine(dstDir, Path.GetFileName(file)), overwrite: true);
            }
            foreach (var subDir in Directory.GetDirectories(srcDir))
            {
                CopyDirectory(subDir, Path.Combine(dstDir, Path.GetFileName(subDir)));
            }
        }
    }
}
