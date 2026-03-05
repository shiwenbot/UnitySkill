#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# unity_connection.py - AgentSkill 项目的共享连接层

# Windows 控制台编码修复（必须在最前面）
import sys
if sys.platform == 'win32':
    import codecs
    if hasattr(sys.stdout, 'buffer'):
        sys.stdout = codecs.getwriter('utf-8')(sys.stdout.buffer, 'replace')
    if hasattr(sys.stderr, 'buffer'):
        sys.stderr = codecs.getwriter('utf-8')(sys.stderr.buffer, 'replace')

import requests
import time

PORT_RANGE_START = 8090
PORT_RANGE_END = 8100
DEFAULT_TIMEOUT = 10


def find_server_port() -> int:
    """扫描 8090~8100，返回第一个响应 /ping 的端口，未找到返回 -1"""
    for port in range(PORT_RANGE_START, PORT_RANGE_END + 1):
        try:
            resp = requests.get(f"http://localhost:{port}/ping", timeout=1)
            if resp.status_code == 200:
                return port
        except Exception:
            pass
    return -1


def is_unity_running() -> bool:
    """扫描端口范围，检查 Unity HTTP 服务器是否正在运行"""
    return find_server_port() != -1


def wait_for_unity(timeout: float = 15.0, check_interval: float = 1.0) -> bool:
    """等待 Unity 服务器上线，适用于 Domain Reload 后的重连"""
    deadline = time.time() + timeout
    while time.time() < deadline:
        if is_unity_running():
            return True
        time.sleep(check_interval)
    return False
