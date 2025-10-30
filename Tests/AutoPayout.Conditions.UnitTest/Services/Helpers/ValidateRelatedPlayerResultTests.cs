using AutoPayout.Conditions.Services.HelperService;
using AutoPayout.Models;
using AutoPayout.Constants;

namespace AutoPayout.Conditions.UnitTests.Services.Helpers
{
    public class ValidateRelatedPlayerResultTests
    {
        [Fact]
        public void IsHomeValueSumValid_ValidSum_ReturnsTrue()
        {
            var result = new ResultDetail
            {
                HomeValue = "15",
                RelatedResults = new List<ResultDetail>
                {
                    new() { HomeValue = "10" },
                    new() { HomeValue = "5" }
                }
            };

            Assert.True(result.IsHomeValueSumValid());
        }

        [Fact]
        public void IsHomeValueSumValid_InvalidSum_ReturnsFalse()
        {
            var result = new ResultDetail
            {
                HomeValue = "12",
                RelatedResults = new List<ResultDetail>
                {
                    new() { HomeValue = "10" },
                    new() { HomeValue = "5" }
                }
            };

            Assert.False(result.IsHomeValueSumValid());
        }

        [Fact]
        public void IsHomeValueSumValid_NonNumericRelated_ReturnsFalse()
        {
            var result = new ResultDetail
            {
                HomeValue = "10",
                RelatedResults = new List<ResultDetail>
                {
                    new() { HomeValue = "x" },
                    new() { HomeValue = "5" }
                }
            };
            Assert.False(result.IsHomeValueSumValid());
        }

        [Fact]
        public void IsValidDoubleDouble_Y_WithTwoStatsAboveThreshold_ReturnsTrue()
        {
            var result = new ResultDetail
            {
                HomeValue = "Y",
                RelatedResults = new List<ResultDetail>
                {
                    new() { HomeValue = "10" },
                    new() { HomeValue = "11" },
                    new() { HomeValue = "3" },
                    new() { HomeValue = "2" },
                }
            };

            Assert.True(result.IsValidDoubleDouble());
        }

        [Fact]
        public void IsValidDoubleDouble_Y_NotEnoughStatsAboveThreshold_ReturnsFalse()
        {
            var result = new ResultDetail
            {
                HomeValue = "Y",
                RelatedResults = new List<ResultDetail>
                {
                    new() { HomeValue = "10" },
                    new() { HomeValue = "9" },
                    new() { HomeValue = "1" },
                    new() { HomeValue = "2" },
                }
            };

            Assert.False(result.IsValidDoubleDouble());
        }

        [Fact]
        public void IsValidDoubleDouble_N_WithFourStatsBelowThreshold_ReturnsTrue()
        {
            var result = new ResultDetail
            {
                HomeValue = "N",
                RelatedResults = new List<ResultDetail>
                {
                    new() { HomeValue = "9" },
                    new() { HomeValue = "8" },
                    new() { HomeValue = "1" },
                    new() { HomeValue = "2" },
                }
            };

            Assert.True(result.IsValidDoubleDouble());
        }

        [Fact]
        public void IsValidDoubleDouble_InvalidHomeValue_ReturnsFalse()
        {
            var result = new ResultDetail
            {
                HomeValue = "?",
                RelatedResults = new List<ResultDetail>
                {
                    new() { HomeValue = "12" },
                    new() { HomeValue = "11" },
                    new() { HomeValue = "10" }
                }
            };
            Assert.False(result.IsValidDoubleDouble());
        }

        [Fact]
        public void IsValidTrippleDouble_Y_WithThreeStatsAboveThreshold_ReturnsTrue()
        {
            var result = new ResultDetail
            {
                HomeValue = "Y",
                RelatedResults = new List<ResultDetail>
                {
                    new() { HomeValue = "10" },
                    new() { HomeValue = "12" },
                    new() { HomeValue = "15" },
                    new() { HomeValue = "3" }
                }
            };

            Assert.True(result.IsValidTrippleDouble());
        }

