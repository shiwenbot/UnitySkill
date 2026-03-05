using Sirenix.Utilities.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AgentSkill
{
    /// <summary>
    /// Agent Skills 主编辑器窗口
    /// </summary>
    public class AgentSkillsWindow : EditorWindow
    {
        [MenuItem("Tools/Agent Skills")]
        private static void ShowWindow()
        {
            var window = GetWindow<AgentSkillsWindow>("Agent Skills");
            var windowWidth = 500;
            var windowHeight = 400;

            // 计算屏幕中心位置
            var screenWidth = Screen.currentResolution.width;
            var screenHeight = Screen.currentResolution.height;
            var x = (screenWidth - windowWidth) / 2;
            var y = (screenHeight - windowHeight) / 2;

            window.position = new Rect(x, y, windowWidth, windowHeight);
        }

        private int selectedTab;
        private List<AgentSkillRegistry.SkillInfo> _skills = new List<AgentSkillRegistry.SkillInfo>();
        private Vector2 _skillsScrollPos;

        private void OnEnable()
        {
            RefreshSkills();
        }

        private void RefreshSkills()
        {
            AgentSkillRegistry.Scan();
            _skills = new List<AgentSkillRegistry.SkillInfo>(AgentSkillRegistry.GetAllSkills());
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();

            // Tab 选择器
            selectedTab = GUILayout.Toolbar(selectedTab, new string[] { "Server", "Skills", "History" });

            EditorGUILayout.Space(10);

            // 根据 Tab 显示内容
            switch (selectedTab)
            {
                case 0:
                    DrawServerTab();
                    break;
                case 1:
                    DrawSkillsTab();
                    break;
                case 2:
                    DrawHistoryTab();
                    break;
            }
        }

        private void Update()
        {
            // 持续重绘以更新服务器状态
            Repaint();
        }

        private void DrawServerTab()
        {
            // 获取服务器状态
            var isServerRunning = SkillsHttpServer.IsRunning;

            SirenixEditorGUI.BeginBox();
            {
                GUILayout.Label("服务器连接", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                var statusColor = isServerRunning ? new Color(0.3f, 0.8f, 0.3f) : new Color(1f, 0.3f, 0.3f);
                var statusText = isServerRunning ? "服务器运行中" : "服务器已停止";

                EditorGUILayout.BeginHorizontal();
                {
                    // 状态文字
                    var oldColor = GUI.color;
                    GUI.color = statusColor;
                    GUILayout.Label(statusText, SirenixGUIStyles.BoldTitle);
                    GUI.color = oldColor;

                    // 弹性空间，将按钮推到右侧
                    GUILayout.FlexibleSpace();

                    // 启动/停止按钮
                    if (!isServerRunning)
                    {
                        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
                        if (GUILayout.Button("启动服务器", GUILayout.Width(120), GUILayout.Height(30)))
                        {
                            SkillsHttpServer.Start();
                        }
                    }
                    else
                    {
                        GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                        if (GUILayout.Button("停止服务器", GUILayout.Width(120), GUILayout.Height(30)))
                        {
                            SkillsHttpServer.Stop();
                        }
                    }
                    GUI.backgroundColor = Color.white;
                }
                EditorGUILayout.EndHorizontal();
            }
            SirenixEditorGUI.EndBox();

            // 服务器 URL 显示
            if (isServerRunning)
            {
                EditorGUILayout.Space(5);
                SirenixEditorGUI.BeginBox();
                {
                    GUILayout.Label("连接信息", EditorStyles.boldLabel);
                    EditorGUILayout.Space();

                    var serverUrl = SkillsHttpServer.ServerUrl;
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.SelectableLabel(serverUrl, EditorStyles.textField, GUILayout.Height(25));
                        if (GUILayout.Button("复制", GUILayout.Width(60), GUILayout.Height(25)))
                        {
                            GUIUtility.systemCopyBuffer = serverUrl;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                SirenixEditorGUI.EndBox();
            }

            EditorGUILayout.Space(5);
            DrawClaudeSkillSetup();
        }

        private string _skillSetupError = null;

        private void DrawClaudeSkillSetup()
        {
            SirenixEditorGUI.BeginBox();
            {
                GUILayout.Label("Claude Skill 配置", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                var isInstalled = SkillSetupManager.IsInstalled();
                var installDir = SkillSetupManager.GetInstallDir();

                // 状态行
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Label("状态：", GUILayout.Width(40));
                    var oldColor = GUI.color;
                    GUI.color = isInstalled ? new Color(0.3f, 0.8f, 0.3f) : new Color(1f, 0.4f, 0.4f);
                    GUILayout.Label(isInstalled ? "✓ 已安装" : "✗ 未安装", EditorStyles.boldLabel);
                    GUI.color = oldColor;
                }
                EditorGUILayout.EndHorizontal();

                // 路径行
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Label("路径：", GUILayout.Width(40));
                    EditorGUILayout.SelectableLabel(installDir, EditorStyles.miniLabel, GUILayout.Height(18));
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);

                // 安装按钮
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    var btnLabel = isInstalled ? "重新安装" : "安装 Skill";
                    GUI.backgroundColor = new Color(0.3f, 0.6f, 1f);
                    if (GUILayout.Button(btnLabel, GUILayout.Width(100), GUILayout.Height(26)))
                    {
                        _skillSetupError = null;
                        var (success, message) = SkillSetupManager.Install();
                        if (success)
                        {
                            EditorUtility.DisplayDialog("安装成功", message, "确定");
                        }
                        else
                        {
                            _skillSetupError = message;
                        }
                    }
                    GUI.backgroundColor = Color.white;
                }
                EditorGUILayout.EndHorizontal();

                // 错误提示
                if (!string.IsNullOrEmpty(_skillSetupError))
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.HelpBox(_skillSetupError, MessageType.Error);
                }
            }
            SirenixEditorGUI.EndBox();
        }

        private void DrawSkillsTab()
        {
            // 顶部标题栏
            SirenixEditorGUI.BeginBox();
            {
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Label($"已注册 Skills（{_skills.Count}）", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("刷新", GUILayout.Width(60), GUILayout.Height(20)))
                    {
                        RefreshSkills();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            SirenixEditorGUI.EndBox();

            EditorGUILayout.Space(4);

            if (_skills.Count == 0)
            {
                EditorGUILayout.HelpBox("未发现任何 Skill。请确保已使用 [AgentSkill] 标记静态方法。", MessageType.Info);
                return;
            }

            // Skill 列表
            _skillsScrollPos = EditorGUILayout.BeginScrollView(_skillsScrollPos);
            {
                foreach (var skill in _skills)
                {
                    SirenixEditorGUI.BeginBox();
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.BeginVertical();
                            {
                                GUILayout.Label(skill.Route, EditorStyles.boldLabel);
                                if (!string.IsNullOrEmpty(skill.Description))
                                {
                                    GUILayout.Label(skill.Description, EditorStyles.wordWrappedMiniLabel);
                                }
                            }
                            EditorGUILayout.EndVertical();

                            GUILayout.FlexibleSpace();

                            // 类型标签
                            var oldBgColor = GUI.backgroundColor;
                            if (skill.SkillType == SkillType.Query)
                            {
                                GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
                                GUILayout.Label("Query", EditorStyles.miniButton, GUILayout.Width(52), GUILayout.Height(18));
                            }
                            else
                            {
                                GUI.backgroundColor = new Color(1f, 0.6f, 0.2f);
                                GUILayout.Label("Mutate", EditorStyles.miniButton, GUILayout.Width(52), GUILayout.Height(18));
                            }
                            GUI.backgroundColor = oldBgColor;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    SirenixEditorGUI.EndBox();

                    EditorGUILayout.Space(2);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawHistoryTab()
        {
            SirenixEditorGUI.BeginBox();
            GUILayout.Label("History", EditorStyles.boldLabel);
            SirenixEditorGUI.EndBox();
        }
    }
}
