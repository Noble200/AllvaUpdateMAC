using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace Allva.Desktop.Services
{
    public class UpdateService
    {
        private readonly UpdateManager? _updateManager;
        private readonly bool _isUpdateAvailable;

        // ============================================
        // CONFIGURACION DE ACTUALIZACIONES
        // ============================================

        // Windows: Velopack via Railway
        private const string RAILWAY_UPDATE_URL = "https://allva-updates-server-production.up.railway.app";

        // macOS: GitHub Releases
        private const string GITHUB_REPO = "Noble200/AllvaUpdateMAC";
        private const string GITHUB_API_URL = "https://api.github.com/repos/Noble200/AllvaUpdateMAC/releases/latest";

        private static string GetUpdateUrl()
        {
            #if DEBUG
            return RAILWAY_UPDATE_URL;
            #else
            return RAILWAY_UPDATE_URL;
            #endif
        }

        public UpdateService()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    // Windows: usar Velopack con Railway
                    var updateUrl = GetUpdateUrl();

                    #if DEBUG
                    Console.WriteLine($"Sistema de actualizaciones (Velopack): {updateUrl}");
                    #endif

                    _updateManager = new UpdateManager(
                        new SimpleWebSource(updateUrl)
                    );

                    _isUpdateAvailable = true;
                }
                else
                {
                    // macOS: actualizaciones via GitHub Releases
                    #if DEBUG
                    Console.WriteLine($"Sistema de actualizaciones (GitHub): {GITHUB_REPO}");
                    #endif

                    _updateManager = null;
                    _isUpdateAvailable = true;
                }
            }
            catch (Exception ex)
            {
                #if DEBUG
                Console.WriteLine($"Error inicializando actualizaciones: {ex.Message}");
                #endif
                _updateManager = null;
                _isUpdateAvailable = false;
            }
        }

        public async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            if (!_isUpdateAvailable)
            {
                return null;
            }

            // Windows: Velopack
            if (OperatingSystem.IsWindows() && _updateManager != null)
            {
                return await CheckForUpdatesVelopackAsync();
            }

            // macOS: verificar via GitHub (no retorna UpdateInfo, solo informa)
            if (OperatingSystem.IsMacOS())
            {
                await CheckForUpdatesMacAsync();
            }

            return null;
        }

        private async Task<UpdateInfo?> CheckForUpdatesVelopackAsync()
        {
            try
            {
                var updateInfo = await _updateManager!.CheckForUpdatesAsync();

                #if DEBUG
                if (updateInfo != null)
                {
                    Console.WriteLine($"Actualizacion disponible: v{updateInfo.TargetFullRelease.Version}");
                }
                else
                {
                    Console.WriteLine($"Aplicacion actualizada (v{CurrentVersion})");
                }
                #endif

                return updateInfo;
            }
            catch (Exception ex)
            {
                #if DEBUG
                Console.WriteLine($"Error verificando actualizaciones: {ex.Message}");
                #endif
                return null;
            }
        }

        /// <summary>
        /// Verifica si hay una version mas nueva en GitHub Releases para macOS.
        /// En macOS la actualizacion se hace via terminal con el script instalar.sh
        /// </summary>
        private async Task CheckForUpdatesMacAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "AllvaSystem");

                var response = await client.GetStringAsync(GITHUB_API_URL);
                using var doc = JsonDocument.Parse(response);
                var root = doc.RootElement;

                if (root.TryGetProperty("tag_name", out var tagElement))
                {
                    var latestVersion = tagElement.GetString()?.TrimStart('v') ?? "";

                    #if DEBUG
                    Console.WriteLine($"Ultima version en GitHub: {latestVersion}");
                    Console.WriteLine($"Version actual: {CurrentVersion}");
                    #endif

                    // Comparar versiones
                    if (Version.TryParse(latestVersion, out var remote) &&
                        Version.TryParse(CurrentVersion, out var local) &&
                        remote > local)
                    {
                        MacUpdateAvailable = true;
                        MacLatestVersion = latestVersion;

                        #if DEBUG
                        Console.WriteLine($"Actualizacion disponible para Mac: v{latestVersion}");
                        Console.WriteLine($"Para actualizar ejecute en terminal:");
                        Console.WriteLine($"  curl -fsSL https://raw.githubusercontent.com/{GITHUB_REPO}/main/instalar.sh | bash");
                        #endif
                    }
                }
            }
            catch (Exception ex)
            {
                #if DEBUG
                Console.WriteLine($"Error verificando actualizaciones Mac: {ex.Message}");
                #endif
            }
        }

        public async Task DownloadUpdatesAsync(UpdateInfo updateInfo, Action<int>? progressCallback = null)
        {
            if (_updateManager == null || updateInfo == null) return;

            try
            {
                await _updateManager.DownloadUpdatesAsync(updateInfo, progressCallback);
            }
            catch (Exception ex)
            {
                #if DEBUG
                Console.WriteLine($"Error descargando actualizacion: {ex.Message}");
                #endif
                throw;
            }
        }

        public void ApplyUpdatesAndRestart(UpdateInfo updateInfo)
        {
            if (_updateManager == null || updateInfo == null) return;

            try
            {
                _updateManager.ApplyUpdatesAndRestart(updateInfo);
            }
            catch (Exception ex)
            {
                #if DEBUG
                Console.WriteLine($"Error aplicando actualizacion: {ex.Message}");
                #endif
                throw;
            }
        }

        public string CurrentVersion => _updateManager?.CurrentVersion?.ToString() ?? "1.4.4";

        public bool IsUpdateSystemAvailable => _isUpdateAvailable;

        public string UpdateUrl => OperatingSystem.IsWindows() ? GetUpdateUrl() : $"https://github.com/{GITHUB_REPO}/releases";

        /// <summary>
        /// Indica si hay una actualizacion disponible en GitHub (solo macOS)
        /// </summary>
        public bool MacUpdateAvailable { get; private set; }

        /// <summary>
        /// Version mas reciente disponible en GitHub (solo macOS)
        /// </summary>
        public string? MacLatestVersion { get; private set; }

        /// <summary>
        /// Comando para actualizar en macOS via terminal
        /// </summary>
        public static string MacUpdateCommand => $"curl -fsSL https://raw.githubusercontent.com/{GITHUB_REPO}/main/instalar.sh | bash";
    }
}