        [Fact]
        public void IsValidTrippleDouble_Y_NotEnoughAboveThreshold_ReturnsFalse()
        {
            var result = new ResultDetail
            {
                HomeValue = "Y",
                RelatedResults = new List<ResultDetail>
                {
                    new() { HomeValue = "10" },
                    new() { HomeValue = "9" },
                    new() { HomeValue = "8" }
                }
            };
            Assert.False(result.IsValidTrippleDouble());
        }

        [Fact]
        public void IsValidTrippleDouble_N_WithLessThanThreeStatsAboveThreshold_ReturnsTrue()
        {
            var result = new ResultDetail
            {
                HomeValue = "N",
                RelatedResults = new List<ResultDetail>
                {
                    new() { HomeValue = "10" },
                    new() { HomeValue = "5" },
                    new() { HomeValue = "3" }
                }
            };

            Assert.True(result.IsValidTrippleDouble());
        }

        [Fact]
        public void IsValidHeadToHead_FirstPlayerWins_ReturnsTrueWhenBetChoiceMatches()
        {
            var first = new ResultDetail { HomeValue = "25", OtherInfo = "101,1" };
            var second = new ResultDetail { HomeValue = "18", OtherInfo = "102,2" };

            var main = new ResultDetail
            {
                HomeValue = "P1",
                OddFlex = new List<OddFlexModel>
                {
                    new() { Info = new OddsInfoModel { PlayerId =101, TeamId =1 }, BetChoice = "P1" },
                    new() { Info = new OddsInfoModel { PlayerId =102, TeamId =2 }, BetChoice = "P2" }
                },
                RelatedResults = new List<ResultDetail> { first, second }
            };

            Assert.True(main.IsValidHeadToHead());
        }

        [Fact]
        public void IsValidHeadToHead_SecondPlayerWins_MismatchChoice_ReturnsFalse()
        {
            var first = new ResultDetail { HomeValue = "10", OtherInfo = "101,1" };
            var second = new ResultDetail { HomeValue = "18", OtherInfo = "102,2" };

            var main = new ResultDetail
            {
                HomeValue = "P1", // expect P2 since second wins
                OddFlex = new List<OddFlexModel>
                {
                    new() { Info = new OddsInfoModel { PlayerId =101, TeamId =1 }, BetChoice = "P1" },
                    new() { Info = new OddsInfoModel { PlayerId =102, TeamId =2 }, BetChoice = "P2" }
                },
                RelatedResults = new List<ResultDetail> { first, second }
            };
            Assert.False(main.IsValidHeadToHead());
        }

        [Fact]
        public void IsValidHeadToHead_WrongRelatedCount_ReturnsFalse()
        {
            var main = new ResultDetail
            {
                HomeValue = "P1",
                OddFlex = new List<OddFlexModel>(),
                RelatedResults = new List<ResultDetail> { new() { HomeValue = "10" } }
            };
            Assert.False(main.IsValidHeadToHead());
        }

        [Fact]
        public void IsValidHeadToHead_InvalidScores_ReturnsFalse()
        {
            var main = new ResultDetail
            {
                HomeValue = "P1",
                OddFlex = new List<OddFlexModel>(),
                RelatedResults = new List<ResultDetail> { new() { HomeValue = "a" }, new() { HomeValue = "b" } }
            };

            Assert.False(main.IsValidHeadToHead());
        }

        [Fact]
        public void IsValidPerformanceOther_WinnerAndStatisticsSatisfied_ReturnsTrueWhenHomeValueY()
        {
            var main = new ResultDetail
            {
                ResourceId = "67501",
                HomeValue = "Y",
                OddFlex = new List<OddFlexModel>
                {
                    new()
                    {
                        ResourceId = "67501",
                        Info = new OddsInfoModel
                        {
                            TeamId =1,
                            IsHome = true,
                            PlayerId =999,
                            Results = new List<ResultInfo>
                            {
                                new() { Type = "winner", Value = "1" }
                            },
                            Statistics = new List<Statistic>
                            {
                                new() { Type = "points", Value = "20+" },
                                new() { Type = "rebounds", Value = "10+" }
                            }
                        }
                    }
                },
                RelatedResults = new List<ResultDetail>
                {
                    new() { ResourceId = ResourceIds.FullTime, HomeValue = "110", AwayValue = "100" },
                    new() { ResourceId = "65301", HomeValue = "25" }, // points
                    new() { ResourceId = "65901", HomeValue = "12" } // rebounds
                }
            };

            Assert.True(main.IsValidPerformanceOther());
        }

