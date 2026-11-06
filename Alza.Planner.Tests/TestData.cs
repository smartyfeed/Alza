using Alza.Planner.Generation;
using Alza.Planner.Models;

namespace Alza.Planner.Tests;

/// <summary>Shared test inputs: fleets, parcel profiles and planners by name.</summary>
public static class TestData
{
    /// <summary>
    /// A tenth of the real fleet. With a few tens of thousands of parcels it is already
    /// saturated, so the tests exercise the same situation as a real day but run fast.
    /// </summary>
    public static Fleet SmallFleet { get; } = Fleet.Default with { VanCount = 12 };

    public static IReadOnlyList<string> ProfileNames { get; } = ["Typical", "Heavy", "Mixed", "SizeCorrelated"];

    public static IReadOnlyList<string> PlannerNames { get; } = ["Adaptive", "RevenuePerVolume", "RevenuePerWeight", "Revenue"];

    /// <summary>Every profile, for theories.</summary>
    public static TheoryData<string> ProfileCases => new(ProfileNames);

    /// <summary>Every planner with every profile, for theories.</summary>
    public static TheoryData<string, string> PlannerAndProfileCases
    {
        get
        {
            var cases = new TheoryData<string, string>();
            foreach (var planner in PlannerNames)
            foreach (var profile in ProfileNames)
                cases.Add(planner, profile);

            return cases;
        }
    }

    public static ParcelProfile Profile(string name) => name switch
    {
        "Typical" => ParcelProfile.Typical,
        "Heavy" => ParcelProfile.Heavy,
        "Mixed" => ParcelProfile.Mixed,
        "SizeCorrelated" => ParcelProfile.SizeCorrelated,
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown profile."),
    };

    public static IDeliveryPlanner Planner(string name, Fleet fleet) => name switch
    {
        "Adaptive" => new AdaptiveGreedyPlanner(fleet),
        "RevenuePerVolume" => new GreedyPlanner(name, fleet, ParcelPriority.RevenuePerVolume),
        "RevenuePerWeight" => new GreedyPlanner(name, fleet, ParcelPriority.RevenuePerWeight),
        "Revenue" => new GreedyPlanner(name, fleet, ParcelPriority.Revenue),
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown planner."),
    };

    public static Parcel[] Parcels(string profileName, int count, int seed = 42) =>
        new ParcelGenerator(Profile(profileName), seed).Generate(count);
}
