using Alza.Planner.Generation;
using Alza.Planner.Models;

var fleet = Fleet.Default;

(string Name, ParcelProfile Profile, int ParcelCount)[] scenarios =
[
    ("Typical day", ParcelProfile.Typical, 300_000),
    ("Light day (Tue/Thu)", ParcelProfile.Typical, 60_000),
    ("Heavy goods", ParcelProfile.Heavy, 300_000),
    ("Size-correlated revenue", ParcelProfile.SizeCorrelated, 300_000),
];

Console.WriteLine($"Fleet: {fleet.VanCount} vans, {fleet.TotalVolumeM3:N0} m³ and {fleet.TotalWeightKg / 1000:N0} t per round");
Console.WriteLine();
Console.WriteLine($"{"Scenario",-25} {"Parcels",9} {"Volume m³",10} {"of fleet",9} {"Weight t",9} {"of fleet",9} {"Revenue CZK",14}");

foreach (var (name, profile, parcelCount) in scenarios)
{
    var parcels = new ParcelGenerator(profile, seed: 42).Generate(parcelCount);

    var volume = parcels.Sum(p => p.VolumeM3);
    var weight = parcels.Sum(p => p.WeightKg);
    var revenue = parcels.Sum(p => p.Revenue);

    Console.WriteLine(
        $"{name,-25} {parcelCount,9:N0} {volume,10:N0} {volume / fleet.TotalVolumeM3,9:P0} " +
        $"{weight / 1000,9:N0} {weight / fleet.TotalWeightKg,9:P0} {revenue,14:N0}");
}
