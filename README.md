# 🌍 城：散装和平 (Cities: Pieces of Peace)

![Unity](https://img.shields.io/badge/Unity-2022.3.62f3_LTS-000000?style=flat&logo=unity)
![Status](https://img.shields.io/badge/Status-Prototyping-blue)
![Platform](https://img.shields.io/badge/Platform-Windows_%7C_macOS-lightgrey)

> [!IMPORTANT]
> **研发背景：** 本项目是为参加 2026 年 5 月的开发比赛而构建的垂直切片，同时作为申请 **Technical Artist (TA)** 与 **游戏测试工程师** 暑期实习的技术展示组合（Portfolio）。项目重点展示自动化地理数据管线、数据驱动架构及服务端权威的网络同步层。

## 📑 目录
* [项目综述](#项目综述)
* [技术地基](#技术地基)
* [项目规范](#项目规范)
* [开发里程碑](#开发里程碑)
* [联系作者](#联系作者)

---

## 🚀 项目综述 <a id="项目综述"></a>

《城：散装和平》是一款探索宏观大地图交互与局部 RTS 战斗无缝切换的多人策略游戏原型。本项目放弃了传统的纯场景内手动拼写，采用高度数据驱动的模式，旨在解决大规模区块状态管理与网络同步的痛点。

---

## 🛠 技术地基 <a id="技术地基"></a>

### 1. 工业级数据管线 (Map Pipeline)
本项目抛弃了高耗能的 Mesh 拼接，自研了一套基于地理信息的自动化地图处理流：

* **离线自动化转换：** 使用 Python 脚本解析 `GeoJSON`，同步生成用于程序识别的 **ID Map** 和用于渲染的 **Border Map**。
* **运行时高性能拾取：** <details>
<summary><b>点击查看拾取算法核心思路</b></summary>

通过射线检测获取球体的 UV 坐标，直接在内存中采样 `id_map.png` 的像素，并将其 RGB 值解码为 `cellId`。
该方案将大地图交互的时间复杂度从 $O(N)$ 降低至 $O(1)$，极大地优化了性能。
</details>

### 2. 数据驱动架构 (Data-Driven Design)
为了支持未来的玩家自定义 (Modding) 及高效率测试验证，玩法逻辑与静态数值完全解耦：

* **JSON Schema 协议：** 建立严密的配置协议，涵盖单位 (Units)、区块 (Cells)、规则集 (Rulesets) 等 8 大核心数据表。
* **代码物理隔离：** 脚本严格划分为 `Simulation` (纯规则)、`Network` (同步)、`Client` (表现) 三层，确保核心逻辑独立运转，为未来向 Dedicated Server 迁移打下基础。

### 3. 服务端权威网络同步 (Server-Authoritative Networking)
* **技术栈：** `Mirror` + `FizzySteamworks` (Steam P2P/Relay)。
* **防作弊设计：** 客户端仅拥有发送“操作意图 (Command)”的权限。服务端负责校验资源、视线与逻辑合法性，裁决后通过 `SyncVar` 和 `ClientRpc` 广播状态，从架构层面杜绝数值篡改。

---

## 📂 项目规范 <a id="项目规范"></a>

* **版本控制：** 采用 `Git LFS` 策略，对 `.blend`、`.fbx` 及大型贴图进行二进制轨道管理。
* **三分离原则：** 目录严格遵循 `Source` (源文件) / `Generated` (脚本生成物) / `Gameplay` (手填配置) 三分离，确保生成数据随时可被一键重建。

---

## 🗺 开发里程碑 <a id="开发里程碑"></a>

| 版本阶段 | 核心目标 (Definition of Done) | 预计交付 |
| :--- | :--- | :--- |
| **v0.1 (Competition Demo)** | **垂直切片闭环：** 完成大地图选区 -> 局部战场 -> 战斗判定 -> 结果回写。 | 2026-05-09 |
| **v0.5 (Framework Build)** | **架构成型：** 完整的数据驱动加载、Authoring Tool 原型及联机框架。 | 2026-08-24 |

---

## 👨‍💻 联系作者 <a id="联系作者"></a>

* **Developer:** 肖云浩 (Xiao Yunhao)
* **Amateur Radio Callsign:** BI9CLY

> [!TIP]
> 欢迎对技术美术管线建设或游戏自动化测试感兴趣的同行交流讨论。如需查看核心数据结构的定义协议，请参阅 `Docs/DataSchema.md` (建设中)。