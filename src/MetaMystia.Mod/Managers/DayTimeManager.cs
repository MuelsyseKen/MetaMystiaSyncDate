using GameData.RunTime.Common;
using GameData.RunTime.DaySceneUtility;

using MetaMystia.Network;

namespace MetaMystia;

/// <summary>
/// 负责计算本地玩家当前的日期+时间展示文本，并在其发生变化时广播给其他玩家。
///
/// ⚠️ 临时方案：目前没有找到游戏原生存储"当前白天时钟（时:分）"的字段，
/// 时间部分是按玩家口述的规则（10:00 开始，每消耗 1 个行动单位 +30 分钟）
/// 从 RemainActions 反推的，不是直接读取游戏真实状态。行动消耗的单位数
/// 由游戏原生结算（如一次烹饪多份可能一次性消耗 2 个单位），本实现每次都
/// 重新读取结算后的 RemainActions 而非自行累加，所以能覆盖"一次消耗超过
/// 30 分钟"的情况——但仍依赖 RunTimeDayScenePatch 里挂的这几个 Postfix
/// 覆盖了所有会改动 RemainActions 的入口。一旦找到游戏原生的当前时钟来源
/// （如 FastTravelPanel_New 构造 TimeIndicatorContext 时用的字段），
/// 应改为直接读取该值，废弃这里的近似算法。
/// </summary>
[AutoLog]
public static partial class DayTimeManager
{
    private const int DayStartHour = 10;
    private const int MinutesPerAction = 30;

    /// <summary>
    /// 计算当前（本地玩家）的日期+时间展示文本，如 "1月5日星期五 10:00"。
    /// 失败时返回空字符串。
    /// </summary>
    public static string ComputeCurrentText()
    {
        try
        {
            var date = RunTimePlayerData.Date;
            string dateText = $"{date.Year}年{date.Month}月{date.Day}日{date.DaysOfTheWeek}";

            int totalActions = RunTimeDayScene.GetTotalActions();
            int remainActions = RunTimeDayScene.RemainActions;
            int usedActions = System.Math.Max(0, totalActions - remainActions);

            int totalMinutes = DayStartHour * 60 + usedActions * MinutesPerAction;
            int hour = (totalMinutes / 60) % 24;
            int minute = totalMinutes % 60;
            string timeText = $"{hour:D2}:{minute:D2}";

            return $"{dateText} {timeText}";
        }
        catch (System.Exception e)
        {
            Log.LogWarning($"ComputeCurrentText failed: {e.Message}");
            return "";
        }
    }

    /// <summary>
    /// 重新计算本地日期+时间文本；若与缓存值不同，则更新本地缓存并广播给其他玩家。
    /// 仅在 DayScene 中有意义，其他场景直接跳过。
    /// </summary>
    public static void RefreshAndBroadcast()
    {
        if (MpManager.LocalScene != Common.UI.Scene.DayScene) return;

        string text = ComputeCurrentText();
        if (string.IsNullOrEmpty(text) || text == PlayerManager.Local.DayTimeText) return;

        PlayerManager.Local.DayTimeText = text;
        DayTimeSyncAction.Send();
    }
}
