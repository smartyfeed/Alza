using Alza.Planner.Models;

namespace Alza.Planner.Tests;

/// <summary>
/// Exact optimum by trying every assignment: each parcel goes into one of the vans or stays.
/// That is (vans + 1)^parcels combinations, so it is only usable for a handful of parcels.
/// </summary>
internal static class BruteForce
{
    public static double MaxRevenue(IReadOnlyList<Parcel> parcels, Fleet fleet)
    {
        var weight = new double[fleet.VanCount];
        var volume = new double[fleet.VanCount];
        return BestFrom(0);

        // Best revenue obtainable from parcels[index..] with the vans loaded as they are now.
        double BestFrom(int index)
        {
            if (index == parcels.Count)
                return 0;

            var parcel = parcels[index];
            var best = BestFrom(index + 1); // the parcel stays in the warehouse

            for (var van = 0; van < fleet.VanCount; van++)
            {
                if (weight[van] + parcel.WeightKg > fleet.VanMaxWeightKg ||
                    volume[van] + parcel.VolumeM3 > fleet.VanMaxVolumeM3)
                    continue;

                weight[van] += parcel.WeightKg;
                volume[van] += parcel.VolumeM3;
                best = Math.Max(best, parcel.Revenue + BestFrom(index + 1));
                weight[van] -= parcel.WeightKg;
                volume[van] -= parcel.VolumeM3;
            }

            return best;
        }
    }
}
