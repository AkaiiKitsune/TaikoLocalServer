using SharedProject.Enums;

namespace SharedProject.Models;

public class SongBestHistory
{
    public uint Score { get; set; }

    public uint Rate { get; set; }

    public CrownType Crown { get; set; }

    public ScoreRank ScoreRank { get; set; }

    public DateTime PlayTime { get; set; }

    public DateTime BestPlayTime { get; set; }

    public uint GoodCount { get; set; }

    public uint OkCount { get; set; }

    public uint MissCount { get; set; }

    public uint ComboCount { get; set; }

    public uint HitCount { get; set; }

    public uint DrumrollCount { get; set; }
}