using Alza.Planner.Models;

namespace Alza.Planner;

/// <summary>
/// The main planner. Loads parcels greedily by revenue per unit of van capacity
/// (<see cref="GreedyPlanner"/>), with the weight share of that capacity tuned to each
/// round's parcels, and reports how close the plan is to the best possible revenue.
/// </summary>
/// <remarks>
/// Which limit matters depends on the parcels: for typical light parcels only volume runs out
/// (best weight share 0), for heavy goods only weight (1), for a mix both at once (in between).
/// The planner uses the weight share with the tightest <see cref="RevenueUpperBound"/>: there the
/// bound's fractional solution takes the most valuable parcels for the capacity that is really
/// scarce, which is the loading order the greedy planner needs. The same search gives the
/// upper bound, so every plan comes with a proven limit on how much revenue was missed.
/// </remarks>
public sealed class AdaptiveGreedyPlanner : IDeliveryPlanner
{
    private readonly Fleet _fleet;

    public AdaptiveGreedyPlanner(Fleet fleet)
    {
        _fleet = fleet;
    }

    public string Name => "Adaptive greedy (weight share tuned per round)";

    public DeliveryPlan Plan(IReadOnlyList<Parcel> parcels)
    {
        var (weightShare, upperBound) = RevenueUpperBound.FindTightest(parcels, _fleet);

        var greedy = new GreedyPlanner(Name, _fleet, ParcelPriority.RevenuePerCapacity(_fleet, weightShare));
        var plan = greedy.Plan(parcels);

        return new DeliveryPlan(plan.Fleet, plan.Vans, plan.LeftOver)
        {
            RevenueUpperBound = upperBound,
            WeightShare = weightShare,
        };
    }
}
