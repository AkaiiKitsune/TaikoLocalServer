using static MudBlazor.Colors;
using System;
using static MudBlazor.CategoryTypes;
using System.Globalization;

namespace TaikoWebUI.Pages;

public partial class LastPlays
{
    [Parameter]
    public int Baid { get; set; }

    private const string IconStyle = "width:25px; height:25px;";

    private string searchString = "";

    private SongHistoryResponse? response;

    private Dictionary<DateTime, List<SongHistoryData>> songHistoryDataMap = new();


    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        response = await Client.GetFromJsonAsync<SongHistoryResponse>($"api/PlayHistory/{Baid}");
        response.ThrowIfNull();

        response.SongHistoryData.ForEach(data =>
        {
            var songId = data.SongId;
            data.Genre = GameDataService.GetMusicGenreBySongId(songId);
            data.MusicName = GameDataService.GetMusicNameBySongId(songId);
            data.MusicArtist = GameDataService.GetMusicArtistBySongId(songId);
            data.Stars = GameDataService.GetMusicStarLevel(songId, data.Difficulty);
            data.ShowDetails = false;
        });

        songHistoryDataMap = response.SongHistoryData.GroupBy(data => data.PlayTime).ToDictionary(data => data.Key,data => data.ToList());
        foreach (var songHistoryDataList in songHistoryDataMap.Values)
        {
            songHistoryDataList.Sort((data1, data2) => data1.SongNumber.CompareTo(data2.SongNumber));
        }
    }

    private static string GetCrownText(CrownType crown)
    {
        return crown switch
        {
            CrownType.None => "Fail",
            CrownType.Clear => "Clear",
            CrownType.Gold => "Full Combo",
            CrownType.Dondaful => "Donderful Combo",
            _ => ""
        };
    }

    private static string GetRankText(ScoreRank rank)
    {
        return rank switch
        {
            ScoreRank.White => "Stylish",
            ScoreRank.Bronze => "Stylish",
            ScoreRank.Silver => "Stylish",
            ScoreRank.Gold => "Graceful",
            ScoreRank.Sakura => "Graceful",
            ScoreRank.Purple => "Graceful",
            ScoreRank.Dondaful => "Top Class",
            _ => ""
        };
    }

    private static string GetDifficultyTitle(Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Easy => "Easy",
            Difficulty.Normal => "Normal",
            Difficulty.Hard => "Hard",
            Difficulty.Oni => "Oni",
            Difficulty.UraOni => "Ura Oni",
            _ => ""
        };
    }

    private static string GetDifficultyIcon(Difficulty difficulty)
    {
        return $"<image href='/images/difficulty_{difficulty}.png' alt='{difficulty}' width='24' height='24'/>";
    }

    private static string GetGenreTitle(SongGenre genre)
    {
        return genre switch
        {
            SongGenre.Pop => "Pop",
            SongGenre.Anime => "Anime",
            SongGenre.Kids => "Kids",
            SongGenre.Vocaloid => "Vocaloid",
            SongGenre.GameMusic => "Game Music",
            SongGenre.NamcoOriginal => "NAMCO Original",
            SongGenre.Variety => "Variety",
            SongGenre.Classical => "Classical",
            _ => ""
        };
    }

    private static string GetGenreStyle(SongGenre genre)
    {
        return genre switch
        {
            SongGenre.Pop => "background: #42c0d2; color: #fff",
            SongGenre.Anime => "background: #ff90d3; color: #fff",
            SongGenre.Kids => "background: #fec000; color: #fff",
            SongGenre.Vocaloid => "background: #ddd; color: #000",
            SongGenre.GameMusic => "background: #cc8aea; color: #fff",
            SongGenre.NamcoOriginal => "background: #ff7027; color: #fff",
            SongGenre.Variety => "background: #1dc83b; color: #fff",
            SongGenre.Classical => "background: #bfa356; color: #000",
            _ => ""
        };
    }

    //private static void ToggleShowAiData(SongBestData data)
    //{
    //    data.ShowAiData = !data.ShowAiData;
    //}

    //private static bool IsAiDataPresent(SongBestData data)
    //{
    //    var aiData = data.AiSectionBestData;

    //    return aiData.Count > 0;
    //}

    //public void RowClicked(TableRowClickEventArgs<SongHistoryData> p)
    //{
    //    p.Item.ShowDetails = !p.Item.ShowDetails;
    //}

    private bool FilterFunc(List<SongHistoryData> element)
    {
        /// TODO : Add more filters

        if (string.IsNullOrWhiteSpace(searchString))
            return true;
        if (element[0].PlayTime.ToString("dddd d MMMM yyyy - HH:mm",CultureInfo.CreateSpecificCulture("fr-FR")).Contains(searchString, StringComparison.OrdinalIgnoreCase))
            return true;
        if (element[0].PlayTime.ToString().Contains(searchString, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }
}

