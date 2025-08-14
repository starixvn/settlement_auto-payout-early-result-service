namespace AutoPayout.Conditions.Services.HelperService.VerifyFinalResult;

public interface MuayThaiHelperService
{
    (string Home, string Away, string Round, bool correctScore) MapSboToSettle(int homeValue, int awayValue);
}

public class MuayThaiHelperServiceImp : MuayThaiHelperService
{
    public (string Home, string Away, string Round, bool correctScore) MapSboToSettle(int homeValue, int awayValue)
    {
        return homeValue switch
        {
            >= 1 and <= 5 when awayValue == 0 => ("1", "0", homeValue.ToString(), true),
            0 when awayValue is >= 1 and <= 5 => ("0", "1", awayValue.ToString(), true),
            6 when awayValue == 0 => ("5", "0", string.Empty, true),
            0 when awayValue == 6 => ("0", "5", string.Empty, true),
            6 when awayValue == 6 => ("5", "5", string.Empty, true),
            _ => (homeValue.ToString(), awayValue.ToString(), string.Empty, true)
        };
    }
}