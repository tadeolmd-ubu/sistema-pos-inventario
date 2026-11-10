using Microsoft.EntityFrameworkCore;
using PosInventario.Data;
using PosInventario.Models;
using PosInventario.Models.Enums;

namespace PosInventario.Service;

public class CajaService
{
    private const int MaxDescripcion = 200;
    private const int MaxObservaciones = 300;
    private const int LimiteHistorial = 50;

    private readonly AppDbContext _db;
    private readonly SesionActual _sesion;

    public CajaService(AppDbContext db, SesionActual sesion)
    {
        _db = db;
        _sesion = sesion;
    }

    /// <summary>
    /// Caja abierta visible para el usuario: el administrador ve cualquiera y
    /// el cajero solo la que él abrió.
    /// </summary>
    public async Task<Caja?> ObtenerCajaAbiertaAsync()
    {
        if (!_sesion.Iniciada) return null;

        var query = _db.Cajas
            .AsNoTracking()
            .Include(c => c.UsuarioApertura)
            .Where(c => c.Estado == EstadoCaja.Abierta);

        if (!_sesion.EsAdministrador)
            query = query.Where(c => c.IdUsuarioApertura == _sesion.IdUsuario);

        return await query.FirstOrDefaultAsync();
    }

    /// <summary>
    /// Indica si otro usuario tiene una caja abierta (solo para no
    /// administradores).
    /// </summary>
    public async Task<bool> HayOtraCajaAbiertaAsync()
    {
        if (!_sesion.Iniciada || _sesion.EsAdministrador)
            return false;

        return await _db.Cajas.AnyAsync(c =>
            c.Estado == EstadoCaja.Abierta &&
            c.IdUsuarioApertura != _sesion.IdUsuario);
    }

    public async Task<List<Caja>> ObtenerHistorialAsync(int limite = LimiteHistorial)
    {
        if (!_sesion.Iniciada) return [];

        if (limite <= 0)
            limite = LimiteHistorial;

        var query = _db.Cajas
            .AsNoTracking()
            .Include(c => c.UsuarioApertura)
            .Where(c => c.Estado == EstadoCaja.Cerrada);

        if (!_sesion.EsAdministrador)
            query = query.Where(c => c.IdUsuarioApertura == _sesion.IdUsuario);

        return await query
            .OrderByDescending(c => c.FechaCierre)
            .ThenByDescending(c => c.Id)
            .Take(limite)
            .ToListAsync();
    }

    public async Task<List<MovimientoCaja>> ObtenerMovimientosAsync(int idCaja)
    {
        if (!_sesion.Iniciada) return [];

        var caja = await _db.Cajas
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == idCaja);

        if (caja is null)
            return [];

        if (!_sesion.EsAdministrador && caja.IdUsuarioApertura != _sesion.IdUsuario)
            return [];

