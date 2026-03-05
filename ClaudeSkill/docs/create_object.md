# create_object

在场景中创建基本几何体。

## 参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `name` | string | `"NewObject"` | GameObject 名称 |
| `primitive_type` | string | `"Cube"` | 形状：`Cube`、`Sphere`、`Capsule`、`Cylinder`、`Plane` |
| `x` | float | `0` | 世界坐标 X |
| `y` | float | `0` | 世界坐标 Y |
| `z` | float | `0` | 世界坐标 Z |
| `scale_x` | float | `1.0` | X 轴缩放 |
| `scale_y` | float | `1.0` | Y 轴缩放 |
| `scale_z` | float | `1.0` | Z 轴缩放 |
| `color_r` | float | `-1.0` | 红色分量（0~1），`-1` 表示不设置颜色 |
| `color_g` | float | `-1.0` | 绿色分量（0~1），`-1` 表示不设置颜色 |
| `color_b` | float | `-1.0` | 蓝色分量（0~1），`-1` 表示不设置颜色 |

## 返回值

成功：

```json
{"success": true, "name": "MyCube"}
```

失败：

```json
{"success": false, "error": "无法连接到 Unity 服务器（端口 8090~8100 均无响应）"}
```
