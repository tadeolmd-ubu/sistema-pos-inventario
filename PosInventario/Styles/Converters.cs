using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using PosInventario.Models.Enums;

namespace PosInventario.Styles;

public class InicialesConverter : IValueConverter
{
    public object? Convert(
        object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var nombre = value as string;
        if (string.IsNullOrWhiteSpace(nombre)) return "?";

        var partes = nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var iniciales = char.ToUpperInvariant(partes[0][0]).ToString();

        if (partes.Length > 1)
            iniciales += char.ToUpperInvariant(partes[1][0]);

        return iniciales;
    }

    public object? ConvertBack(
        object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class FechaCortaConverter : IValueConverter
{
    public object? Convert(
        object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            DateOnly fecha => fecha.ToString("dd/MM/yyyy"),
            DateTimeOffset fecha => fecha.ToString("dd/MM/yyyy"),
            DateTime fecha => fecha.ToString("dd/MM/yyyy"),
            _ => "—"
        };

    public object? ConvertBack(
        object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class ActivoConverter : IValueConverter
{
    public object? Convert(
        object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int activo) return null;

        var esActivo = activo == 1;
        var parametro = parameter?.ToString();

        return parametro switch
        {
            "nombre" => esActivo ? "Activo" : "Inactivo",
            "texto" => new SolidColorBrush(
                Color.Parse(esActivo ? "#2EBD85" : "#5C6F80")),
            _ => new SolidColorBrush(
                Color.Parse(esActivo ? "#EAFCEF" : "#EEF2F6"))
        };
    }

    public object? ConvertBack(
        object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class VistaActualConverter : IValueConverter
{
    public string Modo { get; set; } = "fondo";

    public object? Convert(
        object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var activa = parameter?.ToString() is { } nombre &&
                     value?.GetType().Name == nombre;

        return Modo switch
        {
            "texto" => new SolidColorBrush(
                Color.Parse(activa ? "#F4FBFF" : "#0A2E4F")),
            "barra" => (double)(activa ? 1 : 0),
            _ => new SolidColorBrush(
                Color.Parse(activa ? "#0A2E4F" : "#5FFFFFFF"))
        };
    }

    public object? ConvertBack(
        object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class ActivoFondoConverter : IValueConverter
{
    private static readonly SolidColorBrush FondoInactivo =
        new(Color.Parse("#F8D7DA"));

    public object? Convert(
        object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int activo && activo == 0
            ? FondoInactivo
            : Brushes.Transparent;

    public object? ConvertBack(
        object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class MetodoPagoNombreConverter : IValueConverter
{
    public object? Convert(
        object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is MetodoPago metodo ? metodo.Nombre() : value;

    public object? ConvertBack(
        object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class ArchivadoFondoConverter : IValueConverter
{
    private static readonly SolidColorBrush FondoArchivado =
        new(Color.Parse("#F8D7DA"));

    public object? Convert(
        object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is DateOnly
            ? FondoArchivado
            : Brushes.Transparent;

    public object? ConvertBack(
        object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
