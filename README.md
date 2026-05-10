# 🏡 AlmondHousing 杏仁房屋工具 (深度重制多语言版) 🛠️

### 🛑 严正声明：倒卖狗退散 🛑

**本插件 100% 完全免费！开源！禁止任何形式的售卖！**
如果你是在闲鱼、淘宝或付费群买到的，请立刻**申请退款并反手举报**！
拿着开源社区的成果去牟利，**祝闲鱼倒卖狗买泡面永远没调料包，装修房子永远对不齐坐标！** 🤬
**口号：知识自由，白嫖万岁！抵制倒卖，从我做起！**

---

## ✨ 简介 (Introduction)

**AlmondHousing (杏仁房屋工具)** 是基于 [DisPlace](https://github.com/Drakansoul/DisPlace) 深度重构的 FF14 房屋装修增强插件，由 **AlmondCookie** 独立维护。

相较于原版，我们进行了“脱胎换骨”的升级：

* 🌍 **全球首发 6 国语言支持**：支持 中（简/繁）、英、日、韩、德、法 界面秒切，国际服/国服玩家无缝通用。
* 📊 **高级清单导出**：支持一键导出 **Teamcraft 导入文本** 或 **多语言 CSV 购物清单**，装修采购不再头大。
* 🏗️ **跨平台兼容**：完美支持 [MakePlace](https://jawslouis.itch.io/makeplace) 和 [ReMakePlace](https://github.com/RemakePlace/app) 的 `.json` 装修图纸。

---

## 🚀 如何安装 (Installation)

1. **前提**：你必须使用 [卫月框架 (Dalamud/FFXIV Quick Launcher)](https://goatcorp.github.io/) 启动游戏。
2. 在游戏聊天框输入 `/xlsettings`。
3. 点击 **“实验性功能 (Experimental)”** 标签。
4. 在 **“自定义插件仓库”** 中粘贴 AlmondCookie 专属订阅链接：
```text
https://raw.githubusercontent.com/hyirax/AlmondHousing/master/pluginmaster.json

```


5. 点击 `+` 号并确保勾选 **“启用 (Enabled)”**，点击 **“保存并关闭”**。
6. 在 `/xlplugins` 插件大厅搜索 **`AlmondHousing`** 即可一键安装。

---

## 🎮 核心功能 (Features)

在聊天框输入 **`/almond`** 唤出控制台。

### 🌍 语言热切换

在面板顶部左侧，你可以随时切换界面语言。右侧则是“导出语言”——你可以用中文界面控制，导出一份日文或英文的清单给跨服好友或 Teamcraft。

### 💾 跨房“抄作业” (布局保存)

1. 溜进你喜欢的房子（确保开启了房屋内的 **旋转布局** 模式）。
2. 点击 **`另存为 (Save As)`**，别人的神仙装修坐标就是你的了！

### 🪄 自动归位 (布局应用)

1. 回到家，勾选 **目标楼层**。
2. **关键**：将图纸所需的家具提前从仓库**扔到地板上**（无需摆放，随便乱丢即可）。
3. 点击 **`读取自 (Load From)`** 载入图纸。
4. 勾选 **`应用布局 (Apply Layout)`**，双手离开键盘，见证家具自动飞向指定坐标的魔法！

---

## 🤔 常见问题 (FAQ)

* **Q: 导出 Teamcraft 识别失败？**
* **A:** 在导出前，请将“导出语言”下拉框切到 **English**，英文名称在 Teamcraft 上的识别率是 100% 的！


* **Q: 为什么家具点“布置”没反应？**
* **A:** 请检查你是否开启了系统的“旋转布局”模式（Rotate Furniture）。


* **Q: 使用安全吗？**
* **A:** 本插件仅进行本地坐标写入，属于辅助工具。但使用任何第三方插件均违反 SE 的 TOS，请**保持低调**，风险自担。


* **Q: 自动摆放时游戏崩溃？**
* **A:** 摆放期间**绝对不要动鼠标**！不要悬停在任何家具上！不要让人物挡住家具的移动路线！



---

## 💖 鸣谢 (Credits)

本项目由 **AlmondCookie** 在以下优秀开源项目的基础上重制：

* **[Drakansoul](https://github.com/Drakansoul/DisPlace)** (DisPlace 维护者)
* **[jawslouis](https://github.com/jawslouis)** (MakePlacePlugin 创造者)
* **[NotNite](https://github.com/NotNite)** (开源精神的捍卫者)
* 感谢 [HousingPos](https://github.com/Bluefissure/HousingPos), [BDTH](https://github.com/LeonBlade/BDTHPlugin) 等前辈项目提供的技术灵感。

---

**GitHub 仓库**: [hyirax/AlmondHousing](https://www.google.com/search?q=https://github.com/hyirax/AlmondHousing)

*如果觉得好用，请点一个 Star ⭐️，你的支持是我更新的最大动力！*