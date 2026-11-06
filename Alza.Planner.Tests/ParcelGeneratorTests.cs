using Alza.Planner.Generation;

namespace Alza.Planner.Tests;

public class ParcelGeneratorTests
{
    [Fact]
    public void SameSeedProducesSameParcels()
    {
        var first = new ParcelGenerator(ParcelProfile.Typical, seed: 7).Generate(1_000);
        var second = new ParcelGenerator(ParcelProfile.Typical, seed: 7).Generate(1_000);

        Assert.Equal(first, second);
    }

    [Theory]
    [MemberData(nameof(TestData.ProfileCases), MemberType = typeof(TestData))]
    public void ParcelsStayWithinProfileLimits(string profileName)
    {
        var profile = TestData.Profile(profileName);

        var parcels = new ParcelGenerator(profile, seed: 7).Generate(10_000);

        Assert.All(parcels, parcel =>
        {
            Assert.InRange(parcel.VolumeM3, profile.MinVolumeM3, profile.MaxVolumeM3);
            Assert.InRange(parcel.WeightKg, profile.MinWeightKg, profile.MaxWeightKg);
            Assert.True(parcel.Revenue > 0);
        });
        Assert.Equal(parcels.Length, parcels.Select(p => p.Id).Distinct().Count());
    }
}
