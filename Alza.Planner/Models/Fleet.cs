namespace Alza.Planner.Models;

/// <summary>
/// Vans available for one delivery round. All vans are identical.
/// </summary>
/// <param name="VanCount">Number of vans leaving the warehouse in one round.</param>
/// <param name="VanMaxWeightKg">Maximum load weight of one van in kilograms.</param>
/// <param name="VanMaxVolumeM3">Load volume of one van in cubic metres.</param>
public sealed record Fleet(int VanCount, double VanMaxWeightKg, double VanMaxVolumeM3)
{
    /// <summary>The fleet from the assignment: 120 vans, 5.5 t and 7 m³ each.</summary>
    public static Fleet Default { get; } = new(VanCount: 120, VanMaxWeightKg: 5_500, VanMaxVolumeM3: 7);

    /// <summary>Weight the whole fleet can carry in one round.</summary>
    public double TotalWeightKg => VanCount * VanMaxWeightKg;

    /// <summary>Volume the whole fleet can carry in one round.</summary>
    public double TotalVolumeM3 => VanCount * VanMaxVolumeM3;

    /// <summary>True if the parcel fits into an empty van on its own.</summary>
    public bool FitsIntoVan(Parcel parcel) =>
        parcel.WeightKg <= VanMaxWeightKg && parcel.VolumeM3 <= VanMaxVolumeM3;
}
