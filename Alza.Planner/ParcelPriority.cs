using Alza.Planner.Models;

namespace Alza.Planner;

/// <summary>
/// Priorities that decide the loading order of parcels: a higher value is loaded earlier.
/// </summary>
public static class ParcelPriority
{
    /// <summary>
    /// Revenue per unit of van capacity the parcel takes up (see <see cref="CapacityUsed"/>).
    /// Prefers parcels that earn a lot for the space and weight they need.
    /// </summary>
    public static Func<Parcel, double> RevenuePerCapacity(Fleet fleet, double weightShare) =>
        parcel => parcel.Revenue / CapacityUsed(parcel, fleet, weightShare);

    /// <summary>
    /// How much of a van's capacity the parcel takes up, as one number. A parcel uses two
    /// resources, so their shares of the van limits are mixed:
    /// <c>weightShare · weight / maxWeight + (1 − weightShare) · volume / maxVolume</c>.
    /// <paramref name="weightShare"/> says how much the weight limit counts
    /// (0 = only volume matters, 1 = only weight matters).
    /// </summary>
    public static double CapacityUsed(Parcel parcel, Fleet fleet, double weightShare) =>
        weightShare * parcel.WeightKg / fleet.VanMaxWeightKg +
        (1 - weightShare) * parcel.VolumeM3 / fleet.VanMaxVolumeM3;

    /// <summary>
    /// Baseline: the most valuable parcels first, regardless of their size.
    /// </summary>
    public static double Revenue(Parcel parcel) => parcel.Revenue;

    /// <summary>
    /// Baseline: revenue per cubic metre, the usual rule of thumb for a van that fills up.
    /// Same order as <see cref="RevenuePerCapacity"/> with weight share 0: ignores weight.
    /// </summary>
    public static double RevenuePerVolume(Parcel parcel) => parcel.Revenue / parcel.VolumeM3;

    /// <summary>
    /// Baseline: revenue per kilogram.
    /// Same order as <see cref="RevenuePerCapacity"/> with weight share 1: ignores volume.
    /// </summary>
    public static double RevenuePerWeight(Parcel parcel) => parcel.Revenue / parcel.WeightKg;
}
