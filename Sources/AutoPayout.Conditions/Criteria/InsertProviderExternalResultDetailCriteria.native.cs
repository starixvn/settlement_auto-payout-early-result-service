namespace AutoPayout.Conditions.Criteria;

public class InsertProviderExternalResultDetailCriteria : NonQueryCommand
{
    public int MatchId { get; set; }

    public int SourceId { get; set; } = 97;

    public string Resources { get; set; }

    public override string GetSettingKey() => "NAP_Admin_ProviderExternalResultDetailOSIns";

    public override bool IsValid() => MatchId > 0;
}