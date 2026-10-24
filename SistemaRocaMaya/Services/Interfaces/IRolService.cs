using SistemaRocaMaya.Models;

namespace SistemaRocaMaya.Services.Interfaces;

public interface IRolService
{
    Task<IReadOnlyList<Rol>> ObtenerTodosAsync(
        CancellationToken cancellationToken = default);

    Task<Rol> CrearAsync(
        string nombre,
        IReadOnlyCollection<string> permisos,
        CancellationToken cancellationToken = default);

    Task ActualizarAsync(
        Rol rol,
        CancellationToken cancellationToken = default);
}
