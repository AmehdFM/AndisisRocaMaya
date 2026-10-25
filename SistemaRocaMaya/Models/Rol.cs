namespace SistemaRocaMaya.Models;

public class Rol
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public List<string> Permisos { get; set; } = [];

    public string PermisosTexto => string.Join(" · ", Permisos);
}
