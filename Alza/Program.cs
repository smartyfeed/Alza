using System.Diagnostics;
using Alza.Planner;
using Alza.Planner.Generation;
using Alza.Planner.Models;

var fleet = Fleet.Default;
IDeliveryPlanner planner = new GreedyPlanner(
    "Greedy, revenue per capacity (weight share 0.5)",
    fleet,
    ParcelPriority.RevenuePerCapacity(fleet, weightShare: 0.5));

(string Name, ParcelProfile Profile, int ParcelCount)[] scenarios =
[
    ("Typical day", ParcelProfile.Typical, 300_000),
    ("Light day (Tue/Thu)", ParcelProfile.Typical, 60_000),
    ("Heavy goods", ParcelProfile.Heavy, 300_000),
    ("Size-correlated revenue", ParcelProfile.SizeCorrelated, 300_000),
];

// The first call includes JIT compilation; a small warm-up run keeps the timings fair.
planner.Plan(new ParcelGenerator(ParcelProfile.Typical, seed: 1).Generate(10_000));

Console.WriteLine($"Fleet: {fleet.VanCount} vans, {fleet.TotalVolumeM3:N0} m³ and {fleet.TotalWeightKg / 1000:N0} t per round");
Console.WriteLine($"Planner: {planner.Name}");
Console.WriteLine();
Console.WriteLine(
    $"{"Scenario",-25} {"Parcels",9} {"Time ms",8} {"Shipped",9} {"Revenue CZK",13} " +
    $"{"of all",7} {"Volume",7} {"Weight",7}  Check");

foreach (var (name, profile, parcelCount) in scenarios)
{
    var parcels = new ParcelGenerator(profile, seed: 42).Generate(parcelCount);
    var demandRevenue = parcels.Sum(p => p.Revenue);

    var stopwatch = Stopwatch.StartNew();
    var plan = planner.Plan(parcels);
    stopwatch.Stop();

    var errors = PlanValidator.FindErrors(plan, parcels);
    var check = errors.Count == 0 ? "OK" : $"{errors.Count} errors";

    Console.WriteLine(
        $"{name,-25} {parcelCount,9:N0} {stopwatch.ElapsedMilliseconds,8:N0} {plan.ShippedCount,9:N0} " +
        $"{plan.TotalRevenue,13:N0} {plan.TotalRevenue / demandRevenue,7:P0} " +
        $"{plan.VolumeUtilization,7:P1} {plan.WeightUtilization,7:P1}  {check}");

    foreach (var error in errors.Take(5))
        Console.WriteLine($"    {error}");
}

Console.WriteLine();
Console.WriteLine("Revenue \"of all\" = share of the revenue of all waiting parcels; Volume/Weight = fleet capacity used.");
