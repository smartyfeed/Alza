using Alza.Planner.Generation;
using Alza.Planner.Models;

namespace Alza.Planner.Tests;

public class PlannerTests
{
    [Theory]
    [MemberData(nameof(TestData.PlannerAndProfileCases), MemberType = typeof(TestData))]
    public void Plan_IsValid(string plannerName, string profileName)
    {
        var parcels = TestData.Parcels(profileName, count: 30_000);
        var planner = TestData.Planner(plannerName, TestData.SmallFleet);

        var plan = planner.Plan(parcels);

        // No van is overloaded and every parcel is either in a van or left over, exactly once.
        Assert.Empty(PlanValidator.FindErrors(plan, parcels));
        Assert.Equal(TestData.SmallFleet.VanCount, plan.Vans.Count);
    }

    [Theory]
    [MemberData(nameof(TestData.PlannerAndProfileCases), MemberType = typeof(TestData))]
    public void Plan_RevenueNeverExceedsUpperBound(string plannerName, string profileName)
    {
        var parcels = TestData.Parcels(profileName, count: 30_000);
        var (_, upperBound) = RevenueUpperBound.FindTightest(parcels, TestData.SmallFleet);

        var plan = TestData.Planner(plannerName, TestData.SmallFleet).Plan(parcels);

        Assert.True(plan.TotalRevenue <= upperBound + 1e-6,
            $"Revenue {plan.TotalRevenue:N2} is above the upper bound {upperBound:N2}.");
    }

    [Theory]
    [MemberData(nameof(TestData.ProfileCases), MemberType = typeof(TestData))]
    public void AdaptivePlanner_IsNearOptimalOnRealisticData(string profileName)
    {
        // Full fleet and a busy day: demand is several times the capacity.
        var parcels = TestData.Parcels(profileName, count: 100_000);

        var plan = new AdaptiveGreedyPlanner(Fleet.Default).Plan(parcels);

        Assert.NotNull(plan.GapToUpperBound);
        Assert.InRange(plan.GapToUpperBound.Value, 0, 0.001); // at most 0.1 % below the bound
    }

    [Fact]
    public void AdaptivePlanner_ShipsEverythingOnLightDay()
    {
        // 60 000 typical parcels take about 80 % of the fleet's volume.
        var parcels = TestData.Parcels("Typical", count: 60_000);

        var plan = new AdaptiveGreedyPlanner(Fleet.Default).Plan(parcels);

        Assert.Empty(plan.LeftOver);
        Assert.Equal(parcels.Length, plan.ShippedCount);
    }

    [Fact]
    public void AdaptivePlanner_LeavesParcelThatFitsNoVan()
    {
        var fleet = Fleet.Default;
        Parcel[] parcels =
        [
            new(Id: 1, WeightKg: 10, VolumeM3: 0.1, Revenue: 100),
            new(Id: 2, WeightKg: 10, VolumeM3: fleet.VanMaxVolumeM3 + 1, Revenue: 1_000), // larger than a van
            new(Id: 3, WeightKg: fleet.VanMaxWeightKg + 1, VolumeM3: 0.1, Revenue: 1_000), // heavier than a van
        ];

        var plan = new AdaptiveGreedyPlanner(fleet).Plan(parcels);

        Assert.Equal(1, plan.ShippedCount);
        Assert.Equal(new[] { 2, 3 }, plan.LeftOver.Select(p => p.Id).Order());
        Assert.Empty(PlanValidator.FindErrors(plan, parcels));
    }

    [Fact]
    public void AdaptivePlanner_HandlesNoParcels()
    {
        var plan = new AdaptiveGreedyPlanner(Fleet.Default).Plan([]);

        Assert.Equal(0, plan.ShippedCount);
        Assert.Empty(plan.LeftOver);
        Assert.Equal(0.0, plan.TotalRevenue);
    }

