using Alza.Planner.Models;

namespace Alza.Planner;

/// <summary>
/// Upper bound on the revenue that any valid plan can reach in one round. Comparing a plan
/// with it shows how far from optimal the plan can be at most, without knowing the optimum.
/// </summary>
/// <remarks>
/// Every valid plan keeps the total weight within the fleet's weight capacity and the total
/// volume within its volume capacity. Mixing these two conditions with a weight share gives
/// one condition that every valid plan satisfies as well: the summed
/// <see cref="ParcelPriority.CapacityUsed"/> is at most <see cref="Fleet.VanCount"/>
/// (a van holds exactly 1 unit). With a single condition, the best "fractional" solution is
/// easy to find: take parcels by revenue per capacity, and of the first parcel that no longer
/// fits take just the fraction that does. No real plan can beat it – real plans have to obey
/// both conditions, per van, and cannot ship fractions of parcels – so its revenue is an
/// upper bound for every weight share, and the lowest of them is the tightest.
/// (In optimisation terms: a surrogate relaxation of the capacity constraints with the
/// Dantzig bound of the resulting knapsack problem.)
/// </remarks>
public static class RevenueUpperBound
{
    /// <summary>
    /// Searches the weight share with the tightest (lowest) bound: first in steps of 0.1,
    /// then in steps of 0.01 around the best value. Grid points are evaluated in parallel.
    /// </summary>
    public static (double WeightShare, double Bound) FindTightest(IReadOnlyList<Parcel> parcels, Fleet fleet)
    {
        var candidates = ShippableParcels(parcels, fleet);

        var coarse = TightestOnGrid(candidates, fleet, from: 0, to: 1, step: 0.1);
        return TightestOnGrid(
            candidates, fleet,
            from: Math.Max(0, coarse.WeightShare - 0.1),
            to: Math.Min(1, coarse.WeightShare + 0.1),
            step: 0.01);
    }

    private static (double WeightShare, double Bound) TightestOnGrid(
        Parcel[] candidates, Fleet fleet, double from, double to, double step)
    {
        var pointCount = (int)Math.Round((to - from) / step) + 1;
        var weightShares = new double[pointCount];
        var bounds = new double[pointCount];

        Parallel.For(0, pointCount, i =>
        {
            weightShares[i] = Math.Min(to, Math.Round(from + i * step, 10));
            bounds[i] = Compute(candidates, fleet, weightShares[i]);
        });

        var best = Array.IndexOf(bounds, bounds.Min());
        return (weightShares[best], bounds[best]);
    }

    private static double Compute(Parcel[] candidates, Fleet fleet, double weightShare)
    {
        // Sort parcels by revenue per capacity, best first (keys are negated: Array.Sort sorts ascending).
        var negatedRatio = new double[candidates.Length];
        var capacityUsed = new double[candidates.Length];
        for (var i = 0; i < candidates.Length; i++)
        {
            capacityUsed[i] = ParcelPriority.CapacityUsed(candidates[i], fleet, weightShare);
            negatedRatio[i] = -candidates[i].Revenue / capacityUsed[i];
        }

        Array.Sort(negatedRatio, capacityUsed);

        // Measured in CapacityUsed units, one van holds exactly 1, so the fleet holds VanCount.
        double freeCapacity = fleet.VanCount;
        double bound = 0;
        for (var i = 0; i < candidates.Length; i++)
        {
            var revenue = -negatedRatio[i] * capacityUsed[i];
            if (capacityUsed[i] <= freeCapacity)
            {
                bound += revenue;
                freeCapacity -= capacityUsed[i];
            }
            else
            {
                bound += revenue * freeCapacity / capacityUsed[i]; // the fraction that still fits
                break;
            }
        }

        return bound;
    }

    /// <summary>Parcels that fit into an empty van; the others can never be shipped.</summary>
    private static Parcel[] ShippableParcels(IReadOnlyList<Parcel> parcels, Fleet fleet) =>
        parcels.Where(fleet.FitsIntoVan).ToArray();
}
