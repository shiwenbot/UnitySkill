# move_object

修改指定 GameObject 的 Transform（位置、旋转、缩放）。按名称或 instanceId 定位物体，只更新提供的字段（None 表示不修改）。

**优先用 `instance_id` 定位**，避免同名物体歧义。`instanceId` 由 `find_objects` 返回。

## 参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `name` | string | `""` | GameObject 名称，与 `instance_id` 二选一 |
| `instance_id` | int | `-1` | `find_objects` 返回的 instanceId，优先于 name |
| `x` | float \| None | `None` | 世界坐标 X，`None` 表示不修改 |
| `y` | float \| None | `None` | 世界坐标 Y，`None` 表示不修改 |
| `z` | float \| None | `None` | 世界坐标 Z，`None` 表示不修改 |
| `rot_x` | float \| None | `None` | 世界旋转欧拉角 X，`None` 表示不修改 |
| `rot_y` | float \| None | `None` | 世界旋转欧拉角 Y，`None` 表示不修改 |
| `rot_z` | float \| None | `None` | 世界旋转欧拉角 Z，`None` 表示不修改 |
| `scale_x` | float \| None | `None` | X 轴缩放，`None` 表示不修改 |
| `scale_y` | float \| None | `None` | Y 轴缩放，`None` 表示不修改 |
| `scale_z` | float \| None | `None` | Z 轴缩放，`None` 表示不修改 |

## 返回值

成功：

```json
{
  "success": true,
  "name": "Cube",
  "position": {"x": 5.0, "y": 0.0, "z": 0.0},
  "rotation": {"x": 0.0, "y": 45.0, "z": 0.0},
  "scale":    {"x": 1.0, "y": 1.0,  "z": 1.0}
}
```

失败（找不到物体）：

```json
{"success": false, "error": "找不到 GameObject: name=\"NonExistent\""}
```

失败（无法连接）：

```json
{"success": false, "error": "无法连接到 Unity 服务器（端口 8090~8100 均无响应）"}
```
