using System;
using System.Collections.ObjectModel;

namespace Allva.Desktop.Models;

/// <summary>
/// Modelo para una respuesta de una pregunta frecuente
/// </summary>
public class RespuestaPregunta
{
    public int Id { get; set; }
    public int IdPregunta { get; set; }
    public string Respuesta { get; set; } = "";
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
}

/// <summary>
/// Modelo para una pregunta frecuente con sus respuestas
/// </summary>
public class PreguntaFrecuente
{
    public int Id { get; set; }
    public string Pregunta { get; set; } = "";
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
    public ObservableCollection<RespuestaPregunta> Respuestas { get; set; } = new();

    // Propiedad para mostrar en UI si esta expandida
    public bool EstaExpandida { get; set; } = false;
}