        return await _db.MovimientosCaja
            .AsNoTracking()
            .Include(m => m.Usuario)
            .Where(m => m.IdCaja == idCaja)
            .OrderByDescending(m => m.Fecha)
            .ThenByDescending(m => m.Id)
            .ToListAsync();
    }

    /// <summary>
    /// Efectivo que debería haber en la caja según sus movimientos.
    /// Devuelve null si la caja no existe o no es visible para el usuario.
    /// </summary>
    public async Task<decimal?> CalcularMontoEsperadoAsync(int idCaja)
    {
        if (!_sesion.Iniciada) return null;

        var caja = await _db.Cajas
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == idCaja);

        if (caja is null)
            return null;

        if (!_sesion.EsAdministrador && caja.IdUsuarioApertura != _sesion.IdUsuario)
            return null;

        return await CalcularMontoEsperadoInternoAsync(caja.Id, caja.MontoInicial);
    }

    public async Task<Resultado<Caja>> AbrirAsync(decimal montoInicial)
    {
        if (!_sesion.Iniciada)
            return new Resultado<Caja>(false, "La sesión no está iniciada.", null);

        if (montoInicial < 0)
            return new Resultado<Caja>(
                false, "El monto inicial no puede ser negativo.", null);

        if (await _db.Cajas.AnyAsync(c => c.Estado == EstadoCaja.Abierta))
            return new Resultado<Caja>(
                false, "Ya hay una caja abierta en el sistema.", null);

        var caja = new Caja
        {
            IdUsuarioApertura = _sesion.IdUsuario,
            FechaApertura = DateTime.Now,
            MontoInicial = montoInicial,
            Estado = EstadoCaja.Abierta
        };

        _db.Cajas.Add(caja);
        await _db.SaveChangesAsync();

        return new Resultado<Caja>(true, "Caja abierta.", caja);
    }

    public Task<Resultado> RegistrarIngresoAsync(decimal monto, string? descripcion)
        => RegistrarMovimientoAsync(TipoMovimientoCaja.Ingreso, monto, descripcion);

    public Task<Resultado> RegistrarRetiroAsync(decimal monto, string? descripcion)
        => RegistrarMovimientoAsync(TipoMovimientoCaja.Retiro, monto, descripcion);

    public async Task<Resultado> CerrarAsync(decimal montoFinal, string? observaciones)
    {
        if (!_sesion.Iniciada)
            return new Resultado(false, "La sesión no está iniciada.");

        if (montoFinal < 0)
            return new Resultado(false, "El monto contado no puede ser negativo.");

        var caja = await ObtenerCajaAbiertaParaOperarAsync();

        if (caja is null)
            return new Resultado(false, "No hay una caja abierta para cerrar.");

        var observacionesNormalizadas = NormalizarOpcional(observaciones);

        if (observacionesNormalizadas?.Length > MaxObservaciones)
            return new Resultado(
                false, $"Las observaciones no pueden superar los {MaxObservaciones} caracteres.");

        var esperado = await CalcularMontoEsperadoInternoAsync(
            caja.Id, caja.MontoInicial);

        caja.MontoEsperado = esperado;
        caja.MontoFinal = montoFinal;
        caja.Diferencia = montoFinal - esperado;
        caja.FechaCierre = DateTime.Now;
        caja.IdUsuarioCierre = _sesion.IdUsuario;
        caja.Observaciones = observacionesNormalizadas;
        caja.Estado = EstadoCaja.Cerrada;

        await _db.SaveChangesAsync();

        return new Resultado(true, "Caja cerrada.");
    }

    private async Task<Resultado> RegistrarMovimientoAsync(
        TipoMovimientoCaja tipo, decimal monto, string? descripcion)
    {
        if (!_sesion.Iniciada)
            return new Resultado(false, "La sesión no está iniciada.");

        if (monto <= 0)
            return new Resultado(false, "El monto debe ser mayor que 0.");

        var caja = await ObtenerCajaAbiertaParaOperarAsync();

        if (caja is null)
            return new Resultado(false, "No hay una caja abierta para operar.");

        var descripcionNormalizada = NormalizarOpcional(descripcion);

        if (tipo == TipoMovimientoCaja.Retiro &&
            (descripcionNormalizada is null || descripcionNormalizada.Length < 3))
            return new Resultado(
                false, "La descripción es obligatoria para los retiros.");

        if (descripcionNormalizada?.Length > MaxDescripcion)
            return new Resultado(
                false, $"La descripción no puede superar los {MaxDescripcion} caracteres.");

        if (tipo == TipoMovimientoCaja.Retiro)
        {
            var esperado = await CalcularMontoEsperadoInternoAsync(
                caja.Id, caja.MontoInicial);

            if (monto > esperado)
                return new Resultado(
                    false, "No hay efectivo suficiente en la caja para ese retiro.");
        }

        _db.MovimientosCaja.Add(new MovimientoCaja
        {
            IdCaja = caja.Id,
            IdUsuario = _sesion.IdUsuario,
            Tipo = tipo,
            Monto = monto,
            Descripcion = descripcionNormalizada,
            Fecha = DateTime.Now
        });

        await _db.SaveChangesAsync();

        return new Resultado(true,
            tipo == TipoMovimientoCaja.Ingreso
                ? "Ingreso registrado."
                : "Retiro registrado.");
    }

    private async Task<Caja?> ObtenerCajaAbiertaParaOperarAsync()
    {
        var caja = await _db.Cajas
            .FirstOrDefaultAsync(c => c.Estado == EstadoCaja.Abierta);

        if (caja is null)
            return null;

        if (!_sesion.EsAdministrador && caja.IdUsuarioApertura != _sesion.IdUsuario)
            return null;

        return caja;
    }

    private async Task<decimal> CalcularMontoEsperadoInternoAsync(
        int idCaja, decimal montoInicial)
    {
        var movimientos = await _db.MovimientosCaja
            .AsNoTracking()
            .Where(m => m.IdCaja == idCaja)
            .Select(m => new { m.Tipo, m.Monto })
            .ToListAsync();

        var esperado = montoInicial;

        foreach (var movimiento in movimientos)
        {
            esperado += movimiento.Tipo switch
            {
                TipoMovimientoCaja.Ingreso => movimiento.Monto,
                TipoMovimientoCaja.Venta => movimiento.Monto,
                TipoMovimientoCaja.Retiro => -movimiento.Monto,
                TipoMovimientoCaja.AnulacionVenta => -movimiento.Monto,
                _ => 0
            };
        }

        return esperado;
    }

    private static string? NormalizarOpcional(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
