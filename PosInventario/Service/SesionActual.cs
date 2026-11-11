using PosInventario.Models;
using PosInventario.Models.Enums;

namespace PosInventario.Service;

/// <summary>
/// Usuario con sesión iniciada en la aplicación. Se crea una única instancia
/// en <see cref="App"/> y se comparte con los servicios que validan permisos.
/// </summary>
public class SesionActual
{
    public Usuario? Usuario { get; private set; }

    public bool Iniciada => Usuario is not null;

    public bool EsAdministrador => Usuario?.Rol == RolUsuario.Administrador;

    public int IdUsuario => Usuario?.Id ?? 0;

    public void Iniciar(Usuario usuario) => Usuario = usuario;

    public void Cerrar() => Usuario = null;
}
