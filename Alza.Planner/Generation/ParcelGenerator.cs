using Alza.Planner.Models;

namespace Alza.Planner.Generation;

/// <summary>
/// Generates random parcels following a <see cref="ParcelProfile"/>.
/// The assignment comes without data, so the planner is evaluated on synthetic parcels.
/// The same seed always produces the same parcels, so results can be reproduced.
/// Calling <see cref="Generate"/> again continues the random sequence (e.g. new parcels for the next round).
/// </summary>
public sealed class ParcelGenerator
{
    private readonly ParcelProfile _profile;
    private readonly Random _random;
    private int _nextId = 1;

    public ParcelGenerator(ParcelProfile profile, int seed)
    {
        _profile = profile;
        _random = new Random(seed);
    }

    /// <summary>Generates <paramref name="count"/> new parcels with consecutive ids.</summary>
    public Parcel[] Generate(int count)
    {
        var parcels = new Parcel[count];
        for (var i = 0; i < count; i++)
            parcels[i] = NextParcel();

        return parcels;
    }

    private Parcel NextParcel()
    {
        var volume = Math.Clamp(
            NextLogNormal(_profile.MedianVolumeM3, _profile.VolumeSpread),
            _profile.MinVolumeM3, _profile.MaxVolumeM3);

        var density = NextLogNormal(_profile.MedianDensityKgPerM3, _profile.DensitySpread);
        var weight = Math.Clamp(volume * density, _profile.MinWeightKg, _profile.MaxWeightKg);

        var sizeFactor = Math.Pow(volume / _profile.MedianVolumeM3, _profile.RevenueSizeExponent);
        var revenue = NextLogNormal(_profile.MedianRevenue, _profile.RevenueSpread) * sizeFactor;

        // Round to realistic precision: grams, decilitres, hellers (revenue stays positive).
        return new Parcel(
            Id: _nextId++,
            WeightKg: Math.Round(weight, 3),
            VolumeM3: Math.Round(volume, 4),
            Revenue: Math.Max(Math.Round(revenue, 2), 0.01));
    }

    /// <summary>Log-normal random value with the given median and spread (sigma).</summary>
    private double NextLogNormal(double median, double spread) =>
        median * Math.Exp(spread * NextStandardNormal());

    /// <summary>Standard normal random value (Box–Muller transform).</summary>
    private double NextStandardNormal()
    {
        var u1 = 1.0 - _random.NextDouble(); // (0, 1], avoids Log(0)
        var u2 = _random.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}
