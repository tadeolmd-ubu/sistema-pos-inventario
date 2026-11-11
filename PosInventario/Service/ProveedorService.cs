using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using PosInventario.Data;
using PosInventario.Models;

namespace PosInventario.Service;

public class ProveedorService
{
    private const int MinNombre = 3;
    private const int MaxNombre = 100;
    private const int MinTelefono = 3;
    private const int MaxTelefono = 30;
    private const int MaxEmail = 120;
    private const int MaxDireccion = 200;

    private readonly AppDbContext _db;
    private readonly SesionActual _sesion;

    public ProveedorService(AppDbContext db, SesionActual sesion)
    {
        _db = db;
        _sesion = sesion;
    }

    public async Task<List<Proveedor>> ObtenerTodosAsync()
        => await _db.Proveedores
            .AsNoTracking()
            .OrderBy(p => p.Nombre)
            .ToListAsync();

    public async Task<List<Proveedor>> ObtenerActivosAsync()
        => await _db.Proveedores
            .AsNoTracking()
            .Where(p => p.Activo == 1)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

    public async Task<Proveedor?> ObtenerPorIdAsync(int id)
        => await _db.Proveedores
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Resultado<Proveedor>> AgregarAsync(
        string nombre, string telefono, string? email, string? direccion)
    {
        var autorizacion = ValidarAdministrador();
        if (autorizacion is not null)
            return new Resultado<Proveedor>(false, autorizacion.Mensaje, null);

        var error = ValidarCampos(nombre, telefono, email, direccion);
        if (error is not null)
            return new Resultado<Proveedor>(false, error, null);

        var telefonoNormalizado = telefono.Trim();

        if (await _db.Proveedores.AnyAsync(p => p.Telefono == telefonoNormalizado))
            return new Resultado<Proveedor>(
                false, "Ya existe un proveedor con ese teléfono.", null);

        var proveedor = new Proveedor
        {
            Nombre = nombre.Trim(),
            Telefono = telefonoNormalizado,
            Email = NormalizarOpcional(email),
            Direccion = NormalizarOpcional(direccion),
            Activo = 1,
            FechaCreacion = DateTime.Now
        };

        _db.Proveedores.Add(proveedor);
        await _db.SaveChangesAsync();

        return new Resultado<Proveedor>(true, "Proveedor creado.", proveedor);
    }

    public async Task<Resultado> ActualizarAsync(
        int id, string nombre, string telefono, string? email, string? direccion)
    {
        var autorizacion = ValidarAdministrador();
        if (autorizacion is not null)
            return autorizacion;

        var proveedor = await _db.Proveedores.FirstOrDefaultAsync(p => p.Id == id);

        if (proveedor is null)
            return new Resultado(false, "El proveedor no existe.");

        var error = ValidarCampos(nombre, telefono, email, direccion);
        if (error is not null)
            return new Resultado(false, error);

        var telefonoNormalizado = telefono.Trim();

        if (await _db.Proveedores.AnyAsync(p =>
                p.Id != id && p.Telefono == telefonoNormalizado))
            return new Resultado(false, "Ya existe un proveedor con ese teléfono.");

        proveedor.Nombre = nombre.Trim();
        proveedor.Telefono = telefonoNormalizado;
        proveedor.Email = NormalizarOpcional(email);
        proveedor.Direccion = NormalizarOpcional(direccion);

        await _db.SaveChangesAsync();

        return new Resultado(true, "Proveedor actualizado.");
    }

    public async Task<Resultado> ActivarAsync(int id)
    {
        var autorizacion = ValidarAdministrador();
        if (autorizacion is not null)
            return autorizacion;

        var proveedor = await _db.Proveedores.FirstOrDefaultAsync(p => p.Id == id);

        if (proveedor is null)
            return new Resultado(false, "El proveedor no existe.");

        proveedor.Activo = 1;
        await _db.SaveChangesAsync();

        return new Resultado(true, "Proveedor activado.");
    }

    public async Task<Resultado> DesactivarAsync(int id)
    {
        var autorizacion = ValidarAdministrador();
        if (autorizacion is not null)
            return autorizacion;

        var proveedor = await _db.Proveedores.FirstOrDefaultAsync(p => p.Id == id);

        if (proveedor is null)
            return new Resultado(false, "El proveedor no existe.");

        proveedor.Activo = 0;
        await _db.SaveChangesAsync();

        return new Resultado(true, "Proveedor desactivado.");
    }

    private Resultado? ValidarAdministrador()
        => _sesion.EsAdministrador
            ? null
            : new Resultado(false, "Solo un administrador puede modificar proveedores.");

    private static string? NormalizarOpcional(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static string? ValidarCampos(
        string? nombre, string? telefono, string? email, string? direccion)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return "El nombre es obligatorio.";

        var nombreValor = nombre.Trim();

        if (nombreValor.Length < MinNombre)
            return $"El nombre debe tener al menos {MinNombre} caracteres.";

        if (nombreValor.Length > MaxNombre)
            return $"El nombre no puede superar los {MaxNombre} caracteres.";

        if (string.IsNullOrWhiteSpace(telefono))
            return "El teléfono es obligatorio.";

        var telefonoValor = telefono.Trim();

        if (telefonoValor.Length < MinTelefono)
            return $"El teléfono debe tener al menos {MinTelefono} caracteres.";

        if (telefonoValor.Length > MaxTelefono)
            return $"El teléfono no puede superar los {MaxTelefono} caracteres.";

        var emailValor = email?.Trim();

        if (emailValor?.Length > MaxEmail)
            return $"El correo no puede superar los {MaxEmail} caracteres.";

        if (!string.IsNullOrWhiteSpace(emailValor) && !EsEmailValido(emailValor))
            return "El correo no tiene un formato válido.";

        if (direccion?.Trim().Length > MaxDireccion)
            return $"La dirección no puede superar los {MaxDireccion} caracteres.";

        return null;
    }

    private static bool EsEmailValido(string email)
        => MailAddress.TryCreate(email, out var direccion) &&
           string.Equals(direccion.Address, email, StringComparison.OrdinalIgnoreCase);
}
