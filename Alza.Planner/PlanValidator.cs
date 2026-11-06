using Alza.Planner.Models;

namespace Alza.Planner;

/// <summary>
/// Independent check of a plan, used by the console app and the tests: no van is overloaded
/// and every input parcel ends up exactly once, either in a van or in the left-over list.
/// </summary>
public static class PlanValidator
{
    /// <summary>Allowance for floating-point rounding when summing many parcels.</summary>
    private const double Tolerance = 1e-9;

    /// <summary>Returns a description of every problem found; an empty list means the plan is valid.</summary>
    public static IReadOnlyList<string> FindErrors(DeliveryPlan plan, IReadOnlyList<Parcel> input)
    {
        var errors = new List<string>();
        var fleet = plan.Fleet;

        if (plan.Vans.Count > fleet.VanCount)
            errors.Add($"The plan uses {plan.Vans.Count} vans, the fleet has only {fleet.VanCount}.");

        foreach (var van in plan.Vans)
        {
            var weight = van.Parcels.Sum(p => p.WeightKg);
            var volume = van.Parcels.Sum(p => p.VolumeM3);

            if (weight > fleet.VanMaxWeightKg + Tolerance)
                errors.Add($"Van {van.VanNumber} is overloaded: {weight:N1} kg of {fleet.VanMaxWeightKg:N0} kg.");
            if (volume > fleet.VanMaxVolumeM3 + Tolerance)
                errors.Add($"Van {van.VanNumber} is overfull: {volume:N3} m³ of {fleet.VanMaxVolumeM3:N0} m³.");
        }

        var inputIds = input.Select(p => p.Id).ToHashSet();
        var plannedIds = new HashSet<int>(input.Count);
        var planned = plan.Vans.SelectMany(van => van.Parcels).Concat(plan.LeftOver);

        foreach (var parcel in planned)
        {
            if (!inputIds.Contains(parcel.Id))
                errors.Add($"Parcel {parcel.Id} is not part of the input.");
            else if (!plannedIds.Add(parcel.Id))
                errors.Add($"Parcel {parcel.Id} is planned more than once.");
        }

        var missing = inputIds.Count - plannedIds.Count;
        if (missing > 0)
            errors.Add($"{missing} input parcels are missing from the plan.");

        return errors;
    }
}
