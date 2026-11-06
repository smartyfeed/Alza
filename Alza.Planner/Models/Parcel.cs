namespace Alza.Planner.Models;

/// <summary>
/// A parcel waiting at the warehouse for delivery to an AlzaBox.
/// </summary>
/// <param name="Id">Unique parcel identifier.</param>
/// <param name="WeightKg">Weight in kilograms.</param>
/// <param name="VolumeM3">Volume in cubic metres (exact dimensions are ignored).</param>
/// <param name="Revenue">
/// Expected revenue in CZK. Stored as <see cref="double"/> rather than <see cref="decimal"/>:
/// it is a planning estimate, not an accounting amount, and the planner does millions of
/// arithmetic operations on it, where double is several times faster.
/// </param>
public readonly record struct Parcel(int Id, double WeightKg, double VolumeM3, double Revenue);
