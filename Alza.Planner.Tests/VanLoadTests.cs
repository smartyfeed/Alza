using Alza.Planner.Models;

namespace Alza.Planner.Tests;

public class VanLoadTests
{
    [Fact]
    public void CanBeLoadedUpToExactLimits()
    {
        var van = new VanLoad(vanNumber: 1, maxWeightKg: 100, maxVolumeM3: 1);

        van.Add(new Parcel(Id: 1, WeightKg: 60, VolumeM3: 0.5, Revenue: 10));
        van.Add(new Parcel(Id: 2, WeightKg: 40, VolumeM3: 0.5, Revenue: 20));

        Assert.Equal(100, van.WeightKg);
        Assert.Equal(1, van.VolumeM3);
        Assert.Equal(30, van.Revenue);
        Assert.Equal(0, van.FreeWeightKg);
        Assert.Equal(0, van.FreeVolumeM3);
    }

    [Theory]
    [InlineData(101, 0.5)] // too heavy
    [InlineData(50, 1.01)] // too big
    public void RefusesParcelThatDoesNotFit(double weightKg, double volumeM3)
    {
        var van = new VanLoad(vanNumber: 1, maxWeightKg: 100, maxVolumeM3: 1);
        var parcel = new Parcel(Id: 1, WeightKg: weightKg, VolumeM3: volumeM3, Revenue: 10);

        Assert.False(van.CanFit(parcel));
        Assert.Throws<InvalidOperationException>(() => van.Add(parcel));
        Assert.Empty(van.Parcels);
    }
}
