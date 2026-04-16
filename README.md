[English](#english) | [简体中文](#zh-cn)

<a id="english"></a>
# ExperienceTuner-Fix (Unofficial)

> **Disclaimer**: This is an **unofficial** patch for the Valheim mod "ExperienceTuner". All rights and copyright of the original mod design and code belong to the original author (Honja). I created this patch based on decompiled code because the original author is currently uncontactable and the source code is unavailable. If the original author wishes to take over this branch or requests it to be taken down, please contact me and I will comply immediately.
> 
> **Original Mod Link**: [ExperienceTuner by Honja](https://thunderstore.io/c/valheim/p/Honja/ExperienceTuner/)

## 📝 Description

This is an unofficial fix for the **ExperienceTuner** mod. The original mod has a critical bug that prevents skill multiplier synchronization from working properly when joining a dedicated server.

### The Problem in the Original Mod
In the original mod, newly joined clients were meant to synchronize skill variables (XP multiplier, Death Penalty multiplier) from the server. However, the mod attempted to retrieve the server's ID using `GetServerPeerId()`, which evaluates to `0` for Valheim dedicated servers. 

```csharp
// Original buggy logic (Client side)
long serverPeerId = GetServerPeerId(); // Evaluates to 0 on dedicated servers
if (serverPeerId == 0L)
{
    return; // The request is permanently dropped here!
}
...
ZRoutedRpc.instance.InvokeRoutedRPC(serverPeerId, "SkillTuner Request", ...);
```
Because of a hardcoded condition (`if (serverPeerId == 0L) return;`), the client would immediately drop the synchronisation request. Furthermore, relying on `ZRoutedRpc` for early connection handshakes introduced occasional timing and registration issues that resulted in packet drops.

### The Fix
This unofficial fix completely removes the dependency on `ZRoutedRpc` for config synchronization. Instead, it utilizes the native `ZNetPeer.m_rpc` underlying protocol to establish a point-to-point, instant push of configuration from the server directly to connecting clients. 

```csharp
// Our Fix: Injecting direct m_rpc calls exactly when the connection is established
[HarmonyPatch(typeof(ZNet), "OnNewConnection")]
private static class ZNetOnNewConnectionPatch
{
    private static void Postfix(ZNet __instance, ZNetPeer peer)
    {
        if (peer?.m_rpc != null)
        {
            // Register passive receiver on all peers
            peer.m_rpc.Register<float, float>("SkillTuner Sync", RpcReceiveMultipliers);
            if (__instance.IsServer())
            {
                // Push config instantly downwards
                peer.m_rpc.Invoke("SkillTuner Sync", new object[] { EffectiveExperienceMultiplier, EffectiveDeathPenaltyMultiplier });
            }
        }
    }
}
```
* The server now forcefully pushes its multipliers to the client the moment a connection is established.
* The client acts as a passive receiver, successfully avoiding ID calculation fallacies.

## 🛠️ How to Compile / Build

1. Clone or download this repository.
2. The project requires the following references which are **not** included in the repository due to copyright:
    * `assembly_valheim.dll` (from your Valheim `/valheim_data/Managed` folder)
    * `0Harmony.dll` (from BepInEx core folder)
    * `BepInEx.dll` (from BepInEx core folder)
    * `UnityEngine.dll` (from Valheim Managed folder)
    * `UnityEngine.CoreModule.dll` (from Valheim Managed folder)
3. Place your references into a folder or update the paths inside `build/ExperienceTuner-Fix.csproj` to match your local Valheim install paths.
4. Run `dotnet build` or compile in Visual Studio/Rider.
5. Place the generated `.dll` file into your server and client `BepInEx/plugins` folder.

---

<a id="zh-cn"></a>
# ExperienceTuner-Fix (非官方修复版)

> **免责声明 (Disclaimer)**：这是一个针对《Valheim（英灵神殿）》模组 "ExperienceTuner" 的**非官方修复分支**。原模组的所有设计理念及版权均归原作者 (Honja) 所有。因为目前无法联系到原作者且未找到官方开源仓库，本分支基于反编译代码进行了漏洞热修复。如果原作者看到并希望接管此修复逻辑或要求下架本仓库，请随时与我联系，我将无条件配合。
> 
> **原模组地址**: [ExperienceTuner by Honja](https://thunderstore.io/c/valheim/p/Honja/ExperienceTuner/)

## 📝 概览

这是 **ExperienceTuner** 模组的一个非官方修复版。原版模组在专用多人服务器环境下存在一个严重的问题：客户端加入游戏后，服务器修改的经验倍率/死亡掉落倍率设置无法生效。

### 原版存在的问题
在原版模组中，客户端连接服务器时需要向服务器索取参数。但在检测逻辑中，客户端会使用 `GetServerPeerId()` 获取服务器 ID。对于专用局域网服务器，该 ID 等于 `0`。

```csharp
// 原版出错的核心逻辑
long serverPeerId = GetServerPeerId(); // 专用服务器这里永远获得 0
if (serverPeerId == 0L)
{
    // 如果获取不到身份代码，我就不找服务器要数据了！（直接掉坑）
    return; 
}
...
ZRoutedRpc.instance.InvokeRoutedRPC(serverPeerId, "SkillTuner Request", ...);
```
原作者写下了一句 `if (serverPeerId == 0L) return;` 的防错代码，这导致客户端在索取配置的瞬间就直接把请求拦截丢弃了。此外，原代码高度依赖 `ZRoutedRpc` 进行通信，在早期连接阶段存在注册时序不同步的隐患。

### 修复方案
本分支彻底移除了对 `ZRoutedRpc` 同步逻辑的依赖，将其替换为了底层网络协议的 `ZNetPeer.m_rpc` 点对点直连通讯。

```csharp
// 运用底层的 OnNewConnection 进行点对点暴力强推
[HarmonyPatch(typeof(ZNet), "OnNewConnection")]
private static class ZNetOnNewConnectionPatch
{
    private static void Postfix(ZNet __instance, ZNetPeer peer)
    {
        if (peer?.m_rpc != null)
        {
            // 所有节点监听同步包
            peer.m_rpc.Register<float, float>("SkillTuner Sync", RpcReceiveMultipliers);
            // 只有服务端负责主动往下发包
            if (__instance.IsServer())
            {
                peer.m_rpc.Invoke("SkillTuner Sync", new object[] { EffectiveExperienceMultiplier, EffectiveDeathPenaltyMultiplier });
            }
        }
    }
}
```
* 专用服务器将会在探测到客户端连接的那一刻，主动强行将自身的配置数据推给客户端。
* 客户端不再需要主动索求或者计算服务器ID，作为被动接收方，保证了参数100%成功覆盖。

## 🛠️ 本地编译构建指南

这套代码可以自己编译生成 DLL，但鉴于版权保护，游戏本体的文件未纳入仓库。你需要自行准备：

1. 克隆或下载本仓库代码。
2. 你需要引入以下这 5 个 DLL 的外部引用（这些可从你的 Valheim 游戏目录和 BepInEx 中提取）：
    * `assembly_valheim.dll`（位于游戏目录下 `valheim_data/Managed`）
    * `0Harmony.dll`（位于 `BepInEx/core`）
    * `BepInEx.dll`（位于 `BepInEx/core`）
    * `UnityEngine.dll`（位于游戏目录下 `valheim_data/Managed`）
    * `UnityEngine.CoreModule.dll`（同上）
3. 把这些 DLL 丢到你项目设置的识别路径里，或者直接修改源码中 `build/ExperienceTuner-Fix.csproj` 的依赖路径，指向你的本地游戏目录。
4. 在控制台运行 `dotnet build` 或是使用 Visual Studio / Rider 等 IDE 进行构建。
5. 将编译产出的新 `.dll` 文件丢进服务端和客户端各自的 `BepInEx/plugins` 里即可生效。
