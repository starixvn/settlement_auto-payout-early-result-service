using AutoPayout.EarlyResult.Services;
using AutoPayout.EarlyResult.Services.ExtraStepService;
using AutoPayout.Services;
using Fanex.Settlement.ScaleOut.Services;
using Xunit;

namespace AutoPayout.EarlyResult.UnitTests.Services;

public class SettlementDependenciesTests
{
    [Fact]
    public void Constructor_WithAllDependencies_InitializesProperties()
    {
        var scaleOutSettleService = Substitute.For<ScaleOutSettleService>();
        var createQueryService = Substitute.For<CreateQueryService>();
        var updateResultStatusService = Substitute.For<UpdateResultStatusService>();
        var createExtraStepService = Substitute.For<CreateExtraStepService>();

        var dependencies = new SettlementDependencies(
            scaleOutSettleService,
            createQueryService,
            updateResultStatusService,
            createExtraStepService);

        Assert.Equal(scaleOutSettleService, dependencies.ScaleOutSettleService);
        Assert.Equal(createQueryService, dependencies.CreateQueryService);
        Assert.Equal(updateResultStatusService, dependencies.UpdateResultStatusService);
        Assert.Equal(createExtraStepService, dependencies.CreateExtraStepService);
    }

    [Fact]
    public void Constructor_WithNullScaleOutSettleService_InitializesWithNull()
    {
        var createQueryService = Substitute.For<CreateQueryService>();
        var updateResultStatusService = Substitute.For<UpdateResultStatusService>();
        var createExtraStepService = Substitute.For<CreateExtraStepService>();

        var dependencies = new SettlementDependencies(
            null!,
            createQueryService,
            updateResultStatusService,
            createExtraStepService);

        Assert.Null(dependencies.ScaleOutSettleService);
        Assert.Equal(createQueryService, dependencies.CreateQueryService);
        Assert.Equal(updateResultStatusService, dependencies.UpdateResultStatusService);
        Assert.Equal(createExtraStepService, dependencies.CreateExtraStepService);
    }

    [Fact]
    public void Constructor_WithNullCreateQueryService_InitializesWithNull()
    {
        var scaleOutSettleService = Substitute.For<ScaleOutSettleService>();
        var updateResultStatusService = Substitute.For<UpdateResultStatusService>();
        var createExtraStepService = Substitute.For<CreateExtraStepService>();

        var dependencies = new SettlementDependencies(
            scaleOutSettleService,
            null!,
            updateResultStatusService,
            createExtraStepService);

        Assert.Equal(scaleOutSettleService, dependencies.ScaleOutSettleService);
        Assert.Null(dependencies.CreateQueryService);
        Assert.Equal(updateResultStatusService, dependencies.UpdateResultStatusService);
        Assert.Equal(createExtraStepService, dependencies.CreateExtraStepService);
    }

    [Fact]
    public void Constructor_WithNullUpdateResultStatusService_InitializesWithNull()
    {
        var scaleOutSettleService = Substitute.For<ScaleOutSettleService>();
        var createQueryService = Substitute.For<CreateQueryService>();
        var createExtraStepService = Substitute.For<CreateExtraStepService>();

        var dependencies = new SettlementDependencies(
            scaleOutSettleService,
            createQueryService,
            null!,
            createExtraStepService);

        Assert.Equal(scaleOutSettleService, dependencies.ScaleOutSettleService);
        Assert.Equal(createQueryService, dependencies.CreateQueryService);
        Assert.Null(dependencies.UpdateResultStatusService);
        Assert.Equal(createExtraStepService, dependencies.CreateExtraStepService);
    }

    [Fact]
    public void Constructor_WithNullCreateExtraStepService_InitializesWithNull()
    {
        var scaleOutSettleService = Substitute.For<ScaleOutSettleService>();
        var createQueryService = Substitute.For<CreateQueryService>();
        var updateResultStatusService = Substitute.For<UpdateResultStatusService>();

        var dependencies = new SettlementDependencies(
            scaleOutSettleService,
            createQueryService,
            updateResultStatusService,
            null!);

        Assert.Equal(scaleOutSettleService, dependencies.ScaleOutSettleService);
        Assert.Equal(createQueryService, dependencies.CreateQueryService);
        Assert.Equal(updateResultStatusService, dependencies.UpdateResultStatusService);
        Assert.Null(dependencies.CreateExtraStepService);
    }

    [Fact]
    public void Properties_AreReadOnly()
    {
        var scaleOutSettleService = Substitute.For<ScaleOutSettleService>();
        var createQueryService = Substitute.For<CreateQueryService>();
        var updateResultStatusService = Substitute.For<UpdateResultStatusService>();
        var createExtraStepService = Substitute.For<CreateExtraStepService>();

        var dependencies = new SettlementDependencies(
            scaleOutSettleService,
            createQueryService,
            updateResultStatusService,
            createExtraStepService);

        var originalScaleOut = dependencies.ScaleOutSettleService;
        var originalCreateQuery = dependencies.CreateQueryService;
        var originalUpdateResult = dependencies.UpdateResultStatusService;
        var originalCreateExtra = dependencies.CreateExtraStepService;

        Assert.Equal(originalScaleOut, dependencies.ScaleOutSettleService);
        Assert.Equal(originalCreateQuery, dependencies.CreateQueryService);
        Assert.Equal(originalUpdateResult, dependencies.UpdateResultStatusService);
        Assert.Equal(originalCreateExtra, dependencies.CreateExtraStepService);
    }
}

