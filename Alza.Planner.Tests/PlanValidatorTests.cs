using Alza.Planner.Models;

namespace Alza.Planner.Tests;

/// <summary>
/// The other tests rely on <see cref="PlanValidator"/>, so these make sure it really
/// catches broken plans. Broken plans are built by hand, bypassing the planners.
/// </summary>
public class PlanValidatorTests
{
    private static readonly Fleet Fleet = new(VanCount: 1, VanMaxWeightKg: 100, VanMaxVolumeM3: 1);

    private static readonly Parcel Small = new(Id: 1, WeightKg: 10, VolumeM3: 0.1, Revenue: 50);
    private static readonly Parcel Other = new(Id: 2, WeightKg: 10, VolumeM3: 0.1, Revenue: 50);

    [Fact]
    public void AcceptsValidPlan()
    {
        var van = EmptyVan();
        van.Add(Small);
        var plan = new DeliveryPlan(Fleet, [van], leftOver: [Other]);

        Assert.Empty(PlanValidator.FindErrors(plan, [Small, Other]));
    }

    [Fact]
    public void DetectsOverloadedVan()
    {
        // A van built with a larger capacity than the fleet allows lets the overload through.
        var oversizedVan = new VanLoad(vanNumber: 1, maxWeightKg: 1_000, maxVolumeM3: 10);
        var heavy = new Parcel(Id: 1, WeightKg: 150, VolumeM3: 0.1, Revenue: 50);
        oversizedVan.Add(heavy);
        var plan = new DeliveryPlan(Fleet, [oversizedVan], leftOver: []);

        var errors = PlanValidator.FindErrors(plan, [heavy]);

        Assert.Contains(errors, error => error.Contains("overloaded"));
    }

    [Fact]
    public void DetectsParcelPlannedTwice()
    {
        var van = EmptyVan();
        van.Add(Small);
        var plan = new DeliveryPlan(Fleet, [van], leftOver: [Small]);

        var errors = PlanValidator.FindErrors(plan, [Small]);

        Assert.Contains(errors, error => error.Contains("more than once"));
    }

    [Fact]
    public void DetectsMissingParcel()
    {
        var plan = new DeliveryPlan(Fleet, [EmptyVan()], leftOver: [Small]);

        var errors = PlanValidator.FindErrors(plan, [Small, Other]);

        Assert.Contains(errors, error => error.Contains("missing"));
    }

    [Fact]
    public void DetectsParcelNotInInput()
    {
        var plan = new DeliveryPlan(Fleet, [EmptyVan()], leftOver: [Small, Other]);

        var errors = PlanValidator.FindErrors(plan, [Small]);

        Assert.Contains(errors, error => error.Contains("not part of the input"));
    }

    private static VanLoad EmptyVan() => new(vanNumber: 1, Fleet.VanMaxWeightKg, Fleet.VanMaxVolumeM3);
}
