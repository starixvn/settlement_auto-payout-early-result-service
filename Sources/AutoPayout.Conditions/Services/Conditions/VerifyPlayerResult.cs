using Newtonsoft.Json;

namespace AutoPayout.Conditions.Services.Conditions;

public class VerifyPlayerResult(IDynamicRepository repository) : AbstractCondition
{
    private static List<int> player => [653, 654, 655, 685, 656, 657, 658, 686, 659, 660, 661, 687, 662, 663, 664, 688,
        665, 666, 667, 668, 669, 670, 671, 672, 673, 674, 675, 676, 678, 677, 679, 680, 681, 682, 683, 684];

    public async override Task HandleCondition(Match match)
    {
        var playerResult = match.ResultDetails.Where(x => player.Contains(x.ResultType)).ToList();
        playerResult.ForEach(x =>
        {
            var confirmScore = x.Scores.FirstOrDefault(s => s.SourceId == 0);
            if (confirmScore != null && string.IsNullOrEmpty(confirmScore.OtherInfo))
            {
            }
        });

        await HandleNextCondition(match);
    }

    private async Task<IEnumerable<OddFlexModel>> GetOddFlex(int matchId)
    {
        var criteria = new GetPlayerInfoCriteria
        {
            MatchId = matchId,
            SportType = SportType.BasketBall
        };

        var oddFlex = (await repository.FetchAsync<OddFlexModel>(criteria)).ToList();

        Parallel.ForEach(oddFlex, playerModel => playerModel.Info = JsonConvert.DeserializeObject<OddsInfoModel>(playerModel.OddsInfo));
        return oddFlex;
    }
}
