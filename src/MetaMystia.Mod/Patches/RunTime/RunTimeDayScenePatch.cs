using HarmonyLib;
using System.Linq;

using GameData.Core.Collections.DaySceneUtility.Collections;
using GameData.RunTime.Common;
using GameData.RunTime.DaySceneUtility;

using static GameData.Core.Collections.DaySceneUtility.Collections.Product;

using MetaMystia.ResourceEx.Registries;
using SgrYuki.Utils;

namespace MetaMystia.Patch;

[HarmonyPatch(typeof(GameData.RunTime.DaySceneUtility.RunTimeDayScene))]
[AutoLog]
public partial class RunTimeDayScenePatch
{

    [HarmonyPatch(nameof(RunTimeDayScene.GetMerchantData))]
    [HarmonyPostfix]
    public static void GetMerchantData_Postfix(ref GameData.RunTime.DaySceneUtility.Collection.TrackedMerchant __result, string characterKey)
    {
        Log.Info($"DataBaseDay.GetMerchantData Postfix called with key: {characterKey}");
        if (!MerchantRegistry.TryGetExMerchantData(characterKey, out Merchant merchant))
        {
            return;
        }

        // 游戏原有的 「香霖堂」「爱莲」以及「萌橙果」「蹦蹦跳跳的三妖精」商人有自己的脚本，会特殊处理商品，这里需要简单处理一下商品
        // 对 Ex 商人的已有「食谱」进行过滤
        __result.products = __result.products
            .Where(m => m.productType != ProductType.Recipe
                   || !RunTimeStorage.Recipes.Contains(m.productId))
            .ToIl2CppReferenceArray();
    }

    /// <summary>
    /// 行动次数变化（收集/烹饪/传送等消耗行动）后，刷新并广播当前日期+时间展示文本。
    /// </summary>
    [HarmonyPatch(nameof(RunTimeDayScene.OnTimePassInternal))]
    [HarmonyPostfix]
    public static void OnTimePassInternal_Postfix(int actions) => DayTimeManager.RefreshAndBroadcast();

    /// <summary>
    /// 新的一天开始（行动次数重置）后，刷新并广播当前日期+时间展示文本。
    /// </summary>
    [HarmonyPatch(nameof(RunTimeDayScene.SetupDay))]
    [HarmonyPostfix]
    public static void SetupDay_Postfix(System.Action onDayEnd) => DayTimeManager.RefreshAndBroadcast();

    // 以下三个是 RemainActions 可能被直接改动、但不经过 OnTimePassInternal 的口子（跳过事件/剧情等）。
    // 临时兜底：在换成读取游戏原生时钟前，先保证这些路径也不会漏刷新。
    [HarmonyPatch(nameof(RunTimeDayScene.SetActions))]
    [HarmonyPostfix]
    public static void SetActions_Postfix(int actions) => DayTimeManager.RefreshAndBroadcast();

    [HarmonyPatch(nameof(RunTimeDayScene.WarpActions))]
    [HarmonyPostfix]
    public static void WarpActions_Postfix(int actions, System.Action<System.Action> onCustomEventFinish) => DayTimeManager.RefreshAndBroadcast();

    [HarmonyPatch(nameof(RunTimeDayScene.WarpHours))]
    [HarmonyPostfix]
    public static void WarpHours_Postfix(int hours, System.Action<System.Action> onCustomEventFinish) => DayTimeManager.RefreshAndBroadcast();
}
