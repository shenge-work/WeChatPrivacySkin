# WeChatPrivacySkin

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

## 边界

- 不读取聊天内容，不截图微信窗口。
- 不修改微信安装目录，不注入微信进程，不绕过微信安全机制。
- 主题皮肤是外层沉浸效果，不替换微信内部聊天气泡、消息列表或控件样式。
- 共享“整个屏幕”时遮罩会被一起捕获；部分会议软件如果只共享某一个微信窗口，可能会绕过外层遮罩。
