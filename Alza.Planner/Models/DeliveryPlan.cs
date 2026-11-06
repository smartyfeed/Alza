namespace Alza.Planner.Models;

/// <summary>
/// Result of planning one delivery round: what each van carries and which parcels
/// stay in the warehouse for a later round.
/// </summary>
public sealed class DeliveryPlan
{
    public DeliveryPlan(Fleet fleet, IReadOnlyList<VanLoad> vans, IReadOnlyList<Parcel> leftOver)
    {
        Fleet = fleet;
        Vans = vans;
        LeftOver = leftOver;
    }

    public Fleet Fleet { get; }

    /// <summary>One entry per van of the fleet, including vans that stay empty.</summary>
    public IReadOnlyList<VanLoad> Vans { get; }

    /// <summary>Parcels not loaded in this round; they wait for the next one.</summary>
    public IReadOnlyList<Parcel> LeftOver { get; }

    public int ShippedCount => Vans.Sum(van => van.Parcels.Count);
    public double TotalRevenue => Vans.Sum(van => van.Revenue);
    public double TotalWeightKg => Vans.Sum(van => van.WeightKg);
    public double TotalVolumeM3 => Vans.Sum(van => van.VolumeM3);

    /// <summary>Share of the fleet's weight capacity that is used (0–1).</summary>
    public double WeightUtilization => TotalWeightKg / Fleet.TotalWeightKg;

    /// <summary>Share of the fleet's volume capacity that is used (0–1).</summary>
    public double VolumeUtilization => TotalVolumeM3 / Fleet.TotalVolumeM3;

    /// <summary>
    /// Proven maximum revenue for this round, if the planner computed one: no valid plan
    /// can earn more (see <see cref="Planner.RevenueUpperBound"/>).
    /// </summary>
    public double? RevenueUpperBound { get; init; }

    /// <summary>
    /// How far the revenue is below <see cref="RevenueUpperBound"/>, as a share of it
    /// (0 = provably optimal). Null if no bound is known.
    /// </summary>
    public double? GapToUpperBound =>
        RevenueUpperBound is { } bound && bound > 0
            ? Math.Max(0, (bound - TotalRevenue) / bound) // a plan that reaches the bound can differ by rounding noise
            : null;

    /// <summary>
    /// Weight share of the loading priority, if the planner chose one
    /// (see <see cref="ParcelPriority.CapacityUsed"/>).
    /// </summary>
    public double? WeightShare { get; init; }
}
