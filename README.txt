# 🧾 **Upzy 热更架构总结（当前落地版）**

## 📂 目录划分

```
Assets/Bighead/Upzy/
  Core/                ← UpzySetting、UpzyModuleSO、UpzyBuildableBase 等核心类
  Editor/              ← UpzyConfigProvider、UpzyMenuBuilder 等编辑器逻辑
  Modules/             ← 各模块的 ScriptableObject 配置
Assets/UpzyGenerated/
  current/             ← 当前生效版本
    Menu.bd
    Modules/
      <Module>.bd      ← 每个模块的配置快照
  backup/              ← 上一次版本的备份
```

---

## 🏗 核心类

### `UpzySetting`（ScriptableObject）

* `rootFolder`、`currentRel`、`backupRel`、`modulesRel`
* `registeredModules: List<UpzyEntry>`
  包含所有已接入模块的配置 `UpzyModuleSO`

### `UpzyModuleSO`

* 用户自定义字段（可扩展）
* `ConfigVersion version` → 每次构建有变化时递增

### `UpzyBuildableBase`

* `SetConfig(UpzyModuleSO so)` 注入配置
* `Build(string outputRoot): BuildResult` → 生成产物，返回变更等级和文件列表

### `BuildResult`

* `ChangeLevel changeLevel`（None / Patch / Feature / Minor / Major）
* `string aggregateHash`（配置 + 产物 Hash）
* `List<BuildEntry>`（文件名、相对路径、hash、大小）

---

## 🔧 UpzyMenuBuilder 核心方法

* `BuildModule(setting, so)`
  构建单个模块 → 如果有变化 → 递增模块版本号 → 写 `current/Modules/*.bd`
* `BuildAll(setting)`
  遍历所有模块调用 `BuildModule`
* `Publish(setting, isFullBuild)`
  读取所有模块版本 + builtin 版本

  * 如果 X.Y.Z 或任意模块版本更新 → 递增 W → 重写 Menu.bd

---

## 🖥 UpzyConfigProvider (Project Settings)

* **顶部按钮：**

  * `构建全部模块` → 调用 `BuildAll`
  * `发版（Full）` / `发版（Incremental）` → 调用 `Publish`
* **模块卡片：**

  * 展示 `UpzyModuleSO` 的所有字段
  * `构建该模块` → 调用 `BuildModule`

---

## ✅ 关键设计原则

* **傻瓜化**：用户不需要勾选模块，接入即维护
* **实时性**：构建即更新模块版本，无需 staging
* **一致性**：发版时统一比对，避免 Menu 与模块版本不匹配
* **可扩展**：模块配置自由扩展，基类可 override OnBuild
