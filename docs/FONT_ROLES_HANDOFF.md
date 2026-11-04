# 交接：字体按用途取用（FR-1）+ 中文缺字

状态：**已实现（0.11.0，未发布）。** 2026-11-05 记录，库当前版本 0.10.1，游戏 1.0.4（build 26433890）。

## 0. 实现结果（2026-11-05）

- 1.0.4 反编译已补：`decompiled_root\v1.0.4-build26433890\_tmp_Core`。§3 的机制在 1.0.4 中未变。
- §6 核实结果：
  - 英文 `fontCache`：Round = Fredoka Regular/Medium/SemiBold，Serif = Merriweather Light/Regular/Bold。Fredoka 就是 Round 系列。
  - 中文：Round 三项都是 `Chinese_Round_NotoSansSC-SemiBold SDF`，Serif 三项都是 `Chinese_Serif_NotoSansSC-Light SDF`。**中文主菜单阶段 `fontCache` 经常全空**：主菜单的 `FontLocalizer.Awake` 早于中文字体集加载完成，菜单文字是后来被 I2 `Localize` 按字体名改过来的。所以取用时必须自己调 `LoadFontCache()`。
  - 游戏用法：设置页及全部选项行 SerifRegular；居中 HUD 提示 SerifBold；小号 HUD 文字 SerifSemibold；按键图例 RoundSemibold；资源数 RoundRegular。
  - 落定：设置页 SerifRegular；Popup SerifBold 34pt；Toast 标题 SerifBold、正文 SerifSemibold。
- 与 §4/§5 的偏离：
  - 挂的是 `FontLocalizer.LoadFontCache` 的 postfix，不是 `SetAllFonts`。`LoadFontCache` 是缓存唯一的写入者，既覆盖语言切换（`SetAllFonts` 内部会调它），也覆盖首次填充。
  - 游戏类型用的是带类型的访问（库本来就直接引用 Core），包在 `try/catch` + `NoInlining` 里；补丁不走 `PatchAll`，单独打。这样缺成员时只降级，不会拖垮其它补丁。
  - 设置页的自建行没有 `Bind`（每次打开都会重建，切换语言时它们不在屏上）；页脚文字做了 `Bind`。
- 实机验证：`SulfurLogs\SULFUR Native UI Lib\1`（核实）、`2`（功能 + 语言切换）、`3`、`4`（确定性）。新测试预设 `NativeUI`（Gale 预设 `NativeUI开发`，主机 + 沙盒客机）。
- 未验证：`SetActiveFallbackFontAsset` 清空全局列表（需要手柄），只核对了回退字体挂在了各字体自己的表上；Together 实际流程中的提示。

## 1. 症状

- **中文缺字。**中文环境下，提示框（Toast，右上角）和弹窗（Popup，屏幕中间）里的中文全部为空，只剩 ASCII。例如 Together 的进场提示 `按 [Return] 进入竞技场` 只显示 `[Return]`，提示框只显示 `——`。游戏自己的 HUD（如"位阶 5"）显示正常。
- **字体随机变化。**由本库绘制的文字，有时换成另一种字体。同一个弹窗在 SULFUR Together 的 round 590 中显示正常，到 round 591 就缺字。

证据见 SULFUR Together 的测试日志 `D:\MOD_dev\SulfurLogs\Together\<轮次>\client-Player.log`。客机是中文（`LocalizedFont_zh-CH`），主机是英文、不受影响。

| 轮次 | 游戏 | 客机 `was not found in the [...] font asset or any potential fallbacks` |
|---|---|---|
| 588 | 1.0.3 | 18 条，全在 `Merriweather-Regular SDF` |
| 590 | 1.0.4 | 360 条，全在 `Fredoka-Regular SDF` |
| 591 | 1.0.4 | 450 条在 `Fredoka-Regular SDF`，另有 18 条在 Merriweather |

两端启动时都打出了 `Built CJK glyph fallback: SULFUR_CJK_Fallback (Microsoft YaHei)`，说明回退字体建成了，问题出在没被用上。

## 2. 根因（三层）

