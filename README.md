# 城：散装和平 (Cities: Pieces of Peace)

![Unity](https://img.shields.io/badge/Unity-2022.3.62f3_LTS-000000?style=flat&logo=unity)
![Status](https://img.shields.io/badge/Status-Prototyping-blue)
![Platform](https://img.shields.io/badge/Platform-Windows_%7C_macOS-lightgrey)

> [!IMPORTANT]
> 项目制作开始于3月初，合计约60小时。

## 目录
* [背景](#1)
* [游戏性](#2)
* [文件结构](#3)
* [技术简介](#4)
* [开发里程碑](#5)

---
## 背景 <a id="1"></a>
《城：散装和平》属于 *城（City）* 世界观中的一个部分。用于介绍21世纪50年代的宏观情况。

---
## 游戏性 <a id="2"></a>
根据时间/发展渐进 `放大视野` 和 `关闭权限`
| 阶段 | 视野 | 权限 |
| :---: | ---: | :--- |
| 开局 | 首都 | 建筑建设 / 科技发展 |
| 前期 | 接壤地块 | RTS的资源运营 |
| 中期 | 地球表面 | RTS的战斗 / 情报侦查 / 合并地块创建AI副官 |
| 后期 | 地球 | 外交 / 战略打击 |
| 结束 | 结算画面 | 根据地块 *合并* 或 *损毁* 大小判断胜利 |

特色：
- 有 *即时通信* 的地块（比如无线电塔）才能切换至RTS小地块。
- 地块发展到一定程度可以合并为 *AI副官* ，此时失去地块的小场景并生成一个属于AI的 *核心地块场景* 。
    - 副官可以帮助玩家前线作战。（宏观）
    - 玩家可以通过摧毁核心地场景块让AI失能。（微观）
- 玩家可以通过宏观界面的 *工厂* （有数量限制）生产单位，并指派到各处地块场景内。

---
## 文件结构 <a id="3"></a>
```
#当前架构
    Assets/
    ├── Art/
    ├── Audio/
    ├── Data/
    ├── Mirror/
    ├── Prefabs/
    ├── Scenes/
    ├── Scripts/
    ├── ScriptTemplates/
    ├── Settings/
    ├── StreamingAssets/
    ├── test/
    ├── TextMesh Pro/
    └── UI/
```
* **版本控制：** `GitHub` + `Git LFS`

---
## 技术简介 <a id="4"></a>

### 1）地图
地球地图采用 Python 脚本将 GeoJSON 转化为 `id_map.png` + `border_map.png` + `cells.generated.json` ，分别用于 *颜色采样* 、 *显示边界* 、 *识别区块* 。

### 2）数据
- **采用三分离原则**： `Source` / `Generated`  / `Gameplay` 有助于后续开发 *地图编辑器* 开发给玩家自定义。
- **JSON Schema 协议：** 建立严密的配置协议，涵盖单位 (Units)、区块 (Cells)、规则集 (Rulesets) 等 8 大核心数据表。
- **代码物理隔离：** 脚本严格划分为 `Simulation` (纯规则)、`Network` (同步)、`Client` (表现) 三层，确保核心逻辑独立运转，为未来向 Dedicated Server 迁移打下基础。

### 3）网络
采用 `Mirror` 和 `FizzySteamworks` 包用于 *同步* 和 *传输* ，但还缺少具体的游戏网络层。

### 4）场景
由于除了宏观（地球）场景以外，还有微观（地块RTS）场景。考虑到性能问题，计划在玩家不存在的场景做 *销毁* 处理，并转化为服务器的纯数据模式。

---
## 开发里程碑 <a id="5"></a>
| 版本阶段 | 核心目标 (Definition of Done) | 进度 |
| :--- | :--- | :--- |
| **(Competition Demo)** | **垂直切片闭环：** 完成大地图选区 -> 局部战场 -> 战斗判定 -> 结果回写。 | ✅ |

<details>
<summary><b>test</b></summary>
</details>

> [!TIP]
> 如需查看核心数据结构的定义协议，请参阅 `Docs/DataSchema.md` (建设中)。