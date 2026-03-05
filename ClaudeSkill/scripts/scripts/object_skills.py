#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# object_skills.py - GameObject 创建类 skill

import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))

from unity_connection import find_server_port
from typing import Dict, Any

DEFAULT_TIMEOUT = 10


def create_object(
    name: str = "NewObject",
    primitive_type: str = "Cube",
    x: float = 0,
    y: float = 0,
    z: float = 0,
    scale_x: float = 1.0,
    scale_y: float = 1.0,
    scale_z: float = 1.0,
    color_r: float = -1.0,
    color_g: float = -1.0,
    color_b: float = -1.0,
) -> Dict[str, Any]:
    """在 Unity 场景中创建 GameObject。"""
    import requests

    port = find_server_port()
    if port == -1:
        return {
            "success": False,
            "error": "无法连接到 Unity 服务器（端口 8090~8100 均无响应），请检查是否已启动服务器",
        }
    try:
        body = {
            "name": name,
            "primitiveType": primitive_type,
            "x": x,
            "y": y,
            "z": z,
            "scaleX": scale_x,
            "scaleY": scale_y,
            "scaleZ": scale_z,
            "colorR": color_r,
            "colorG": color_g,
            "colorB": color_b,
        }
        resp = requests.post(
            f"http://localhost:{port}/create_object",
            json=body,
            timeout=DEFAULT_TIMEOUT,
        )
        resp.encoding = 'utf-8'
        return resp.json()
    except Exception as e:
        return {"success": False, "error": str(e)}