    [Fact]
    public void AdaptivePlanner_MatchesHandCalculatedExample()
    {
        // One van of 1 m³; weight is no limit here. Revenue per m³: A 200, B 150, C 100.
        var fleet = new Fleet(VanCount: 1, VanMaxWeightKg: 1_000, VanMaxVolumeM3: 1);
        Parcel[] parcels =
        [
            new(Id: 1, WeightKg: 1, VolumeM3: 0.5, Revenue: 100), // A
            new(Id: 2, WeightKg: 1, VolumeM3: 0.4, Revenue: 60),  // B
            new(Id: 3, WeightKg: 1, VolumeM3: 0.3, Revenue: 30),  // C
        ];

        var plan = new AdaptiveGreedyPlanner(fleet).Plan(parcels);

        // Best real plan: A + B = 0.9 m³, 160 CZK; C no longer fits.
        Assert.Equal(160, plan.TotalRevenue, precision: 6);
        Assert.Equal(new[] { 3 }, plan.LeftOver.Select(p => p.Id));

        // Upper bound: A + B + a third of C (the 0.1 m³ left) = 100 + 60 + 10 = 170 CZK,
        // reached with only volume counting.
        Assert.Equal(170, plan.RevenueUpperBound!.Value, precision: 6);
        Assert.Equal(0.0, plan.WeightShare);
    }

    [Fact]
    public void UpperBound_IsNeverBelowExactOptimum()
    {
        foreach (var (seed, parcels, optimum) in SmallCasesWithExactOptimum())
        {
            var plan = new AdaptiveGreedyPlanner(SmallCaseFleet).Plan(parcels);

            Assert.Empty(PlanValidator.FindErrors(plan, parcels));
            Assert.True(plan.TotalRevenue <= optimum + 1e-6, $"Seed {seed}: the plan beats the exact optimum.");
            Assert.True(plan.RevenueUpperBound >= optimum - 1e-6, $"Seed {seed}: the bound is below the exact optimum.");
        }
    }

    [Fact]
    public void AdaptivePlanner_ReachesMostOfExactOptimumOnSmallCases()
    {
        // Only 2–4 parcels fit into a van here, the worst case for a greedy rule: one badly
        // chosen parcel blocks a large part of a van. Single cases drop to about 70 %.
        // On real data (hundreds of parcels per van) the gap is below 0.1 %, see above.
        var cases = SmallCasesWithExactOptimum();

        var planned = cases.Sum(c => new AdaptiveGreedyPlanner(SmallCaseFleet).Plan(c.Parcels).TotalRevenue);
        var optimum = cases.Sum(c => c.Optimum);

        Assert.True(planned >= 0.9 * optimum, $"All cases together reach {planned / optimum:P1} of the optimum.");
    }

    /// <summary>Two small vans and a dozen parcels that each take a large part of a van.</summary>
    private static readonly Fleet SmallCaseFleet = new(VanCount: 2, VanMaxWeightKg: 100, VanMaxVolumeM3: 1);

    /// <summary>30 small random cases with their exact optimum found by brute force.</summary>
    private static List<(int Seed, Parcel[] Parcels, double Optimum)> SmallCasesWithExactOptimum()
    {
        var profile = new ParcelProfile(
            MedianVolumeM3: 0.25, VolumeSpread: 0.5,
            MedianDensityKgPerM3: 150, DensitySpread: 0.5,
            MedianRevenue: 100, RevenueSpread: 0.5,
            RevenueSizeExponent: 0.5)
        {
            MaxVolumeM3 = 0.9,
            MaxWeightKg = 90,
        };

        var cases = new List<(int Seed, Parcel[] Parcels, double Optimum)>();
        for (var seed = 1; seed <= 30; seed++)
        {
            var parcels = new ParcelGenerator(profile, seed).Generate(12);
            cases.Add((seed, parcels, BruteForce.MaxRevenue(parcels, SmallCaseFleet)));
        }

        return cases;
    }
}
