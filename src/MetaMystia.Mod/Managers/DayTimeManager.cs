using GameData.RunTime.Common;
using GameData.RunTime.DaySceneUtility;

using MetaMystia.Network;

namespace MetaMystia;

/// <summary>
/// 负责计算本地玩家当前的日期+时间展示文本，并在其发生变化时广播给其他玩家。
///
/// 时间部分的计算基于玩家口述的实测规则（并非逆向确认）：
/// 白天场景固定从 10:00 开始，每消耗 1 次行动（RemainActions 减 1）视为 +30 分钟，
/// 18:00 为开店时间。若实际游戏表现与此不符，请调整 <see cref="DayStartHour"/>
/// 和 <see cref="MinutesPerAction"/>。
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
            string dateText = $"{date.Month}月{date.Day}日{date.DaysOfTheWeek}";

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
