using Fanex.Data.Repository;

namespace AutoPayout.Conditions.UnitTest.Services;
public class VerifyIntervalBasedOnGameSessionTests
{
    private readonly IDynamicRepository repository;

    private readonly ResultDetailService resultDetailService;

    private readonly VerifyFinalResult condition;

    public VerifyIntervalBasedOnGameSessionTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        resultDetailService = Substitute.For<ResultDetailService>();
        condition = new VerifyFinalResult(repository, resultDetailService);
    }

    //[Fact]
    //public void HandleCondition_
}
