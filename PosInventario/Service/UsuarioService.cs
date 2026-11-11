using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using PosInventario.Data;
using PosInventario.Models;
using PosInventario.Models.Enums;

namespace PosInventario.Service;

public class UsuarioService
{
    private const int MinNombreUsuario = 3;
    private const int MaxNombreUsuario = 50;
    private const int MinNombre = 3;
    private const int MaxNombre = 100;
    private const int MinContrasena = 8;
    private const int MaxContrasena = 128;

    private const int Iteraciones = 100_000;
    private const int TamanhoSalt = 16;
    private const int TamanhoHash = 32;

    private readonly AppDbContext _db;
    private readonly SesionActual _sesion;

    public UsuarioService(AppDbContext db, SesionActual sesion)
    {
        _db = db;
        _sesion = sesion;
    }

    public async Task<bool> ExisteAlgunUsuarioAsync()
        => await _db.Usuarios.AnyAsync();

    public async Task<List<Usuario>> ObtenerTodosAsync()
        => await _db.Usuarios
            .AsNoTracking()
            .OrderBy(u => u.NombreUsuario)
            .ToListAsync();

    public async Task<Usuario?> ObtenerPorIdAsync(int id)
        => await _db.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);

    /// <summary>
    /// Solo funciona cuando la base no tiene ningún usuario. Fuerza el rol
    /// Administrador y se usa desde el asistente de configuración inicial.
    /// </summary>
    public async Task<Resultado<Usuario>> CrearPrimerAdministradorAsync(
        string nombreUsuario, string nombre, string contrasena)
    {
        if (await _db.Usuarios.AnyAsync())
            return new Resultado<Usuario>(
                false, "Ya existe un usuario; el asistente solo crea el primer administrador.", null);

        var error = ValidarCampos(nombreUsuario, nombre, contrasena);
        if (error is not null)
            return new Resultado<Usuario>(false, error, null);

        var usuario = new Usuario
        {
            NombreUsuario = nombreUsuario.Trim(),
            Nombre = nombre.Trim(),
            ContrasenaHash = HashContrasena(contrasena),
            Rol = RolUsuario.Administrador,
            Activo = 1,
            FechaCreacion = DateTime.Now
        };

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        return new Resultado<Usuario>(true, "Administrador creado.", usuario);
    }

    public async Task<Resultado<Usuario>> AgregarAsync(
        string nombreUsuario, string nombre, string contrasena, RolUsuario rol)
    {
        var autorizacion = ValidarAdministrador();
        if (autorizacion is not null)
            return new Resultado<Usuario>(false, autorizacion.Mensaje, null);

        var error = ValidarCampos(nombreUsuario, nombre, contrasena);
        if (error is not null)
            return new Resultado<Usuario>(false, error, null);

        var nombreUsuarioNormalizado = nombreUsuario.Trim();

        if (await _db.Usuarios.AnyAsync(u => u.NombreUsuario == nombreUsuarioNormalizado))
            return new Resultado<Usuario>(
                false, "Ya existe un usuario con ese nombre de usuario.", null);

        var usuario = new Usuario
        {
            NombreUsuario = nombreUsuarioNormalizado,
            Nombre = nombre.Trim(),
            ContrasenaHash = HashContrasena(contrasena),
            Rol = rol,
            Activo = 1,
            FechaCreacion = DateTime.Now
        };

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        return new Resultado<Usuario>(true, "Usuario creado.", usuario);
    }

    public async Task<Resultado> ActualizarAsync(
        int id, string nombreUsuario, string nombre, RolUsuario rol)
    {
        var autorizacion = ValidarAdministrador();
        if (autorizacion is not null)
            return autorizacion;

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id);

        if (usuario is null)
            return new Resultado(false, "El usuario no existe.");

        var error = ValidarCampos(nombreUsuario, nombre, contrasena: null, incluirContrasena: false);
        if (error is not null)
            return new Resultado(false, error);

        var nombreUsuarioNormalizado = nombreUsuario.Trim();

        if (await _db.Usuarios.AnyAsync(u =>
                u.Id != id && u.NombreUsuario == nombreUsuarioNormalizado))
            return new Resultado(false, "Ya existe un usuario con ese nombre de usuario.");

        if (usuario.Rol == RolUsuario.Administrador &&
            rol != RolUsuario.Administrador &&
            !await ExisteOtroAdministradorActivoAsync(id))
            return new Resultado(false, "Debe quedar al menos un administrador activo.");

        usuario.NombreUsuario = nombreUsuarioNormalizado;
        usuario.Nombre = nombre.Trim();
        usuario.Rol = rol;

        await _db.SaveChangesAsync();

        return new Resultado(true, "Usuario actualizado.");
    }

    /// <summary>
    /// Restablece la contraseña de cualquier usuario. Requiere administrador.
    /// </summary>
    public async Task<Resultado> RestablecerContrasenaAsync(int id, string nuevaContrasena)
    {
        var autorizacion = ValidarAdministrador();
        if (autorizacion is not null)
            return autorizacion;

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id);

        if (usuario is null)
            return new Resultado(false, "El usuario no existe.");

        var error = ValidarContrasena(nuevaContrasena);
        if (error is not null)
            return new Resultado(false, error);

        usuario.ContrasenaHash = HashContrasena(nuevaContrasena);
        await _db.SaveChangesAsync();

        return new Resultado(true, "Contraseña restablecida.");
    }

    public async Task<Resultado> ActivarAsync(int id)
    {
        var autorizacion = ValidarAdministrador();
        if (autorizacion is not null)
            return autorizacion;

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id);

        if (usuario is null)
            return new Resultado(false, "El usuario no existe.");

        usuario.Activo = 1;
        await _db.SaveChangesAsync();

        return new Resultado(true, "Usuario activado.");
    }

    public async Task<Resultado> DesactivarAsync(int id)
    {
        var autorizacion = ValidarAdministrador();
        if (autorizacion is not null)
            return autorizacion;

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id);

        if (usuario is null)
            return new Resultado(false, "El usuario no existe.");

        if (usuario.Id == _sesion.IdUsuario)
            return new Resultado(false, "No puedes desactivar tu propio usuario.");

        if (usuario.Rol == RolUsuario.Administrador &&
            !await ExisteOtroAdministradorActivoAsync(id))
            return new Resultado(false, "Debe quedar al menos un administrador activo.");

        usuario.Activo = 0;
        await _db.SaveChangesAsync();

        return new Resultado(true, "Usuario desactivado.");
    }

    /// <summary>
    /// Verifica credenciales y devuelve el usuario activo correspondiente.
    /// No revela si el fallo vino del usuario o de la contraseña.
    /// </summary>
    public async Task<Resultado<Usuario>> ValidarCredencialesAsync(
        string nombreUsuario, string contrasena)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario) ||
            string.IsNullOrWhiteSpace(contrasena))
            return new Resultado<Usuario>(
                false, "Ingresa tu usuario y contraseña.", null);

        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u =>
                u.NombreUsuario == nombreUsuario.Trim() && u.Activo == 1);

        if (usuario is null || !VerificarContrasena(contrasena, usuario.ContrasenaHash))
            return new Resultado<Usuario>(
                false, "Usuario o contraseña incorrectos.", null);

        usuario.FechaUltimoAcceso = DateTime.Now;
        await _db.SaveChangesAsync();

        return new Resultado<Usuario>(true, "Acceso concedido.", usuario);
    }

    private Resultado? ValidarAdministrador()
        => _sesion.EsAdministrador
            ? null
            : new Resultado(false, "Solo un administrador puede realizar esta operación.");

    private async Task<bool> ExisteOtroAdministradorActivoAsync(int idExcluido)
        => await _db.Usuarios.AnyAsync(u =>
            u.Rol == RolUsuario.Administrador &&
            u.Activo == 1 &&
            u.Id != idExcluido);

    private static string? ValidarCampos(
        string? nombreUsuario, string? nombre, string? contrasena,
        bool incluirContrasena = true)
    {
        var error = ValidarNombreUsuario(nombreUsuario);
        if (error is not null) return error;

        error = ValidarNombre(nombre);
        if (error is not null) return error;

        if (incluirContrasena)
        {
            error = ValidarContrasena(contrasena);
            if (error is not null) return error;
        }

        return null;
    }

    private static string? ValidarNombreUsuario(string? nombreUsuario)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario))
            return "El nombre de usuario es obligatorio.";

        var valor = nombreUsuario.Trim();

        if (valor.Length < MinNombreUsuario)
            return $"El nombre de usuario debe tener al menos {MinNombreUsuario} caracteres.";

        if (valor.Length > MaxNombreUsuario)
            return $"El nombre de usuario no puede superar los {MaxNombreUsuario} caracteres.";

        if (valor.Any(char.IsWhiteSpace))
            return "El nombre de usuario no puede contener espacios.";

        return null;
    }

    private static string? ValidarNombre(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return "El nombre es obligatorio.";

        var valor = nombre.Trim();

        if (valor.Length < MinNombre)
            return $"El nombre debe tener al menos {MinNombre} caracteres.";

        if (valor.Length > MaxNombre)
            return $"El nombre no puede superar los {MaxNombre} caracteres.";

        return null;
    }

    private static string? ValidarContrasena(string? contrasena)
    {
        if (string.IsNullOrWhiteSpace(contrasena))
            return "La contraseña es obligatoria.";

        if (contrasena.Length < MinContrasena)
            return $"La contraseña debe tener al menos {MinContrasena} caracteres.";

        if (contrasena.Length > MaxContrasena)
            return $"La contraseña no puede superar los {MaxContrasena} caracteres.";

        return null;
    }

    /// <summary>
    /// PBKDF2-SHA256 con formato pbkdf2-sha256$iteraciones$salt$hash.
    /// Nunca almacenar la contraseña en texto plano.
    /// </summary>
    public static string HashContrasena(string contrasena)
    {
        var salt = RandomNumberGenerator.GetBytes(TamanhoSalt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            contrasena, salt, Iteraciones, HashAlgorithmName.SHA256, TamanhoHash);

        return $"pbkdf2-sha256${Iteraciones}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    private static bool VerificarContrasena(string contrasena, string almacenado)
    {
        var partes = almacenado.Split('$');

        if (partes.Length != 4 || partes[0] != "pbkdf2-sha256")
            return false;

        if (!int.TryParse(partes[1], out var iteraciones) || iteraciones <= 0)
            return false;

        byte[] salt;
        byte[] hash;

        try
        {
            salt = Convert.FromBase64String(partes[2]);
            hash = Convert.FromBase64String(partes[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var calculado = Rfc2898DeriveBytes.Pbkdf2(
            contrasena, salt, iteraciones, HashAlgorithmName.SHA256, hash.Length);

        return CryptographicOperations.FixedTimeEquals(calculado, hash);
    }
}
