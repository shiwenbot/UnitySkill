using Sirenix.Utilities.Editor;
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
            SirenixEditorGUI.BeginBox();
            {
                GUILayout.Label("服务器连接", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                // 获取服务器状态
                var isServerRunning = SkillsHttpServer.IsRunning;
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
                            SkillsHttpServer.Start(8080);
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
        }

        private void DrawSkillsTab()
        {
            SirenixEditorGUI.BeginBox();
            GUILayout.Label("Skills", EditorStyles.boldLabel);
            SirenixEditorGUI.EndBox();
        }

        private void DrawHistoryTab()
        {
            SirenixEditorGUI.BeginBox();
            GUILayout.Label("History", EditorStyles.boldLabel);
            SirenixEditorGUI.EndBox();
        }
    }
}
