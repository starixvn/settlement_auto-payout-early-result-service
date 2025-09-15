namespace AutoPayout.Conditions.Criteria;

public class GetPlayerInfoCriteria : CriteriaBase
{
    public int MatchId { get; set; }
    public int SportType { get; set; }
    public override string GetSettingKey()
    {
        throw new NotImplementedException();
    }

    public override bool IsValid()
    {
        throw new NotImplementedException();
    }
}
