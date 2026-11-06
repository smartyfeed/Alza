using System.Diagnostics;
using Alza.Planner;
using Alza.Planner.Generation;
using Alza.Planner.Models;

var fleet = Fleet.Default;
IDeliveryPlanner planner = new AdaptiveGreedyPlanner(fleet);

(string Name, ParcelProfile Profile, int ParcelCount)[] scenarios =
[
    ("Typical (volume limits)", ParcelProfile.Typical, 300_000),
    ("Light day (Tue/Thu)", ParcelProfile.Typical, 60_000),
    ("Heavy (weight limits)", ParcelProfile.Heavy, 300_000),
    ("Mixed (both limit)", ParcelProfile.Mixed, 300_000),
    ("Size-correlated revenue", ParcelProfile.SizeCorrelated, 300_000),
    ("Typical, 1 million", ParcelProfile.Typical, 1_000_000),
];

// The first call includes JIT compilation; a small warm-up run keeps the timings fair.
planner.Plan(new ParcelGenerator(ParcelProfile.Typical, seed: 1).Generate(10_000));

Console.WriteLine($"Fleet: {fleet.VanCount} vans, {fleet.TotalVolumeM3:N0} m³ and {fleet.TotalWeightKg / 1000:N0} t per round");
Console.WriteLine($"Planner: {planner.Name}");
Console.WriteLine();
Console.WriteLine(
    $"{"Scenario",-25} {"Parcels",9} {"Time ms",8} {"Shipped",9} {"Revenue CZK",13} {"Upper bound",13} " +
    $"{"Gap",7} {"W.share",7} {"Volume",7} {"Weight",7}  Check");

foreach (var (name, profile, parcelCount) in scenarios)
{
    var parcels = new ParcelGenerator(profile, seed: 42).Generate(parcelCount);

    var stopwatch = Stopwatch.StartNew();
    var plan = planner.Plan(parcels);
    stopwatch.Stop();

    var errors = PlanValidator.FindErrors(plan, parcels);
    var check = errors.Count == 0 ? "OK" : $"{errors.Count} errors";

    // When everything ships (light day), the loading order and its weight share do not matter.
    var weightShare = plan.LeftOver.Count == 0 ? "–" : $"{plan.WeightShare:F2}";

    Console.WriteLine(
        $"{name,-25} {parcelCount,9:N0} {stopwatch.ElapsedMilliseconds,8:N0} {plan.ShippedCount,9:N0} " +
        $"{plan.TotalRevenue,13:N0} {plan.RevenueUpperBound,13:N0} {plan.GapToUpperBound,7:P3} {weightShare,7} " +
        $"{plan.VolumeUtilization,7:P1} {plan.WeightUtilization,7:P1}  {check}");

    foreach (var error in errors.Take(5))
        Console.WriteLine($"    {error}");
}

Console.WriteLine();
Console.WriteLine("Gap = revenue missed compared with the upper bound (no plan can earn more than the bound).");
Console.WriteLine("W.share = how much weight counts in the loading priority (0 = only volume, 1 = only weight).");
Console.WriteLine("Volume/Weight = share of the fleet's capacity used.");
