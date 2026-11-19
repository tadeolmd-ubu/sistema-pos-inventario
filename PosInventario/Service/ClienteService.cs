using System.Globalization;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using PosInventario.Data;
using PosInventario.Models;
using PosInventario.Models.Enums;

namespace PosInventario.Service;

/// <summary>
/// Resumen de una venta a crédito dentro de la cuenta de un cliente.
/// </summary>
public sealed record VentaCreditoResumen(
    int IdVenta,
    DateTime Fecha,
    decimal Total,
    decimal Devuelto,
    decimal Abonado)
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    public decimal Pendiente => Math.Max(0m, Total - Devuelto - Abonado);

    public string Ticket => $"V-{IdVenta:000000}";

    public string FechaTexto =>
        Fecha.ToString("dd/MM/yyyy HH:mm", Cultura);

    public string TotalTexto => Total.ToString("0.00", Cultura);

    public string DevueltoTexto => Devuelto.ToString("0.00", Cultura);

    public string AbonadoTexto => Abonado.ToString("0.00", Cultura);

    public string PendienteTexto => Pendiente.ToString("0.00", Cultura);
}

public sealed record CuentaCliente(
    Cliente Cliente,
    decimal Saldo,
    IReadOnlyList<VentaCreditoResumen> Ventas,
    IReadOnlyList<Abono> Abonos);

/// <summary>
/// Catálogo de clientes y su cuenta de crédito. Los saldos se calculan a
/// partir de las ventas a crédito, devoluciones y abonos; no se guarda un
/// saldo duplicado en la base.
/// </summary>
public class ClienteService
{
    private const int MinNombre = 3;
    private const int MaxNombre = 100;
    private const int MaxTelefono = 30;
    private const int MaxEmail = 120;
    private const int MaxDireccion = 200;
    private const int MaxReferencia = 100;

    private readonly AppDbContext _db;
    private readonly SesionActual _sesion;

    public ClienteService(AppDbContext db, SesionActual sesion)
    {
        _db = db;
        _sesion = sesion;
    }

    public async Task<List<Cliente>> ObtenerActivosAsync()
        => await ObtenerClientesAsync(soloActivos: true);

    public async Task<List<Cliente>> ObtenerTodosAsync()
        => await ObtenerClientesAsync(soloActivos: false);

    private async Task<List<Cliente>> ObtenerClientesAsync(bool soloActivos)
    {
        var query = _db.Clientes.AsNoTracking().AsQueryable();

        if (soloActivos)
            query = query.Where(c => c.Activo == 1);

        var clientes = await query
            .OrderBy(c => c.Nombre)
            .ThenBy(c => c.Id)
            .ToListAsync();

        await AplicarSaldosAsync(clientes);

        return clientes;
    }

    public async Task<Cliente?> ObtenerPorIdAsync(int id)
        => await _db.Clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<CuentaCliente?> ObtenerCuentaAsync(int idCliente)
    {
        var cliente = await _db.Clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == idCliente);

        if (cliente is null)
            return null;

        var ventas = await _db.Ventas
            .AsNoTracking()
            .Include(v => v.Abonos)
            .Where(v => v.IdCliente == idCliente &&
                        v.TipoVenta == TipoVenta.Credito)
            .OrderByDescending(v => v.Fecha)
            .ThenByDescending(v => v.Id)
            .ToListAsync();

        var abonos = await _db.Abonos
            .AsNoTracking()
            .Include(a => a.Venta)
            .Include(a => a.Usuario)
            .Where(a => a.IdCliente == idCliente)
            .OrderByDescending(a => a.Fecha)
            .ThenByDescending(a => a.Id)
            .ToListAsync();

        var resumenes = ventas
            .Select(v => new VentaCreditoResumen(
                v.Id,
                v.Fecha,
                v.Total,
                v.TotalDevuelto,
                v.Abonos.Sum(a => a.Monto)))
            .ToList();

        var saldo = resumenes.Sum(v => v.Pendiente);

        cliente.SaldoActual = saldo;

