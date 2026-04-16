[English](#english) | [简体中文](#zh-cn)

<a id="english"></a>
# ExperienceTuner-Fix Reference Notes

This directory only keeps the compile-time references required to build this `Valheim` `BepInEx` mod.

## Kept content

### `BepInEx/core`

The full `BepInEx` core assembly set is kept because plugin development depends on these files directly or indirectly:

- `BepInEx.dll`
- `BepInEx.Harmony.dll`
- `0Harmony.dll`
- `HarmonyXInterop.dll`
- `Mono.Cecil*.dll`
- `MonoMod*.dll`

Notes:

- Even if the mod code only references part of them directly, keeping the full `core` directory is safer.
- It allows the `.csproj` to use stable reference paths without repeatedly fixing missing transitive dependencies.

### `Valheim/Managed`

The full `Managed` directory is kept because, in the current Valheim version, gameplay, UI, and networking-related types are spread across multiple assemblies:

- core gameplay logic is now in `assembly_valheim.dll`
- compatibility layers and some older references may still involve `Assembly-CSharp.dll`
- server list, join flow, UI, TextMeshPro, and Steam/PlayFab related types rely on additional Unity and platform assemblies

Notes:

- For this project, keeping the full `Managed` directory is safer than manually trimming it down to only a small set of DLLs.
- It is still only a few dozen MB, but it avoids development interruptions caused by missing transitive dependencies while writing patches.

## Removed content

The following items are not necessary compile-time dependencies for this project and were removed from the slimmed-down reference set:

- resource files, executables, logs, configs, and plugin folders from the full game copy
- the `BepInExPack_Valheim` distribution bundle
- the `Harmony-Fat` multi-target framework distribution package
- `ValheimModding-Jotunn`

## Why Jotunn is not kept

The current target is a lightweight network sync mod, so the preferred stack is:

- `BepInEx`
- `Harmony`
- native `Valheim` types

This feature does not require Jotunn's higher-level APIs. If we later decide to add a richer config UI, more advanced commands, or other larger features, we can add it back then.

---

<a id="zh-cn"></a>
# ExperienceTuner-Fix 开发依赖清单

这个目录只保留了为开发 `Valheim` 的 `BepInEx` mod 所需的编译期引用。

## 保留内容

### `BepInEx/core`

保留整套 `BepInEx` 核心程序集，原因是插件开发会直接或间接依赖这些文件：

- `BepInEx.dll`
- `BepInEx.Harmony.dll`
- `0Harmony.dll`
- `HarmonyXInterop.dll`
- `Mono.Cecil*.dll`
- `MonoMod*.dll`

说明：

- 即使我们的 mod 代码最终只直接引用其中一部分，保留整个 `core` 目录更稳妥。
- 这样后面创建 `.csproj` 时可以直接按固定路径引用，不需要反复补传递依赖。

### `Valheim/Managed`

保留整个 `Managed` 目录，原因是当前版本的 Valheim 游戏逻辑与 UI/联网相关类型分散在多个程序集里：

- 主要游戏逻辑已经在 `assembly_valheim.dll`
- 兼容层和部分旧引用仍可能涉及 `Assembly-CSharp.dll`
- 服务器列表、加入服务器、UI、TextMeshPro、Steam/PlayFab 相关类型需要额外的 Unity 和平台程序集

说明：

- 对这个项目来说，完整保留 `Managed` 比手工删到只剩十几个 dll 更安全。
- 这一层总大小仍然只有大约几十 MB，但能避免后面写补丁时因为缺少传递依赖而中断开发。

## 已移除内容

下面这些内容不属于本项目的必要编译依赖，已经从精简版中排除：

- 完整游戏副本中的资源文件、可执行文件、日志、配置、插件目录
- `BepInExPack_Valheim` 整包分发文件
- `Harmony-Fat` 多目标框架发行包
- `ValheimModding-Jotunn`

## 为什么没有保留 Jotunn

当前目标是一个轻量级的网络同步 mod，优先采用：

- `BepInEx`
- `Harmony`
- `Valheim` 原生类型

这类功能不需要 `Jotunn` 的高层 API。后面如果我们决定扩展出配置界面、复杂命令或其他内容，再重新加入也不晚。