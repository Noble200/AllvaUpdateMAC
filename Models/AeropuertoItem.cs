namespace Allva.Desktop.Models;

public class AeropuertoItem
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Ciudad { get; set; } = string.Empty;
    public string Pais { get; set; } = string.Empty;
    public string NombreAeropuerto { get; set; } = string.Empty;

    /// <summary>
    /// "España - Barcelona" para mostrar en dropdown
    /// </summary>
    public string TextoCompleto => $"{Pais} - {Ciudad}";

    /// <summary>
    /// "Barcelona (BCN)" para mostrar en resultados
    /// </summary>
    public string TextoConCodigo => $"{Ciudad} ({Codigo})";

    public override string ToString() => TextoCompleto;
}
