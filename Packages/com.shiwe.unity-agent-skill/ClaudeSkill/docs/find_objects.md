# find_objects

按条件查找场景中的 GameObject，至少提供一个过滤条件（name、tag、component_type 三选一或多选）。

## 参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `name` | string \| None | `None` | 名称子串，大小写不敏感，如 `"Cube"` |
| `tag` | string \| None | `None` | Unity Tag 精确匹配，如 `"Player"`、`"MainCamera"` |
| `component_type` | string \| None | `None` | 组件类型名，如 `"Light"`、`"Camera"`、`"Rigidbody"` |
| `include_inactive` | bool | `False` | 是否包含未激活的 GameObject |
| `limit` | int | `100` | 最大返回数量 |

## 返回值

成功：

```json
{
  "success": true,
  "count": 1,
  "objects": [
    {
      "name": "Main Camera",
      "tag": "MainCamera",
      "active": true,
      "instanceId": 12345,
      "position": {"x": 0.0, "y": 1.0, "z": -10.0},
      "components": ["Transform", "Camera", "AudioListener"]
    }
  ]
}
```

失败（未提供过滤条件）：

```json
{"success": false, "error": "至少需要提供一个过滤条件：name、tag 或 component_type"}
```

失败（无法连接）：

```json
{"success": false, "error": "无法连接到 Unity 服务器（端口 8090~8100 均无响应）"}
```
