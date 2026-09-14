using GameData.RunTime.Common;
using GameData.RunTime.DaySceneUtility;

using MetaMystia.Network;

namespace MetaMystia;

/// <summary>
/// 负责计算本地玩家当前的日期+时间展示文本，并在其发生变化时广播给其他玩家。
///
/// ⚠️ 临时方案：目前没有找到游戏原生存储"当前白天时钟（时:分）"的字段，
/// 时间部分是按玩家口述的规则（10:00 开始，每消耗 1 个行动单位 +30 分钟）
/// 从 RemainActions 反推的，不是直接读取游戏真实状态。为了在推算有偏差时仍
/// 有据可查，时间后面附带原始的剩余行动点数，如 "10:00(16)"；行动点数耗尽
/// （对应 18:00 开店）后不再显示时间和行动点，只显示日期。一旦找到游戏原生
/// 的当前时钟来源（如 FastTravelPanel_New 构造 TimeIndicatorContext 时用
/// 的字段），应改为直接读取该值，废弃这里的近似算法。
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

            int remainActions = RunTimeDayScene.RemainActions;
            if (remainActions <= 0)
            {
                // 行动次数耗尽 = 18:00 已到，进入夜晚开店流程，不再显示具体时间和剩余行动点
                return dateText;
            }

            int totalActions = RunTimeDayScene.GetTotalActions();
            int usedActions = System.Math.Max(0, totalActions - remainActions);

            int totalMinutes = DayStartHour * 60 + usedActions * MinutesPerAction;
            int hour = (totalMinutes / 60) % 24;
            int minute = totalMinutes % 60;
            // 时间后面带上剩余行动点数（如 "10:00(16)"），这样即便时间本身的推算有偏差，
            // 剩余行动点数（RemainActions 原始值）仍然是准确的，可以互相印证。
            string timeText = $"{hour:D2}:{minute:D2}({remainActions})";

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
