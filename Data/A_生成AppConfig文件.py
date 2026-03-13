#! /usr/bin/env python3
# -*- coding: utf-8 -*-
import shutil

import json
import os
from pathlib import Path

# 获取当前脚本所在目录（即 Data 文件夹）
current_dir = Path(__file__).parent.absolute()

# 代理配置文件目录
agent_config_dir = current_dir / "AgentConfig"

# 代理配置文件目录
app_config_dir = current_dir / "AppConfig"


def process_config(dir_name):
    # 只保留dir_name中的数字，删除其他字符
    agent_id = int("".join(filter(str.isdigit, dir_name)))
    # print(f"Agent {agent_id}:")

    xieyi_data = ""
    yinsi_data = ""

    xieyi_file = agent_config_dir / dir_name / "xieyi.txt"
    if not xieyi_file.exists():
        print(f"配置文件 {xieyi_file} 不存在")
    else:
        with open(xieyi_file, 'r', encoding='utf-8') as f:
            xieyi_data = f.read()

    yinsi_file = agent_config_dir / dir_name / "yinsi.txt"
    if not yinsi_file.exists():
        print(f"配置文件 {yinsi_file} 不存在")
    else:
        with open(yinsi_file, 'r', encoding='utf-8') as f:
            yinsi_data = f.read()

    config_file = agent_config_dir / dir_name / "Config.json"
    if not config_file.exists():
        print(f"配置文件 {config_file} 不存在")
    else:
        with open(config_file, 'r', encoding='utf-8') as f:
            config_data = json.load(f)

    old_app_config = config_data.get("AppConfig", {})
    # 构造新的配置数据
    app_config = {
        "Agent": agent_id,
        "UnderReviewVersion": old_app_config.get("UnderReviewVersion", ""),
        "UnderReviewVersionMacOS": old_app_config.get("UnderReviewVersionMacOS", ""),
        "WebABUrl": old_app_config.get("WebABUrl", ""),
        "IsDebug": old_app_config.get("IsDebug", False),
        "ApiUrlList": old_app_config.get("ApiUrlList", [
            "https://jaserver.bocew.com"
        ]),
        "ServiceAgreement": xieyi_data.replace('\ufeff', ''),
        "PrivatePolicy": yinsi_data.replace('\ufeff', '')
    }
    # 在json文件夹内创建目标json文件
    output_file = app_config_dir / dir_name / "AppConfig.json"
    os.makedirs(app_config_dir / dir_name, exist_ok=True)
    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(app_config, f, indent=4, ensure_ascii=False)

    # 将 agent_config_dir / dir_name / "CustomerService.png " 复制到 app_config_dir / str(agentId)
    if os.path.exists(agent_config_dir / dir_name / "CustomerService.png"):
        shutil.copy(agent_config_dir / dir_name / "CustomerService.png", app_config_dir / dir_name)


# 遍历处理所有代理的配置文件
for agent_dir in agent_config_dir.iterdir():
    if agent_dir.is_dir():
        process_config(agent_dir.name)
