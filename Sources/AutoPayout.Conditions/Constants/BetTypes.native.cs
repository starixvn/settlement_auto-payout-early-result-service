namespace AutoPayout.Conditions.Constants;
public static class BetTypes
{
    public const string SetXWinner = "3903";
    public const string SetXRaceToPoints = "3915";

    public static List<string> PlayerDependentResources => ["669", "671", "673", "674", "675", "676", "677", "678", "679", "680", "681", "682", "683", "684"]; // has oddlex
    public static List<string> Player => ["653", "654", "655", "685", "656", "657", "658", "686", "659", "660", "661", "687", "662", "663", "664", "688", "665", "666"
        , "667", "668", "669", "670", "671", "672", "673", "674", "675", "676", "678", "677", "679", "680", "681", "682", "683", "684"];
}