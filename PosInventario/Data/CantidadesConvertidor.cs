using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PosInventario.Data;

/// <summary>
/// SQLite no tiene un tipo decimal nativo y EF Core lo guarda como texto, lo
/// que dificulta comparar y ordenar cantidades. Las cantidades se guardan
/// como enteros en milésimas y se convierten a decimal al leerlas.
/// </summary>
public static class CantidadesConvertidor
{
    public const decimal Factor = 1000m;

    public static readonly ValueConverter<decimal, long> Instancia =
        new(
            valor => (long)decimal.Round(
                valor * Factor, 0, MidpointRounding.AwayFromZero),
            valor => valor / Factor);
}
