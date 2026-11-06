using Alza.Planner.Models;

namespace Alza.Planner;

/// <summary>
/// Greedy planner: orders parcels by a priority (most valuable first) and loads each one
/// into the van with room that suits it best. A parcel that fits nowhere stays in the warehouse,
/// and smaller parcels further down the order fill the gaps that are left.
/// </summary>
/// <remarks>
/// On light days (Tue/Thu) the whole demand fits within the fleet's total capacity, so the
/// goal is to ship everything rather than to choose. The planner then loads the largest
/// parcels first (first-fit decreasing), the standard way to pack items into bins.
/// No weekday is configured; the planner recognises the situation from the data.
/// </remarks>
public sealed class GreedyPlanner : IDeliveryPlanner
{
    private readonly Fleet _fleet;
    private readonly Func<Parcel, double> _priority;

    /// <param name="name">Name used in reports.</param>
    /// <param name="fleet">Vans available for the round.</param>
    /// <param name="priority">Loading priority; higher values are loaded first.</param>
    public GreedyPlanner(string name, Fleet fleet, Func<Parcel, double> priority)
    {
        Name = name;
        _fleet = fleet;
        _priority = priority;
    }

    public string Name { get; }

    public DeliveryPlan Plan(IReadOnlyList<Parcel> parcels)
    {
        // A parcel that does not fit even into an empty van can never be shipped.
        var candidates = new List<Parcel>(parcels.Count);
        var leftOver = new List<Parcel>();
        foreach (var parcel in parcels)
        {
            if (_fleet.FitsIntoVan(parcel))
                candidates.Add(parcel);
            else
                leftOver.Add(parcel);
        }

        var loadingOrder = FitsIntoFleet(candidates)
            ? SortDescending(candidates, SizeInVan)
            : SortDescending(candidates, _priority);

        var vans = CreateEmptyVans();
        foreach (var parcel in loadingOrder)
        {
            var van = FindBestVan(vans, parcel);
            if (van is null)
                leftOver.Add(parcel);
            else
                van.Add(parcel);
        }

        return new DeliveryPlan(_fleet, vans, leftOver);
    }

    /// <summary>True if total weight and volume are within the fleet's capacity (a light day).</summary>
    private bool FitsIntoFleet(List<Parcel> parcels) =>
        parcels.Sum(p => p.WeightKg) <= _fleet.TotalWeightKg &&
        parcels.Sum(p => p.VolumeM3) <= _fleet.TotalVolumeM3;

    /// <summary>Size of a parcel relative to a van: the larger of its weight and volume shares.</summary>
    private double SizeInVan(Parcel parcel) =>
        Math.Max(parcel.WeightKg / _fleet.VanMaxWeightKg, parcel.VolumeM3 / _fleet.VanMaxVolumeM3);

    private VanLoad[] CreateEmptyVans()
    {
        var vans = new VanLoad[_fleet.VanCount];
        for (var i = 0; i < vans.Length; i++)
            vans[i] = new VanLoad(vanNumber: i + 1, _fleet.VanMaxWeightKg, _fleet.VanMaxVolumeM3);

        return vans;
    }

    /// <summary>
    /// Picks the van whose free capacity best matches the parcel (the dot-product rule from
    /// vector bin packing): a small but heavy parcel goes where weight is still free, a bulky
    /// but light one where volume is free. Weight and volume of each van then fill up together.
    /// Simply taking the first van with room loses up to about 1 % of revenue when weight is
    /// a limit: the first vans fill up with bulky parcels and their weight capacity is wasted.
    /// Returns null if the parcel fits into no van.
    /// </summary>
    private VanLoad? FindBestVan(VanLoad[] vans, Parcel parcel)
    {
        var parcelWeightShare = parcel.WeightKg / _fleet.VanMaxWeightKg;
        var parcelVolumeShare = parcel.VolumeM3 / _fleet.VanMaxVolumeM3;

        VanLoad? bestVan = null;
        var bestMatch = double.MinValue;
        foreach (var van in vans)
        {
            if (!van.CanFit(parcel))
                continue;

            var match = parcelWeightShare * van.FreeWeightKg / _fleet.VanMaxWeightKg +
                        parcelVolumeShare * van.FreeVolumeM3 / _fleet.VanMaxVolumeM3;
            if (match > bestMatch)
            {
                bestMatch = match;
                bestVan = van;
            }
        }

        return bestVan;
    }

    /// <summary>
    /// Sorts by a key, highest first. Each key is computed once and Array.Sort sorts the
    /// parcels by the precomputed keys, which is much faster than LINQ OrderByDescending
    /// on hundreds of thousands of parcels.
    /// </summary>
    private static Parcel[] SortDescending(List<Parcel> parcels, Func<Parcel, double> key)
    {
        var sorted = parcels.ToArray();
        var keys = new double[sorted.Length];
        for (var i = 0; i < sorted.Length; i++)
            keys[i] = -key(sorted[i]); // negated: Array.Sort sorts ascending

        Array.Sort(keys, sorted);
        return sorted;
    }
}
