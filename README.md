# AgentSkill — Unity Editor AI Agent Bridge

让 AI Agent（如 Claude Code）通过 HTTP REST API 直接操控 Unity Editor 场景。

## 概述

AgentSkill 是一个 Unity Editor 插件，在编辑器内启动一个轻量 HTTP 服务器，将场景操作封装为标准 REST 接口。AI Agent 通过调用这些接口，可以在不打开 Unity 界面的情况下创建、查询、移动、删除场景中的 GameObject，并且所有操作都支持撤销（Ctrl+Z）。

```
AI Agent (Claude Code)
       │
       │  HTTP POST /create_object
       ▼
SkillsHttpServer (localhost:8090)
       │
       │  路由分发
       ▼
AgentSkillRegistry → GameObjectSkills / TransformSkills / QueryObjectSkills
       │
       ▼
Unity Scene (主线程执行 + Undo 支持)
```

## 功能特性

- **自动发现 Skill**：使用 `[AgentSkill]` 属性标注静态方法，服务器启动时通过反射自动注册为 HTTP 端点，无需手动配置路由
- **主线程安全**：所有 Skill 方法由服务器调度到 Unity 主线程执行，无需在 Skill 内处理线程问题
- **Undo 支持**：Mutate 类操作自动注册到 Unity Undo 系统，支持编辑器 Ctrl+Z 撤销
- **事务管理**：可将多个操作组合为一个 Undo 分组，支持原子性提交或一键回滚
- **端口自动递增**：默认端口 8090，若被占用自动尝试 8091~8100
- **Editor 窗口**：`Tools → Agent Skills` 查看所有已注册接口及其类型（Query / Mutate）

## API 端点

### GameObject 操作

| 端点 | 方法 | 说明 |
|------|------|------|
| `/create_object` | POST | 创建基础几何体（Cube / Sphere / Capsule / Cylinder / Plane） |
| `/delete_object` | POST | 按名称或 instanceId 删除 GameObject |
| `/find_objects` | POST | 按名称、Tag 或组件类型查询场景中的 GameObject |
| `/move_object` | POST | 修改 GameObject 的位置、旋转或缩放 |

### 事务管理

| 端点 | 方法 | 说明 |
|------|------|------|
| `/begin_transaction` | POST | 开启 Undo 事务分组 |
| `/commit_transaction` | POST | 提交事务（封存 Undo 分组） |
| `/rollback_transaction` | POST | 回滚事务（撤销整组操作） |
| `/undo` | POST | 执行一步 Undo（等同 Ctrl+Z） |

### 请求示例

**创建一个红色 Cube：**
```json
POST http://localhost:8090/create_object
{
  "name": "RedCube",
  "type": "Cube",
  "x": 0, "y": 1, "z": 0,
  "scaleX": 1, "scaleY": 1, "scaleZ": 1,
  "colorR": 1.0, "colorG": 0.0, "colorB": 0.0
}
```

**查询场景中所有带 Light 组件的物体：**
```json
POST http://localhost:8090/find_objects
{
  "componentType": "Light"
}
```

**移动物体：**
```json
POST http://localhost:8090/move_object
{
  "name": "RedCube",
  "x": 3, "y": 0, "z": 2
}
```

**统一响应格式：**
```json
{ "success": true, "name": "RedCube" }
{ "success": false, "error": "找不到 GameObject: name=\"Foo\"" }
```

## 扩展：添加自定义 Skill

只需在任意 C# 静态类中添加带 `[AgentSkill]` 属性的静态方法：

```csharp
public static class MySkills
{
    [AgentSkill("my_action", SkillType.Mutate, description: "执行自定义操作")]
    public static SkillResponse MyAction(SkillRequest request)
    {
        // request.Body 包含 JSON 请求体
        // 直接在这里写 Unity API 调用，已在主线程
        return SkillResponse.Ok("{\"success\":true}");
    }
}
```

服务器下次启动（或 Domain Reload 后）会自动发现并注册该端点。

## 项目结构

```
Assets/Scripts/
├── Runtime/
│   ├── SkillsHttpServer.cs         # HTTP 服务器 & 主线程调度
│   ├── AgentSkillRegistry.cs       # 反射扫描 & 路由注册
│   ├── AgentTransactionManager.cs  # Undo 事务分组管理
│   ├── AgentSkillAttribute.cs      # [AgentSkill] 属性定义
│   ├── SkillRequest.cs             # 请求上下文封装
│   ├── SkillResponse.cs            # 响应结果封装
│   └── Skills/
│       ├── GameObjectSkills.cs     # create_object / delete_object
│       ├── QueryObjectSkills.cs    # find_objects
│       └── TransformSkills.cs      # move_object
└── Editor/
    ├── AgentSkillsWindow.cs         # Editor 窗口（Tools/Agent Skills）
    └── SkillsHttpServerReloadHandler.cs  # Domain Reload 后自动重启服务器
```

## 技术栈

- **Unity** 2022.x+（Editor 模式）
- **C# / .NET** — `System.Net.HttpListener` 实现 HTTP 服务器
- **Newtonsoft.Json** — 请求/响应 JSON 序列化
- **Odin Inspector**（可选）— Editor 窗口 UI 增强

## 快速开始

1. 将 `Assets/Scripts` 导入 Unity 项目
2. 在 Unity Editor 中打开 `Tools → Agent Skills`
3. 点击 **Start Server** 启动服务器（默认 `http://localhost:8090`）
4. 使用 HTTP 客户端或 AI Agent 调用上述 API

服务器在 Play Mode 进入/退出和 Domain Reload 后会自动重启。
