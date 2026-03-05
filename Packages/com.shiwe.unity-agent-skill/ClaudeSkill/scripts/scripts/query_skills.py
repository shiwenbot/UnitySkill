#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# query_skills.py - GameObject 查询类 skill

import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))

from unity_connection import find_server_port
from typing import Dict, Any, Optional

DEFAULT_TIMEOUT = 10


def find_objects(
    name: Optional[str] = None,
    tag: Optional[str] = None,
    component_type: Optional[str] = None,
    include_inactive: bool = False,
    limit: int = 100,
) -> Dict[str, Any]:
    """查询 Unity 场景中的 GameObject，至少需提供一个过滤条件。"""
    import requests

    if not name and not tag and not component_type:
        return {"success": False, "error": "至少需要提供一个过滤条件：name、tag 或 component_type"}

    port = find_server_port()
    if port == -1:
        return {
            "success": False,
            "error": "无法连接到 Unity 服务器（端口 8090~8100 均无响应），请检查是否已启动服务器",
        }

    body: Dict[str, Any] = {
        "includeInactive": include_inactive,
        "limit": limit,
    }
    if name:
        body["name"] = name
    if tag:
        body["tag"] = tag
    if component_type:
        body["componentType"] = component_type

    try:
        resp = requests.post(
            f"http://localhost:{port}/find_objects",
            json=body,
            timeout=DEFAULT_TIMEOUT,
        )
        resp.encoding = 'utf-8'
        return resp.json()
    except Exception as e:
        return {"success": False, "error": str(e)}
