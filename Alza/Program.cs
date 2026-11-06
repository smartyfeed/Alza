using System.Diagnostics;
using System.Text;
using Alza.Planner;
using Alza.Planner.Generation;
using Alza.Planner.Models;

// Windows consoles default to a legacy code page that cannot show "m³" or "–".
Console.OutputEncoding = Encoding.UTF8;

var fleet = Fleet.Default;
IDeliveryPlanner mainPlanner = new AdaptiveGreedyPlanner(fleet);

// Simpler loading orders for comparison. They load vans the same way as the main planner,
// so any difference in revenue comes only from the order in which parcels are loaded.
IDeliveryPlanner[] baselines =
[
    new GreedyPlanner("Fixed share 0.5", fleet, ParcelPriority.RevenuePerCapacity(fleet, weightShare: 0.5)),
    new GreedyPlanner("Revenue per m³", fleet, ParcelPriority.RevenuePerVolume),
    new GreedyPlanner("Revenue per kg", fleet, ParcelPriority.RevenuePerWeight),
    new GreedyPlanner("Revenue only", fleet, ParcelPriority.Revenue),
];

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
var warmUpParcels = new ParcelGenerator(ParcelProfile.Typical, seed: 1).Generate(10_000);
foreach (var planner in baselines.Prepend(mainPlanner))
    planner.Plan(warmUpParcels);

Console.WriteLine($"Fleet: {fleet.VanCount} vans, {fleet.TotalVolumeM3:N0} m³ and {fleet.TotalWeightKg / 1000:N0} t per round");
Console.WriteLine($"Planner: {mainPlanner.Name}");
Console.WriteLine();
Console.WriteLine(
    $"{"Scenario",-25} {"Parcels",9} {"Time ms",8} {"Shipped",9} {"Revenue CZK",13} {"Upper bound",13} " +
    $"{"Gap",7} {"W.share",7} {"Volume",7} {"Weight",7}  Check");

var comparisonRows = new List<string>();

foreach (var (name, profile, parcelCount) in scenarios)
{
    var parcels = new ParcelGenerator(profile, seed: 42).Generate(parcelCount);

    var stopwatch = Stopwatch.StartNew();
    var plan = mainPlanner.Plan(parcels);
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

    // The bound depends only on the parcels and the fleet, so it applies to every planner.
    var upperBound = plan.RevenueUpperBound ?? throw new InvalidOperationException("The main planner always reports a bound.");
    var gaps = baselines
        .Select(baseline => FormatGap(baseline.Plan(parcels), parcels, upperBound))
        .Prepend(FormatGap(plan, parcels, upperBound));
    comparisonRows.Add($"{name,-25} " + string.Join(" ", gaps.Select(gap => $"{gap,15}")));
}

Console.WriteLine();
Console.WriteLine("Gap = revenue missed compared with the upper bound (no plan can earn more than the bound).");
Console.WriteLine("W.share = how much weight counts in the loading priority (0 = only volume, 1 = only weight).");
Console.WriteLine("Volume/Weight = share of the fleet's capacity used.");

Console.WriteLine();
Console.WriteLine("Gap by loading order (lower is better):");
Console.WriteLine($"{"Scenario",-25} {"Adaptive",15} " + string.Join(" ", baselines.Select(baseline => $"{baseline.Name,15}")));
foreach (var row in comparisonRows)
    Console.WriteLine(row);

static string FormatGap(DeliveryPlan plan, IReadOnlyList<Parcel> parcels, double upperBound)
{
    if (PlanValidator.FindErrors(plan, parcels).Count > 0)
        return "invalid";

    var gap = Math.Max(0, (upperBound - plan.TotalRevenue) / upperBound); // a plan that reaches the bound can differ by rounding noise
    return $"{gap:P3}";
}
