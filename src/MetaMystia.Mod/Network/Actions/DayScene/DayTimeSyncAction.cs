using MemoryPack;

namespace MetaMystia.Network;

/// <summary>
/// 任何玩家 → 全体玩家：广播白天场景当前日期+时间的展示文本（用于玩家列表面板显示）。
/// 文本由发送方在本地按 <see cref="MetaMystia.DayTimeManager"/> 的规则计算，仅用于展示，
/// 不参与游戏逻辑判定，接收方直接信任并写入对应 peer 的缓存文本。
/// </summary>
[MemoryPackable]
[AutoLog]
[PublicRelay]
public partial class DayTimeSyncAction : Action
{
    public string DayTimeText { get; set; } = "";

    protected override BepInEx.Logging.LogLevel OnReceiveLogLevel => BepInEx.Logging.LogLevel.Debug;
    protected override BepInEx.Logging.LogLevel OnSendLogLevel => BepInEx.Logging.LogLevel.Debug;

    public override void OnReceivedDerived()
    {
        if (PlayerManager.TryGetVisiblePeer(SenderUid, out var peer))
            peer.DayTimeText = DayTimeText;
    }

    /// <summary>
    /// 广播本地当前的日期+时间文本（<see cref="PlayerManager.Local"/>.DayTimeText 需已是最新值）。
    /// </summary>
    public static void Send()
    {
        if (!MpManager.IsConnected) return;
        new DayTimeSyncAction { DayTimeText = PlayerManager.Local.DayTimeText }.Enqueue(lowPriority: true);
    }
}
