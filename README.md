# WeChatPrivacySkin

中文 | [English](#english)

一个不修改微信本体的 Windows 伴随工具，用外层窗口为微信提供隐私守护、沉浸皮肤和后台管理平台。

## 功能

- 托盘常驻：
  - 右键托盘图标打开管理平台。
  - 双击托盘图标开启/关闭隐私守护。
  - 按住 `Shift` 右键可打开退出兜底菜单。
- 隐私场景：
  - 日常保护：当前微信窗口可见，其他微信窗口脱敏。
  - 会议共享：强化遮罩，并提示单窗口共享的保护边界。
  - 离席保护：遮住所有微信窗口。
  - 快速净屏：一键遮住所有微信窗口，再按一次回到日常保护。
  - 专注聊天：只保留当前聊天窗口可见。
- 全局快捷键：
  - `Ctrl + Alt + P`：开启/关闭隐私守护。
  - `Ctrl + Alt + T`：切换主题皮肤。
  - `Ctrl + Alt + L`：快速净屏。
- 主题皮肤：
  - 内置办公浅色、夜间深色、低调隐私、可爱便签、动漫霓虹、像素游戏、纸感手账。
  - 支持外扩边框、贴纸角标、柔光轮廓、背景图片和遮罩纹理。
- 窗口覆盖：
  - 识别微信主窗口/独立聊天、文件传输、图片预览、搜索、收藏、转发、登录、通知和其他弹窗。
- 配置保存在 `%APPDATA%\WeChatPrivacySkin\settings.json`，旧版配置会自动迁移。

## 构建

本项目使用 WPF，目标框架为 `net10.0-windows`。

```powershell
cd D:\WeChatPrivacySkin
.\Build-Release.ps1
```

如果本机只有 .NET Runtime 而没有 .NET SDK，`Build-Release.ps1` 会把 SDK 安装到项目内的 `.dotnet` 目录，不修改系统级 SDK。

## 使用

```powershell
cd D:\WeChatPrivacySkin
.\Start-WeChatPrivacySkin.ps1
```

也可以直接运行：

```text
D:\WeChatPrivacySkin\bin\Release\net10.0-windows\win-x64\publish\WeChatPrivacySkin.exe
```

## 安全边界

- 不读取聊天内容，不截图微信窗口。
- 不修改微信安装目录，不注入微信进程，不绕过微信安全机制。
- 主题皮肤是外层沉浸效果，不替换微信内部聊天气泡、消息列表或控件样式。
- 共享“整个屏幕”时遮罩会被一起捕获；部分会议软件如果只共享某一个微信窗口，可能会绕过外层遮罩。

## 项目状态

当前版本是本地 Windows 桌面工具原型，重点覆盖微信窗口隐私保护、主题皮肤、托盘入口和管理平台。真实多微信窗口、不同会议软件、不同 DPI/多显示器环境仍建议在实际办公场景中继续验证。

---

## English

WeChatPrivacySkin is a Windows companion app for WeChat. It does not modify, inject into, or repackage the official WeChat client. Instead, it uses external overlay windows to provide privacy protection, immersive skins, and a lightweight management platform.

## Features

- System tray companion:
  - Right-click the tray icon to open the management platform.
  - Double-click the tray icon to enable or disable privacy protection.
  - Hold `Shift` and right-click to open the fallback exit menu.
- Privacy scenarios:
  - Daily protection: keep the active WeChat window visible and mask other WeChat windows.
  - Meeting share: strengthen masking and warn about single-window sharing limitations.
  - Away cover: cover all WeChat windows when stepping away from the desk.
  - Clean screen: instantly cover all WeChat windows, then press again to return to daily protection.
  - Focus chat: keep only the active chat window visible.
- Global hotkeys:
  - `Ctrl + Alt + P`: enable or disable privacy protection.
  - `Ctrl + Alt + T`: switch theme skins.
  - `Ctrl + Alt + L`: toggle clean screen mode.
- Themes and skins:
  - Built-in themes include Office Light, Night Dark, Stealth Privacy, Kawaii Note, Anime Neon, Pixel Game, and Paper Journal.
  - Supports extended frames, sticker badges, glow outlines, custom backgrounds, and overlay textures.
- Window coverage:
  - Detects WeChat main/chat windows, file transfer, image preview, search, favorites, forwarding, login, notification, and other pop-up windows.
- Settings are stored in `%APPDATA%\WeChatPrivacySkin\settings.json`; older settings are migrated automatically.

## Build

This project uses WPF and targets `net10.0-windows`.

```powershell
cd D:\WeChatPrivacySkin
.\Build-Release.ps1
```

If the machine only has the .NET Runtime and not the .NET SDK, `Build-Release.ps1` installs the SDK into the local `.dotnet` folder inside the project. It does not modify the system-level SDK installation.

## Run

```powershell
cd D:\WeChatPrivacySkin
.\Start-WeChatPrivacySkin.ps1
```

You can also run the published executable directly:

```text
D:\WeChatPrivacySkin\bin\Release\net10.0-windows\win-x64\publish\WeChatPrivacySkin.exe
```

## Safety Boundaries

- The app does not read chat content or take screenshots of WeChat windows.
- The app does not modify the WeChat installation directory, inject into WeChat, or bypass WeChat security mechanisms.
- Theme skins are external immersive overlays; they do not replace WeChat's internal chat bubbles, message list, or native controls.
- When sharing the entire screen, overlays should be captured together with the screen. Some meeting tools may bypass the overlay if only a single WeChat window is shared.

## Project Status

This is a local Windows desktop tool prototype focused on WeChat privacy protection, theme skins, tray entry points, and a management platform. Real-world validation is still recommended with multiple WeChat windows, different meeting apps, DPI settings, and multi-monitor setups.
