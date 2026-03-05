#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# transform_skills.py - Transform 修改类 skill

import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))

from unity_connection import find_server_port
from typing import Dict, Any, Optional

DEFAULT_TIMEOUT = 10


def move_object(
    name: str = "",
    instance_id: int = -1,
    x: Optional[float] = None,
    y: Optional[float] = None,
    z: Optional[float] = None,
    rot_x: Optional[float] = None,
    rot_y: Optional[float] = None,
    rot_z: Optional[float] = None,
    scale_x: Optional[float] = None,
    scale_y: Optional[float] = None,
    scale_z: Optional[float] = None,
) -> Dict[str, Any]:
    """修改 Unity 场景中指定 GameObject 的 Transform。"""
    import requests

    port = find_server_port()
    if port == -1:
        return {
            "success": False,
            "error": "无法连接到 Unity 服务器（端口 8090~8100 均无响应），请检查是否已启动服务器",
        }

    body: Dict[str, Any] = {}
    if name:
        body["name"] = name
    if instance_id >= 0:
        body["instanceId"] = instance_id
    if x         is not None: body["x"]      = x
    if y         is not None: body["y"]      = y
    if z         is not None: body["z"]      = z
    if rot_x     is not None: body["rotX"]   = rot_x
    if rot_y     is not None: body["rotY"]   = rot_y
    if rot_z     is not None: body["rotZ"]   = rot_z
    if scale_x   is not None: body["scaleX"] = scale_x
    if scale_y   is not None: body["scaleY"] = scale_y
    if scale_z   is not None: body["scaleZ"] = scale_z

    try:
        resp = requests.post(
            f"http://localhost:{port}/move_object",
            json=body,
            timeout=DEFAULT_TIMEOUT,
        )
        resp.encoding = 'utf-8'
        return resp.json()
    except Exception as e:
        return {"success": False, "error": str(e)}