        return new CuentaCliente(cliente, saldo, resumenes, abonos);
    }

    public async Task<decimal> ObtenerSaldoAsync(int idCliente)
    {
        var saldos = await CalcularSaldosAsync([idCliente]);
        return saldos.GetValueOrDefault(idCliente);
    }

    public async Task<Resultado<Cliente>> AgregarAsync(
        string nombre, string? telefono, string? email, string? direccion,
        decimal limiteCredito)
    {
        var autorizacion = ValidarAdministrador();
        if (autorizacion is not null)
            return new Resultado<Cliente>(false, autorizacion, null);

        var error = ValidarCampos(
            nombre, telefono, email, direccion, limiteCredito);

        if (error is not null)
            return new Resultado<Cliente>(false, error, null);

        var cliente = new Cliente
        {
            Nombre = nombre.Trim(),
            Telefono = NormalizarOpcional(telefono),
            Email = NormalizarOpcional(email),
            Direccion = NormalizarOpcional(direccion),
            LimiteCredito = limiteCredito,
            Activo = 1,
            FechaCreacion = DateTime.Now,
            FechaModificacion = DateTime.Now
        };

        _db.Clientes.Add(cliente);
        await _db.SaveChangesAsync();

        return new Resultado<Cliente>(true, "Cliente creado.", cliente);
    }

    public async Task<Resultado> ActualizarAsync(
        int id, string nombre, string? telefono, string? email,
        string? direccion, decimal limiteCredito)
    {
        var autorizacion = ValidarAdministrador();
        if (autorizacion is not null)
            return new Resultado(false, autorizacion);

        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == id);

        if (cliente is null)
            return new Resultado(false, "El cliente no existe.");

        var error = ValidarCampos(
            nombre, telefono, email, direccion, limiteCredito);

        if (error is not null)
            return new Resultado(false, error);

        cliente.Nombre = nombre.Trim();
        cliente.Telefono = NormalizarOpcional(telefono);
        cliente.Email = NormalizarOpcional(email);
        cliente.Direccion = NormalizarOpcional(direccion);
        cliente.LimiteCredito = limiteCredito;
        cliente.FechaModificacion = DateTime.Now;

        await _db.SaveChangesAsync();

        return new Resultado(true, "Cliente actualizado.");
    }

    public async Task<Resultado> ActivarAsync(int id)
    {
        var autorizacion = ValidarAdministrador();
        if (autorizacion is not null)
            return new Resultado(false, autorizacion);

        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == id);

        if (cliente is null)
            return new Resultado(false, "El cliente no existe.");

        cliente.Activo = 1;
        cliente.FechaModificacion = DateTime.Now;
        await _db.SaveChangesAsync();

        return new Resultado(true, "Cliente activado.");
    }

    public async Task<Resultado> DesactivarAsync(int id)
    {
        var autorizacion = ValidarAdministrador();
        if (autorizacion is not null)
            return new Resultado(false, autorizacion);

        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == id);

        if (cliente is null)
            return new Resultado(false, "El cliente no existe.");

        if (await TieneSaldoPendienteAsync(id))
            return new Resultado(
                false,
                "No se puede desactivar un cliente con saldo pendiente.");

        cliente.Activo = 0;
        cliente.FechaModificacion = DateTime.Now;
        await _db.SaveChangesAsync();

        return new Resultado(true, "Cliente desactivado.");
    }

    public async Task<Resultado<Abono>> RegistrarAbonoAsync(
        int idVenta, decimal monto, MetodoPago metodo, string? referencia)
    {
        var autorizacion = ValidarAdministrador();
        if (autorizacion is not null)
            return new Resultado<Abono>(false, autorizacion, null);

        if (monto <= 0)
            return new Resultado<Abono>(
                false, "El abono debe ser mayor que 0.", null);

        if (metodo is not (MetodoPago.Efectivo or MetodoPago.Tarjeta or
            MetodoPago.Transferencia))
        {
            return new Resultado<Abono>(
                false, "Selecciona un método de abono válido.", null);
        }

        var referenciaNormalizada = NormalizarOpcional(referencia);

        if (referenciaNormalizada?.Length > MaxReferencia)
            return new Resultado<Abono>(
                false,
                $"La referencia no puede superar los {MaxReferencia} caracteres.",
                null);

        var venta = await _db.Ventas
            .Include(v => v.Cliente)
            .Include(v => v.Abonos)
            .FirstOrDefaultAsync(v => v.Id == idVenta);

        if (venta is null)
            return new Resultado<Abono>(false, "La venta no existe.", null);

        if (venta.TipoVenta != TipoVenta.Credito || venta.IdCliente is null)
            return new Resultado<Abono>(
                false, "La venta no es una venta a crédito.", null);

        if (venta.Estado != EstadoVenta.Completada)
            return new Resultado<Abono>(
                false, "La venta no está completada.", null);

        var pendiente = venta.Total - venta.TotalDevuelto -
                        venta.Abonos.Sum(a => a.Monto);

        if (monto > pendiente)
        {
            return new Resultado<Abono>(
                false,
                $"El abono no puede superar el saldo pendiente " +
                $"({pendiente.ToString("0.00", CultureInfo.InvariantCulture)}).",
                null);
        }

        Caja? caja = null;

        if (metodo == MetodoPago.Efectivo)
        {
            caja = await _db.Cajas
                .FirstOrDefaultAsync(c => c.Estado == EstadoCaja.Abierta);

            if (caja is null)
            {
                return new Resultado<Abono>(
                    false,
                    "Abre una caja para registrar el abono en efectivo.",
                    null);
            }
        }

        var momento = DateTime.Now;

        var abono = new Abono
        {
            IdCliente = venta.IdCliente.Value,
            IdVenta = venta.Id,
            Monto = monto,
            Metodo = metodo,
            Referencia = referenciaNormalizada,
            Fecha = momento,
            IdUsuario = _sesion.IdUsuario
        };

        _db.Abonos.Add(abono);

        if (caja is not null)
        {
            _db.MovimientosCaja.Add(new MovimientoCaja
            {
                IdCaja = caja.Id,
                IdUsuario = _sesion.IdUsuario,
                Tipo = TipoMovimientoCaja.Ingreso,
                Monto = monto,
                Descripcion =
                    $"Abono {venta.Cliente?.Nombre ?? "cliente"} V-{venta.Id:000000}",
                IdVenta = venta.Id,
                Fecha = momento
            });
        }

        await _db.SaveChangesAsync();

        return new Resultado<Abono>(
            true,
            $"Abono registrado por {monto.ToString("0.00", CultureInfo.InvariantCulture)}.",
            abono);
    }

    private async Task AplicarSaldosAsync(IReadOnlyList<Cliente> clientes)
    {
        var saldos = await CalcularSaldosAsync(
            clientes.Select(c => c.Id).ToList());

        foreach (var cliente in clientes)
            cliente.SaldoActual = saldos.GetValueOrDefault(cliente.Id);
    }

    private async Task<Dictionary<int, decimal>> CalcularSaldosAsync(
        IReadOnlyCollection<int> ids)
    {
        var saldos = ids.ToDictionary(id => id, _ => 0m);

        if (ids.Count == 0)
            return saldos;

        var ventas = await _db.Ventas
            .AsNoTracking()
            .Where(v => v.IdCliente != null &&
                        ids.Contains(v.IdCliente.Value) &&
                        v.TipoVenta == TipoVenta.Credito &&
                        v.Estado == EstadoVenta.Completada)
            .Select(v => new
            {
                IdCliente = v.IdCliente!.Value,
                v.Total,
                v.TotalDevuelto
            })
            .ToListAsync();

        foreach (var venta in ventas)
        {
            if (saldos.ContainsKey(venta.IdCliente))
                saldos[venta.IdCliente] += venta.Total - venta.TotalDevuelto;
        }

        var abonos = await _db.Abonos
            .AsNoTracking()
            .Where(a => ids.Contains(a.IdCliente))
            .Select(a => new { a.IdCliente, a.Monto })
            .ToListAsync();

        foreach (var abono in abonos)
        {
            if (saldos.ContainsKey(abono.IdCliente))
                saldos[abono.IdCliente] -= abono.Monto;
        }

        foreach (var id in saldos.Keys)
            saldos[id] = Math.Max(0m, saldos[id]);

        return saldos;
    }

    private async Task<bool> TieneSaldoPendienteAsync(int idCliente)
        => await ObtenerSaldoAsync(idCliente) > 0m;

    private string? ValidarAdministrador()
        => _sesion.EsAdministrador
            ? null
            : "Solo un administrador puede modificar clientes.";

    private static string? ValidarCampos(
        string? nombre, string? telefono, string? email, string? direccion,
        decimal limiteCredito)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return "El nombre del cliente es obligatorio.";

        var nombreValor = nombre.Trim();

        if (nombreValor.Length < MinNombre)
            return $"El nombre debe tener al menos {MinNombre} caracteres.";

        if (nombreValor.Length > MaxNombre)
            return $"El nombre no puede superar los {MaxNombre} caracteres.";

        if (telefono?.Trim().Length > MaxTelefono)
            return $"El teléfono no puede superar los {MaxTelefono} caracteres.";

        if (email?.Trim().Length > MaxEmail)
            return $"El correo no puede superar los {MaxEmail} caracteres.";

        if (!string.IsNullOrWhiteSpace(email) &&
            !MailAddress.TryCreate(email.Trim(), out _))
        {
            return "El correo no tiene un formato válido.";
        }

        if (direccion?.Trim().Length > MaxDireccion)
            return $"La dirección no puede superar los {MaxDireccion} caracteres.";

        if (limiteCredito < 0)
            return "El límite de crédito no puede ser negativo.";

        return null;
    }

    private static string? NormalizarOpcional(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
