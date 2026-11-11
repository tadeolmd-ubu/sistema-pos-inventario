namespace PosInventario.Service;

/// <summary>
/// Ancho del papel térmico soportado por el ticket.
/// </summary>
public enum AnchoTicket
{
    Mm58,
    Mm80
}

public static class AnchoTicketExtensiones
{
    public static int Milimetros(this AnchoTicket ancho)
        => ancho == AnchoTicket.Mm58 ? 58 : 80;

    /// <summary>
    /// Columnas de texto que caben en el ancho indicado usando una fuente
    /// monoespaciada. Es la base para que el ticket sea legible en ambos
    /// tamaños de papel.
    /// </summary>
    public static int Columnas(this AnchoTicket ancho)
        => ancho == AnchoTicket.Mm58 ? 32 : 48;

    public static string Etiqueta(this AnchoTicket ancho)
        => $"{ancho.Milimetros()} mm";
}

/// <summary>
/// Opción del selector de ancho de papel del ticket.
/// </summary>
public sealed record AnchoTicketOpcion(AnchoTicket Ancho)
{
    public string Etiqueta => Ancho.Etiqueta();
}
