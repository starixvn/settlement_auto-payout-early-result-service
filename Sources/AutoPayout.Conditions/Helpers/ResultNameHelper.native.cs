namespace AutoPayout.Conditions.Helpers;
public static class ResultNameHelper
{
    private static readonly Dictionary<(int, string), string> ResultNameDictionary = new()
    {

        {(SportType.Tennis, "01") , "Set 1"},
        {(SportType.Tennis, "02") , "Set 2"},
        {(SportType.Tennis, "03") , "Set 3"},
        {(SportType.Tennis, "04") , "Set 4"},
        {(SportType.Tennis, "05") , "Set 5"},
        {(SportType.Badminton, "0101") , "Game 1"},
        {(SportType.Badminton, "0102") , "Game 2"},
        {(SportType.Badminton, "0103") , "Game 3"},
        {(SportType.Badminton, "0104") , "Game 4"},
        {(SportType.Badminton, "0105") , "Game 5"},
        {(SportType.ESports, "M1") , "Map 1"},
        {(SportType.ESports, "M2") , "Map 2"},
        {(SportType.ESports, "M3") , "Map 3"},
        {(SportType.ESports, "M4") , "Map 4"},
        {(SportType.ESports, "M5") , "Map 5"},
    };

    public static string GetResultsName(string resultIds, int sportType)
    {
        var ids = resultIds.Split(',');
        var resultNames = new List<string>();
        foreach (var id in ids)
        {
            if (ResultNameDictionary.TryGetValue((sportType, id), out var name))
            {
                resultNames.Add(name);
            }
        }

        return string.Join(", ", resultNames);
    }
}
