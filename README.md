# 🏡 AlmondHousing 杏仁房屋工具

*🌐 Languages: **[简体中文](README.md)** | [繁體中文](docs/zh_TW/README.md) | [English](docs/en/README.md) | [日本語](docs/ja/README.md) | [한국어](docs/ko/README.md) | [Deutsch](docs/de/README.md) | [Français](docs/fr/README.md)*

---

### 🛑 严正声明：抵制倒卖，尊重原创 🛑

**本插件 100% 完全免费且开源！严禁任何形式的售卖！**

本工具的初衷是为了方便玩家**备份自己的装修**、**跨区共享免费图纸**。绝不容忍任何人使用本工具偷窃其他玩家、设计师的原创装修数据，并用于接单、倒卖或任何商业牟利行为！
如果您是在任何付费渠道获取到的本插件，或发现有人利用本插件盗卖图纸，请立刻**申请退款并反手举报**！

**知识自由，尊重原创！抵制倒卖，从我做起！**

---

### ✨ 简介

**AlmondHousing** 是基于 [DisPlace](https://github.com/Drakansoul/DisPlace) 深度重构的 FF14 房屋装修增强插件，由 **AlmondCookie** 独立维护。致力于让复杂的房屋装修、材料盘点与数据分享变得更加极客与纯粹。

**核心特性：**
* 🌍 **多语言无缝切换**：原生支持 中文（简/繁）、英语、日语、韩语、德语、法语，满足国际服/国服全环境需求。
* 📦 **智能材料盘点**：全自动扫描玩家背包与陆行鸟鞍包，实时对比当前图纸，精准计算家具“缺口”。
* 📊 **高级清单导出**：一键导出剔除了已有家具的 **Teamcraft 采购单**，或带有详细余缺数据的 CSV 表格。
* 🏗️ **图纸兼容与保护**：完美向下兼容读取 MakePlace 和 ReMakePlace 的 `.json` 图纸；同时采用 ChaCha20-Poly1305 算法支持保存为专属 `.almond` 加密格式，防篡改、防盗用。

---

### 🚀 如何安装

1. **前提**：使用 [卫月框架 (Dalamud / FFXIV Quick Launcher)](https://goatcorp.github.io/) 启动游戏。
2. 在游戏聊天框输入 `/xlsettings` 打开卫月设置。
3. 切换至 **“实验性功能 (Experimental)”** 标签卡。
4. 在 **“自定义插件仓库”** 中添加以下链接：
   `https://raw.githubusercontent.com/hyirax/AlmondHousing/master/pluginmaster.json`
5. 点击 **+** 号并勾选 **“启用 (Enabled)”**，然后点击“保存并关闭”。
6. 在 `/xlplugins` 插件大厅搜索 **`AlmondHousing`** 即可安装。

---

### 🎮 快速指南

在游戏聊天框输入 **`/almond`** 唤出控制台。

1. **保存布局**：进入您拥有权限的房屋，开启自带的“旋转布局”模式后，点击 `另存为 (Save As)` 即可备份当前房屋数据。
2. **材料准备**：点击 `材料盘点 (Material Audit)` 选项卡，查看所缺家具，并导出清单进行精准采购。
3. **应用布局**：将图纸所需的家具放在背包或放置在地板上，点击 `选择文件并应用 (Select File & Apply)`，插件将自动完成摆放。

*(注意：自动摆放期间请勿移动鼠标或让人物挡住家具移动路线，以免引发游戏崩溃。)*

---

### 💖 鸣谢

本项目在以下优秀开源项目的基础上重构与优化，特此致敬：

* **Drakansoul**：感谢创建并维护了 DisPlace。
* **NotNite**：感谢将 MakePlace 插件重新开源。
* **jawslouis**：感谢制作了非常出色的 MakePlacePlugin 插件。
* 感谢 **HousingPos**, **BDTH** 以及 **HouseMate** 等前辈项目提供的技术灵感。

---
*如果您觉得本插件有帮助，请为这个项目点一个 ⭐️ Star！*