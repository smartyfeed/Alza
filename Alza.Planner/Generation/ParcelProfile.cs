namespace Alza.Planner.Generation;

/// <summary>
/// Describes a synthetic parcel population for <see cref="ParcelGenerator"/>.
/// Volume, density and revenue are log-normally distributed: most parcels are small and cheap,
/// a few are large or valuable. "Median" is the typical value; "Spread" is the sigma of the
/// underlying normal distribution (about two thirds of the values lie within ×/÷ e^Spread of
/// the median, e.g. ×/÷ 1.65 for 0.5 and ×/÷ 2.7 for 1.0).
/// </summary>
/// <param name="MedianVolumeM3">Typical parcel volume.</param>
/// <param name="VolumeSpread">Spread of parcel volumes.</param>
/// <param name="MedianDensityKgPerM3">Typical parcel density; weight = volume × density.</param>
/// <param name="DensitySpread">Spread of parcel densities.</param>
/// <param name="MedianRevenue">Typical revenue of a parcel of median volume, in CZK.</param>
/// <param name="RevenueSpread">Spread of revenues.</param>
/// <param name="RevenueSizeExponent">
/// How revenue depends on size: 0 = independent of volume, 1 = proportional to volume.
/// </param>
public sealed record ParcelProfile(
    double MedianVolumeM3,
    double VolumeSpread,
    double MedianDensityKgPerM3,
    double DensitySpread,
    double MedianRevenue,
    double RevenueSpread,
    double RevenueSizeExponent)
{
    /// <summary>Smallest parcel volume (an envelope, 0.5 l).</summary>
    public double MinVolumeM3 { get; init; } = 0.0005;

    /// <summary>Largest parcel volume (200 l).</summary>
    public double MaxVolumeM3 { get; init; } = 0.2;

    /// <summary>Lightest parcel weight.</summary>
    public double MinWeightKg { get; init; } = 0.05;

    /// <summary>Heaviest parcel weight.</summary>
    public double MaxWeightKg { get; init; } = 50;

    /// <summary>
    /// Typical e-shop mix: mostly small, light parcels (median 8 l, about 1.4 kg);
    /// revenue does not depend on size. Volume is the van limit that binds first.
    /// </summary>
    public static ParcelProfile Typical { get; } = new(
        MedianVolumeM3: 0.008, VolumeSpread: 0.8,
        MedianDensityKgPerM3: 180, DensitySpread: 0.5,
        MedianRevenue: 120, RevenueSpread: 0.9,
        RevenueSizeExponent: 0);

    /// <summary>
    /// Dense goods (drinks, books, tools). The average density is above the van's
    /// 5 500 kg / 7 m³ ≈ 786 kg/m³, so the weight limit binds before the volume limit.
    /// </summary>
    public static ParcelProfile Heavy { get; } = Typical with
    {
        MedianDensityKgPerM3 = 900,
        DensitySpread = 0.4,
    };

    /// <summary>
    /// Mix of light and dense goods with a density around the van's 786 kg/m³: the weight
    /// and volume limits run out at the same time, so the planner has to balance them.
    /// </summary>
    public static ParcelProfile Mixed { get; } = Typical with
    {
        MedianDensityKgPerM3 = 800,
        DensitySpread = 0.4,
    };

    /// <summary>
    /// Revenue roughly proportional to volume, so revenue per m³ is similar for all parcels.
    /// The planner cannot simply pick "obviously profitable" parcels; this is the hard case.
    /// </summary>
    public static ParcelProfile SizeCorrelated { get; } = Typical with
    {
        RevenueSizeExponent = 1,
        RevenueSpread = 0.3,
    };
}