        [Fact]
        public void IsValidPerformanceOther_WinnerMismatch_ReturnsFalseWhenHomeValueY()
        {
            var main = new ResultDetail
            {
                ResourceId = "67501",
                HomeValue = "Y",
                OddFlex = new List<OddFlexModel>
                {
                    new()
                    {
                        ResourceId = "67501",
                        Info = new OddsInfoModel
                        {
                            TeamId =1,
                            IsHome = true,
                            PlayerId =999,
                            Results = new List<ResultInfo>
                            {
                                new() { Type = "winner", Value = "1" }
                            },
                            Statistics = new List<Statistic>()
                        }
                    }
                },
                RelatedResults = new List<ResultDetail>
                {
                    new() { ResourceId = ResourceIds.FullTime, HomeValue = "90", AwayValue = "100" } // away wins
                }
            };
            Assert.False(main.IsValidPerformanceOther());
        }

        [Fact]
        public void IsValidPerformanceOther_StatisticConditionFails_ReturnsFalseWhenHomeValueY()
        {
            var main = new ResultDetail
            {
                ResourceId = "67501",
                HomeValue = "Y",
                OddFlex = new List<OddFlexModel>
                {
                    new()
                    {
                        ResourceId = "67501",
                        Info = new OddsInfoModel
                        {
                            TeamId =1,
                            IsHome = true,
                            PlayerId =999,
                            Statistics = new List<Statistic>
                            {
                                new() { Type = "points", Value = "30+" }
                            }
                        }
                    }
                },
                RelatedResults = new List<ResultDetail>
                {
                    new() { ResourceId = "65301", HomeValue = "20" }
                }
            };
            Assert.False(main.IsValidPerformanceOther());
        }

        [Fact]
        public void IsValidPerformanceOther_NoGameResultsNoStats_ReturnsYWhenHomeValueY()
        {
            var main = new ResultDetail
            {
                ResourceId = "67501",
                HomeValue = "Y",
                OddFlex = new List<OddFlexModel>
                {
                    new()
                    {
                        ResourceId = "67501",
                        Info = new OddsInfoModel
                        {
                            TeamId =1,
                            IsHome = true,
                            PlayerId =999,
                            Results = new List<ResultInfo>(),
                            Statistics = new List<Statistic>()
                        }
                    }
                }
            };
            Assert.True(main.IsValidPerformanceOther());
        }

        [Fact]
        public void ValidateByConsistenceRule_RoutesToCorrectRule()
        {
            // Sum rule (669)
            var sum = new ResultDetail
            {
                ResourceId = "66901",
                HomeValue = "15",
                RelatedResults = new List<ResultDetail> { new() { HomeValue = "10" }, new() { HomeValue = "5" } }
            };
            Assert.True(sum.ValidateByConsistenceRule());

            // Double double (677)
            var dd = new ResultDetail
            {
                ResourceId = "67701",
                HomeValue = "Y",
                RelatedResults = new List<ResultDetail> { new() { HomeValue = "10" }, new() { HomeValue = "12" } }
            };
            Assert.True(dd.ValidateByConsistenceRule());

            // Head to head (680)
            var first = new ResultDetail { HomeValue = "30", OtherInfo = "201,1" };
            var second = new ResultDetail { HomeValue = "10", OtherInfo = "202,2" };
            var h2h = new ResultDetail
            {
                ResourceId = "68001",
                HomeValue = "A",
                OddFlex = new List<OddFlexModel>
                {
                    new() { Info = new OddsInfoModel { PlayerId =201, TeamId =1 }, BetChoice = "A" },
                    new() { Info = new OddsInfoModel { PlayerId =202, TeamId =2 }, BetChoice = "B" }
                },
                RelatedResults = new List<ResultDetail> { first, second }
            };
            Assert.True(h2h.ValidateByConsistenceRule());

            // Unknown goes default true
            var unknown = new ResultDetail { ResourceId = "99999", HomeValue = "X" };
            Assert.True(unknown.ValidateByConsistenceRule());
        }
    }
}