1. **字体来源是随机的。**`Core/SulfurUiFont.FindLiveGameText` 返回 `FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None)` 里第一个可见的游戏文字。顺序不确定，抓到的字体取决于当时场景里的加载顺序，而 Toast / Popup 抓到一次就固定（`fontResolved`）。1.0.4 起部分界面文字用了纯拉丁字体 `Fredoka-Regular SDF`（这一点是从日志推断的，未核对游戏资源），抓中它，中文就整段消失。
2. **全局回退列表会被游戏清空。**游戏的 `SetActiveFallbackFontAsset`（1.0.3 反编译 `PerfectRandom.Sulfur.Core.decompiled.cs` 约第 32430 行）在刷新按键图标时执行 `TMP_Settings.fallbackFontAssets.Clear()`，然后只放入按键图标字体。`SulfurFontFallback.EnsureInGlobalFallbacks` 加进去的回退字体会被随时抹掉。
3. **Toast / Popup 没有把回退字体挂到自己的字体上。**设置页那条路径（`SulfurOptionsContext.FindSampleText`）会调用 `SulfurFontFallback.EnsureFontHasFallback(sampleTextCache.font)`，所以设置页没出问题；`SulfurToastController.EnsureFont` 和 `SulfurPopupController.EnsureFont` 没调，只能靠第 2 条里那个不可靠的全局列表。

## 3. 游戏自己的机制（要对接的源头）

1.0.3 反编译 `PerfectRandom.Sulfur.Core.decompiled.cs` 约第 52031–52125 行：

- `enum FontStyle { RoundRegular, RoundSemibold, RoundBold, SerifRegular, SerifSemibold, SerifBold, End }`，共 6 种用途。
- `FontLocalizer`（MonoBehaviour）：
  - `public static void LoadFontCache()`：对每种用途调用 `LocalizationManager.GetTranslatedObjectByTermName<TMP_FontAsset>($"Font/{(FontStyle)i}")`，结果存进 `AsyncAssetLoading.Instance.fontCache[6]`。
  - `public static void SetAllFonts()`：重新加载缓存，再把字体套到 `AsyncAssetLoading.Instance.allFontLocalizers` 里登记的每一段文字上。这是**语言切换**时的原版事件。
  - `Awake`：缓存为空时先 `LoadFontCache()`，再按自己的 `fontStyle` 套字体。

"游戏内固定字体"就是这 6 种按用途、跟随语言的字体。游戏界面自己就是这样取字体的。

## 4. 设计（已定的四条规则）

1. **只按用途取字体。**不随机抓，也不按资源名取。
   - 公开接口形如 `SulfurFonts.Get(SulfurFontRole role)`，返回 `TMP_FontAsset`。
   - `SulfurFontRole` 是库自己的枚举，和游戏 `FontStyle` 的 6 个值一一对应，游戏的类型不出现在公开 API 里。
   - 解析：读 `AsyncAssetLoading.Instance.fontCache[(int)style]`；缓存为空就先调 `FontLocalizer.LoadFontCache()`。
   - **不提供按名字取字体的接口**：字体名会随游戏更新改变（Fredoka 就是例子），而且不跟随语言。
2. **绑定，而不是一次性赋值。**
   - `SulfurFonts.Bind(TMP_Text text, SulfurFontRole role)`：库用弱引用登记文字，游戏切换语言时自动重新套字体。时机用 `FontLocalizer.SetAllFonts` 的 postfix，这是原版事件。
   - 只要字体资源的调用方可以订阅 `SulfurFonts.FontsChanged`。
3. **库交出去的每一个字体，回退字体都挂在它自己身上。**
   - 中文和符号回退一律走 `SulfurFontFallback.EnsureFontHasFallback(font)`，挂进字体自己的 `fallbackFontAssetTable`，不再依赖全局列表（见根因 2）。
   - 自带字体的 mod 用 `SulfurFonts.WithFallback(TMP_FontAsset own)` 得到同样的保证。
   - **这一条单独就能修掉中文缺字**，而且不管字体从哪里来都成立。
4. **游戏机制缺失时给出确定的结果。**
   - 找不到 `FontLocalizer`、`LoadFontCache` 或 `fontCache` 时，只记一行"该游戏版本不支持按用途取字体"；
   - 退回 `TMP_Settings.defaultFontAsset`，同样挂上回退字体；
   - **绝不退回随机抓字体。**

字号、颜色、材质不再从游戏文字借，由库为每种界面固定。材质用字体自带的 `font.material`。

## 5. 要改的地方

