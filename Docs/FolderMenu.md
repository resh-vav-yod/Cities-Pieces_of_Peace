# 文件目录
中文 | [English](./en/FolderMenu_en.md) | [日本語](./jp/FolderMenu_jp.md) | 

---

## 总览
> [!IMPORTANT]
> *总览目录* 仅用于直观显示文件结构（精确到文件类型）。  
> 不包含详细介绍和跳转链接，此部分参见下方 **目录** 章节。  

> [!TIP]
> 为保障阅读连贯性，故对 *总览目录* 进行折叠。点击下方 `Drop!` 展开目录。   

<details>
<summary><b>Drop!</b></summary>
  
```text
Cities-Pieces_of_Peace/        #root
  Docs/
    FolderMenu.md
    GDD/
      CoreLoop.md
      DesignPillars.md
    Tech/
      DataSchema.md
      MapPipeline.md
      SaveLoad.md
      BuildRelease.md
    Logs/
      Devlog.md
      Changelog.md

  ExternalData/
    Map/
      Source/
        world_source.geojson
      Generated/
        id_map.png
        border_map.png
        preview_map.png
        cells.generated.json
      Gameplay/
        cells.values.json
        groups.json
    Gameplay/
      factions.json
      units.json
      buildings.json
      scenarios.json
      rulesets.json
      localization.zh-Hans.json
    Saves/
      save_001.json

  UnityProject/
    Assets/
      Art/
        Models/
        Textures/
        Materials/
        VFX/
      Audio/
        Music/
        Sound/
      Code/
        Scripts/
          Runtime/
            Core/
            Data/
            Map/
            Battle/
            Economy/
            Scenario/
            Save/
            UI/
            Utilities/
          Editor/
            Importers/
            Inspectors/
            Windows/
            Validators/
          Tests/
            EditMode/
            PlayMode/

      Data/
        Imported/
        RuntimeCache/
      Level/
        Prefabs/
          Units/
          Buildings/
          UI/
          World/
        Scenes/
          Boot/
          Globe/
          Battle/
          Sandbox/
      
      Addressables/
    Packages/
    ProjectSettings/
```

</details>

---

## 目录 <a id="0"></a>

> [!TIP]
> 为轻松熟悉文件结构，点击下方 `Drop!` 可观看文件夹分类示例。   

<details>
<summary><b>Drop!</b></summary>

`Cities-Pieces_of_Peace/` **项目根目录**  

**独立文件根目录**
- 区分文件夹
  - 类型文件夹
    - 分类文件夹 *（数字编号仅出现在对应详细页面）*
      1. 具体文件夹A
      2. 具体文件夹B

</details>

[Docs](#1) 
  - [ArtBook](#11) 
  - [Design](#12) 
    - [GDD](#121) 
    - [TDD](#122) 
  - [Logs](#13) 
    - [DevLog](#131) 
    - [WorkLog](#132) 

[ExternalData](#2) 
  - [Map](#21) 
    - [Source](#211) 
    - [Generated](#212) 
      - [id_map.png](#2121)
      - [border_map.png](#2122)
      - [cells.generated.json](#2123)
    - [Gameplay](#213) 
  - [Gameplay](#22) 

[UnityProject](#3) 
  - [Assets](#31)
    - [Art](#311)
      - [Materials](#3111) 
      - [Models](#3112) 
      - [Textures](#3113) 
    - [Audio](#312)
      - [Music](#3121) 
      - [Sound](#3122) 
    - [Code](#313)
      - [Editor](#3131) 
      - [PlayerEditor](#3132) 
      - [Scripts](#3133) 
        1. [Client](#31331) 
        2. [Runtime](#31332) 
           1. Simulation
           2. logic
        3. [Server](#31333) 
        4. [Share](#31334) 
      - [Shader](#3134) 
      - [Tests](#3135) 
    - [Data](#314)
      - [Import](#3141) 
    - [Level](#315)
      - [Prefabs](#3151) 
      - [Scenes](#3152) 
      - [UI](#3153) 
    - [Other](#316)

  - [Packages](#32)
  - [ProjectSettings](#33)

---
## Docs <a id="1"></a>

### ArtBook <a id="11"></a>

### Design <a id="12"></a>

#### 1. GDD <a id="121"></a>
##### 1.1  <a id="1211"></a>
##### 1.2  <a id="1212"></a>
##### 1.3  <a id="1213"></a>

#### 2. Tech <a id="122"></a>
##### 2.1  <a id="1221"></a>

### Logs <a id="13"></a>

#### 1. Devlog <a id="131"></a>

#### 2. Worklog <a id="122"></a>

###### [*回到目录*](#0)

---
## ExternalData <a id="2"></a>

### Map <a id="21"></a>

#### 1. Source <a id="211"></a>
##### 1.1  <a id="2111"></a>

#### 2. Generated <a id="212"></a>
##### 2.1 id_map.png <a id="2121"></a>
##### 2.2 border_map.png <a id="2212"></a>
##### 2.3 cells.generated.json <a id="2213"></a>

#### 3. Gameplay <a id="213"></a>

### Gameplay <a id="22"></a>

###### [*回到目录*](#0)

---
## UnityProject <a id="3"></a>

### Assets <a id="31"></a>

#### 1. Art <a id="311"></a>
##### 1.1 Materials <a id="3111"></a>
##### 1.2 Models <a id="3112"></a>
##### 1.3 Textures <a id="3113"></a>

#### 2. Audio <a id="312"></a>
##### 2.1 Music <a id="3121"></a>
##### 2.2 Sound <a id="3122"></a>

#### 3. Code <a id="313"></a>
##### 3.1 Editor <a id="3131"></a>
##### 3.2 PlayerEditor <a id="3132"></a>
##### 3.3 Scripts <a id="3133"></a>
###### 3.3.1 Client <a id="31331"></a>
纯客户端，比如视角操控、渲染质量
###### 3.3.2 Runtime <a id="31332"></a>
玩家看到的，如单位。具体内容看game manager。
###### 3.3.3 Server <a id="31333"></a>
纯服务器，比如校准时间，以及未来双包的log等内容
###### 3.3.4 Share <a id="31334"></a>
##### 3.4 Shader <a id="3134"></a>
##### 3.5 Tests <a id="3135"></a>

#### 4. Data <a id="314"></a>
##### 4.1 Import <a id="3141"></a>

#### 5. Level <a id="315"></a>
##### 5.1 Prefabs <a id="3151"></a>
##### 5.2 Scenes <a id="3152"></a>
##### 5.3 UI <a id="3153"></a>

#### 6. Other <a id="316"></a>

### Packages <a id="32"></a>

### ProjectSettings <a id="33"></a>

###### [*回到目录*](#0)

---

##

