# WeChatPrivacySkin

一个不修改微信本体的 Windows 伴随工具，用来给微信窗口提供外层主题、背景皮肤和企业隐私遮罩。

## 功能

- 托盘常驻，右键可切换隐私模式、主题、遮罩强度和背景图片。
- 全局快捷键：
  - `Ctrl + Alt + P`：开启/关闭隐私模式。
  - `Ctrl + Alt + T`：切换主题。
- 仅识别 `C:\Program Files\Tencent\Weixin\Weixin.exe` 或同目录下的微信窗口。
- 当前正在操作的微信窗口保持可见，其他微信窗口显示企业隐私遮罩。
- 当前微信窗口会显示主题色焦点边框，帮助确认哪一个窗口没有被脱敏。
- 配置保存在 `%APPDATA%\WeChatPrivacySkin\settings.json`。

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

也可以直接运行 `D:\WeChatPrivacySkin\bin\Release\net10.0-windows\win-x64\publish\WeChatPrivacySkin.exe`。启动后会出现在系统托盘。双击托盘图标可以打开设置窗口。

## 边界

- 不会读取聊天内容，不会截图微信窗口。
- 不修改微信安装目录，不注入微信进程，不绕过微信安全机制。
- v1 的“主题/皮肤”是外层遮罩和背景视觉，不替换微信内部聊天气泡、消息列表或控件样式。
- 共享“整个屏幕”时遮罩会被一起捕获；部分会议软件如果只共享某一个微信窗口，可能会绕过伴随工具的外层遮罩。
