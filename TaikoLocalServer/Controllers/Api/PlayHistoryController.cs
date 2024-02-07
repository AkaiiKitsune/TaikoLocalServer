using GameDatabase.Entities;
using SharedProject.Models;
using SharedProject.Models.Responses;

namespace TaikoLocalServer.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class PlayHistoryController : BaseController<PlayDataController>
{
    private readonly IUserDatumService userDatumService;

    private readonly ISongBestDatumService songBestDatumService;

    private readonly ISongPlayDatumService songPlayDatumService;

    public PlayHistoryController(IUserDatumService userDatumService, ISongBestDatumService songBestDatumService,
        ISongPlayDatumService songPlayDatumService)
    {
        this.userDatumService = userDatumService;
        this.songBestDatumService = songBestDatumService;
        this.songPlayDatumService = songPlayDatumService;
    }

    [HttpGet("{baid}")]
    public async Task<ActionResult<SongHistoryResponse>> GetSongHistory(ulong baid)
    {
        var user = await userDatumService.GetFirstUserDatumOrNull(baid);
        if (user is null)
        {
            return NotFound();
        }

        var songHistory = new List<SongHistoryData>();
        var playLogs = await songPlayDatumService.GetSongPlayDatumByBaid(baid);
        foreach (var play in playLogs)
        {
            var newSongEntry = new SongHistoryData();
            newSongEntry.SongId = play.SongId;
            newSongEntry.Difficulty = play.Difficulty;

            newSongEntry.Score = play.Score;
            newSongEntry.ScoreRank = play.ScoreRank;
            newSongEntry.Crown = play.Crown;
            newSongEntry.GoodCount = play.GoodCount;
            newSongEntry.OkCount = play.OkCount;
            newSongEntry.MissCount = play.MissCount;
            newSongEntry.HitCount = play.HitCount;
            newSongEntry.DrumrollCount = play.DrumrollCount;
            newSongEntry.ComboCount = play.ComboCount;
            newSongEntry.PlayTime = play.PlayTime;
            newSongEntry.SongNumber = play.SongNumber;

            songHistory.Add(newSongEntry);
        }

        var favoriteSongs = await userDatumService.GetFavoriteSongIds(baid);
        var favoriteSet = favoriteSongs.ToHashSet();
        foreach (var song in songHistory.Where(song => favoriteSet.Contains(song.SongId)))
        {
            song.IsFavorite = true;
        }

        return Ok(new SongHistoryResponse{SongHistoryData = songHistory});
    }
}