- 新增：`Core/SulfurFonts.cs`（公开 API）、`SulfurFontRole` 枚举、`FontLocalizer.SetAllFonts` 的 postfix（Harmony；游戏类型用反射，启动时解析一次）。
- `Toast/SulfurToastController.cs` `EnsureFont`：改用 `SulfurFonts.Get(role)`，并 `Bind` 到 `SulfurToastView` 的文字上。
- `Popup/SulfurPopupController.cs` `EnsureFont`：同上。字号目前按借来的文字乘 1.1（`Mathf.Clamp(sample.fontSize * 1.1f, 28f, 48f)`），改成固定值。
- `Options/SulfurOptionsContext.cs` `ResolveSampleText` / `FindLiveLanguageText`，以及 `Integration/SulfurOptionsScreenBridge.cs`、`Rows/SulfurOptionsFoldoutExtensions.cs` 里的 `x.font = sample.font`：
  - 字体改用 `SulfurFonts.Get(role)`；
  - 字号基准仍可沿用选项预制体（`FindPrefabSampleText`，那是稳定来源，注释里写了为什么不能扫容器）。
- `Core/SulfurUiFont.FindLiveGameText`：所有调用方都改完后删除。
- `Core/SulfurFontFallback.EnsureInGlobalFallbacks`：可以保留作额外一层，但不能再作为唯一保证。
- 文档：`docs/API_REFERENCE.md`（新 API）、`docs/TOAST_NOTIFICATIONS.md` / `docs/POPUP_BANNER.md`（字体说明）、`docs/FOLDOUT_STYLE.md`（示例代码里的 `sample.font`）、`CHANGELOG.md`。

## 6. 动手前先核实

1. **补上 1.0.4 的反编译**（`decompiled_root` 下目前只到 1.0.3），确认 `FontLocalizer`、`FontStyle`、`LoadFontCache`、`SetAllFonts`、`fontCache`、`SetActiveFallbackFontAsset` 在 1.0.4 里都没变。
2. **中文环境下 `fontCache` 的实际内容。**在中文游戏里 inspect `AsyncAssetLoading.Instance.fontCache[*].name`，看 6 种用途各是什么字体、Round 系列本身带不带中文。英文环境再看一次，确认 Fredoka 是不是就是 Round 系列。
3. **每种界面用哪种用途。**先看游戏自己的 HUD 提示、按键提示、设置页文字分别挂的是哪个 `FontLocalizer.fontStyle`，再决定 Toast、Popup 和设置页各用哪种。倾向于 Toast / Popup 和游戏 HUD 提示用同一种。
4. **时机。**库第一次画 Toast 时 `fontCache` 可能还空着（要等第一个 `FontLocalizer.Awake`）。确认直接调 `LoadFontCache()` 在主菜单或刚开局时能拿到非空结果。

## 7. 验证方法（实机）

用中文游戏（沙盒客机是中文，用 SULFUR Together 的 mcp-sulfur 双开即可）：

1. 启动日志：确认 `SulfurFonts` 解析成功的那一行，以及不出现"不支持"。
2. 触发 Toast 和 Popup：Together 的加入会话提示，以及竞技场"按 [Return] 进入竞技场"。截图确认中文完整显示，`client-Player.log` 里 `was not found in the` 的条数为 0。
3. **确定性**：重启 3 次，每次截图对比，确认 Toast / Popup 的字体完全一致。
4. **全局列表被清空的情况**：在键盘和手柄之间切换，触发 `SetActiveFallbackFontAsset`，确认中文仍然完整。
5. **语言切换**：游戏里把语言从中文切到英文再切回来，确认已显示的 Toast / 设置页文字跟着换字体，且不缺字。
6. 设置页回归：自定义设置页的字号、样式和改动前一致（防止字号随重建缩小的旧问题回来）。

## 8. 版本与兼容

- 新增公开 API，Toast / Popup 的字体从随机变成确定：发 **0.11.0**。
- 现有调用方（SULFUR Together、SULFUR Config 等用 `SulfurToastApi` / `SulfurPopupApi` 的 mod）**不用改代码**，换上新版库就生效。有自建文字的 mod 以后可以改用 `SulfurFonts.Bind`。
- `[BepInPlugin]` 的版本号和 `Thunderstore/manifest.json` 要一起改（ModHub WO-14 记录过两者不一致的问题）。
