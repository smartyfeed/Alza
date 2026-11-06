namespace Alza.Planner.Models;

/// <summary>
/// Cargo of one van in one delivery round. Keeps running totals so that checking
/// whether another parcel fits is a constant-time operation.
/// </summary>
public sealed class VanLoad
{
    private readonly List<Parcel> _parcels = [];

    public VanLoad(int vanNumber, double maxWeightKg, double maxVolumeM3)
    {
        VanNumber = vanNumber;
        MaxWeightKg = maxWeightKg;
        MaxVolumeM3 = maxVolumeM3;
    }

    /// <summary>1-based number of the van within the fleet.</summary>
    public int VanNumber { get; }

    public double MaxWeightKg { get; }
    public double MaxVolumeM3 { get; }

    public IReadOnlyList<Parcel> Parcels => _parcels;

    public double WeightKg { get; private set; }
    public double VolumeM3 { get; private set; }
    public double Revenue { get; private set; }

    public double FreeWeightKg => MaxWeightKg - WeightKg;
    public double FreeVolumeM3 => MaxVolumeM3 - VolumeM3;

    /// <summary>True if the parcel still fits, both by weight and by volume.</summary>
    public bool CanFit(Parcel parcel) =>
        WeightKg + parcel.WeightKg <= MaxWeightKg &&
        VolumeM3 + parcel.VolumeM3 <= MaxVolumeM3;

    /// <summary>Loads the parcel. Only planners inside this library can change a van's cargo.</summary>
    internal void Add(Parcel parcel)
    {
        if (!CanFit(parcel))
            throw new InvalidOperationException($"Parcel {parcel.Id} does not fit into van {VanNumber}.");

        _parcels.Add(parcel);
        WeightKg += parcel.WeightKg;
        VolumeM3 += parcel.VolumeM3;
        Revenue += parcel.Revenue;
    }
}
