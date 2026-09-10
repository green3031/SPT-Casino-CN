# 汉化说明（zh-CN）

本仓库是 [JoelHauser/SPT-Casino](https://github.com/JoelHauser/SPT-Casino) 的**非官方简体中文汉化版**，
基于原版 **V1.2.6** 源码汉化并重新编译。**未经原作者授权或背书**，仅供学习交流与非商业分享；
一切权利归原作者，若原作者要求会立即删除。原版发布页：[sp-mod.com/mod/2994](https://sp-mod.com/mod/2994/spt-casino)。

## 汉化范围

- 大厅、首次欢迎卡、礼金卡、21点 / 德州扑克 / 轮盘 / 老虎机 四张牌桌的全部可见界面文字；
- F12 设置面板中的设置项名称与说明；
- 发布包中服务端 DLL 内玩家可见的提示（下注被拒、余额不足、礼金发放情况）。

**保持英文**（翻译会破坏功能）：HTTP 路由（`/blackjack/ping` 等）、JSON 字段键、存档/配置键、
资源与音效文件名（`table.png`、`slot-win.wav` 等）、BepInEx 控制台日志与调试输出。

对局逻辑、概率、金额计算与结算流程**未做任何修改**。服务端源码（`src/*.Server`、`src/*.Game`）
保持原样；发布包中的服务端 DLL 仅做字符串级定点汉化。

## 相对原版的代码改动

| 文件 | 改动 |
| --- | --- |
| `src/Casino.Shared/FontPick.cs` | **新增**。在所有已加载的 `TMP_FontAsset` 中挑选一个同时含中文与拉丁字形的成品字体作为主字体（原版是“借用第一个字体”，中文会走全局回退字体链，出现方块或黑字白点杂色）；找不到时退回原版行为。 |
| `src/Casino.Client/CasinoLobby.cs`、`src/Blackjack.Client/BlackjackPanel.cs`、`src/Poker.Client/PokerPanel.cs`、`src/Roulette.Client/RoulettePanel.cs`、`src/SlotMachine.Client/SlotPanel.cs` | 字体获取处改为优先调用 `FontPick.Pick()`。 |
| `src/Roulette.Client/RoulettePanel.cs` | 轮盘与下注台布整组挂到一个容器下，按实际屏幕宽度围绕屏幕中心统一缩放，避免在 16:10 等窄屏上溢出屏幕（宽度足够时不缩放）。 |
| 21点 / 扑克面板 | 少数“同一字符串既作按钮文字又作协议键”的位置改为显示层映射（动作名、结果、街名），发给服务器的值仍是英文。 |

## 重新编译

客户端为 `net472`，且必须**针对实际游戏安装**编译（插件会校验对 `spt-*` 的引用版本）：

```powershell
dotnet build src/Casino.Client/Casino.Client.csproj -c Release -p:SPTPath=<SPT 安装根目录>
```

产物：`src/Casino.Client/bin/Release/Casino.Client.dll`。服务端按原版方式构建（本仓库未改其源码）。

## 已知限制

- 中文显示依赖游戏内已加载的含中文字形的 TMP 字体（常见来源是社区的 FontReplace 之类中文界面模组）；
  游戏若没有任何这类字体，中文会退回原版的显示问题；
- BepInEx 控制台日志仍为英文；
- 三个已退役的独立插件（`Blackjack.Client`、`Poker.Client`、`Roulette.Client`）不随包发布，其内部 F12 文案未汉化。
