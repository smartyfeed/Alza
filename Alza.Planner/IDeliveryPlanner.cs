using Alza.Planner.Models;

namespace Alza.Planner;

/// <summary>Decides which parcels go into which van for one delivery round.</summary>
public interface IDeliveryPlanner
{
    /// <summary>Short name used in reports.</summary>
    string Name { get; }

    /// <summary>
    /// Plans one round. Every input parcel ends up exactly once in the result:
    /// either in a van or in <see cref="DeliveryPlan.LeftOver"/>.
    /// </summary>
    DeliveryPlan Plan(IReadOnlyList<Parcel> parcels);
}
