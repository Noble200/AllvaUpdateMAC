using System;
using System.IO;
using System.Threading.Tasks;

namespace Allva.Desktop.Services;

/// <summary>
/// Servicio para gestionar imagenes de documentos de clientes.
/// Las imagenes se guardan en carpeta local en lugar de BD.
/// Nomenclatura: cliente_{ID}_frontal.jpg, cliente_{ID}_trasera.jpg
/// </summary>
public static class DocumentImageService
{
    // Nombre de carpeta confuso a proposito para evitar acceso casual
    private const string FOLDER_NAME = "_x7k9sys";

    private static string _basePath = string.Empty;

    /// <summary>
    /// Obtiene o crea la ruta base para imagenes
    /// </summary>
    public static string GetBasePath()
    {
        if (string.IsNullOrEmpty(_basePath))
        {
            // Usar carpeta junto al ejecutable
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            _basePath = Path.Combine(appDir, FOLDER_NAME);

            // Crear carpeta si no existe
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);

                // Ocultar la carpeta en Windows
                if (OperatingSystem.IsWindows())
                {
                    try
                    {
                        var di = new DirectoryInfo(_basePath);
                        di.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
                    }
                    catch { /* Ignorar si no se puede ocultar */ }
                }
            }
        }

        return _basePath;
    }

    /// <summary>
    /// Genera la ruta para imagen frontal de un cliente
    /// </summary>
    public static string GetRutaImagenFrontal(int idCliente)
    {
        return Path.Combine(GetBasePath(), $"cliente_{idCliente}_frontal.jpg");
    }

    /// <summary>
    /// Genera la ruta para imagen trasera de un cliente
    /// </summary>
    public static string GetRutaImagenTrasera(int idCliente)
    {
        return Path.Combine(GetBasePath(), $"cliente_{idCliente}_trasera.jpg");
    }

    /// <summary>
    /// Guarda la imagen frontal del documento
    /// </summary>
    public static async Task GuardarImagenFrontalAsync(int idCliente, byte[] imagen)
    {
        var ruta = GetRutaImagenFrontal(idCliente);
        await File.WriteAllBytesAsync(ruta, imagen);
    }

    /// <summary>
    /// Guarda la imagen trasera del documento
    /// </summary>
    public static async Task GuardarImagenTraseraAsync(int idCliente, byte[] imagen)
    {
        var ruta = GetRutaImagenTrasera(idCliente);
        await File.WriteAllBytesAsync(ruta, imagen);
    }

    /// <summary>
    /// Carga la imagen frontal del documento
    /// </summary>
    public static async Task<byte[]?> CargarImagenFrontalAsync(int idCliente)
    {
        var ruta = GetRutaImagenFrontal(idCliente);
        if (!File.Exists(ruta)) return null;
        return await File.ReadAllBytesAsync(ruta);
    }

    /// <summary>
    /// Carga la imagen trasera del documento
    /// </summary>
    public static async Task<byte[]?> CargarImagenTraseraAsync(int idCliente)
    {
        var ruta = GetRutaImagenTrasera(idCliente);
        if (!File.Exists(ruta)) return null;
        return await File.ReadAllBytesAsync(ruta);
    }

    /// <summary>
    /// Verifica si existen ambas imagenes del cliente
    /// </summary>
    public static bool ExistenImagenes(int idCliente)
    {
        return File.Exists(GetRutaImagenFrontal(idCliente)) &&
               File.Exists(GetRutaImagenTrasera(idCliente));
    }

    /// <summary>
    /// Verifica si existe la imagen frontal
    /// </summary>
    public static bool ExisteImagenFrontal(int idCliente)
    {
        return File.Exists(GetRutaImagenFrontal(idCliente));
    }

    /// <summary>
    /// Verifica si existe la imagen trasera
    /// </summary>
    public static bool ExisteImagenTrasera(int idCliente)
    {
        return File.Exists(GetRutaImagenTrasera(idCliente));
    }

    /// <summary>
    /// Elimina las imagenes de un cliente
    /// </summary>
    public static void EliminarImagenes(int idCliente)
    {
        var rutaFrontal = GetRutaImagenFrontal(idCliente);
        var rutaTrasera = GetRutaImagenTrasera(idCliente);

        if (File.Exists(rutaFrontal)) File.Delete(rutaFrontal);
        if (File.Exists(rutaTrasera)) File.Delete(rutaTrasera);
    }

    /// <summary>
    /// Guarda imagen temporal para cliente nuevo (sin ID aun).
    /// Retorna el path temporal.
    /// </summary>
    public static async Task<string> GuardarImagenTemporalAsync(byte[] imagen, string tipo)
    {
        var tempPath = Path.Combine(GetBasePath(), $"temp_{tipo}_{Guid.NewGuid():N}.jpg");
        await File.WriteAllBytesAsync(tempPath, imagen);
        return tempPath;
    }

    /// <summary>
    /// Mueve las imagenes temporales a su ubicacion final con el ID del cliente
    /// </summary>
    public static void MoverTemporalesADefinitivo(string? tempFrontal, string? tempTrasera, int idCliente)
    {
        if (!string.IsNullOrEmpty(tempFrontal) && File.Exists(tempFrontal))
        {
            var destino = GetRutaImagenFrontal(idCliente);
            if (File.Exists(destino)) File.Delete(destino);
            File.Move(tempFrontal, destino);
        }

        if (!string.IsNullOrEmpty(tempTrasera) && File.Exists(tempTrasera))
        {
            var destino = GetRutaImagenTrasera(idCliente);
            if (File.Exists(destino)) File.Delete(destino);
            File.Move(tempTrasera, destino);
        }
    }

    /// <summary>
    /// Limpia archivos temporales antiguos (mas de 1 hora)
    /// </summary>
    public static void LimpiarTemporales()
    {
        try
        {
            var basePath = GetBasePath();
            var tempFiles = Directory.GetFiles(basePath, "temp_*.jpg");
            var limite = DateTime.Now.AddHours(-1);

            foreach (var file in tempFiles)
            {
                var info = new FileInfo(file);
                if (info.CreationTime < limite)
                {
                    File.Delete(file);
                }
            }
        }
        catch { /* Ignorar errores de limpieza */ }
    }
}
