using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Allva.Desktop.ViewModels.Admin;

namespace Allva.Desktop.Views.Admin.MenuHamburguesa
{
    public partial class UltimasNoticiasAdminView : UserControl
    {
        private bool _isDragging = false;
        private bool _isResizing = false;
        private string _resizeHandle = string.Empty;
        private Point _lastPosition;

        public UltimasNoticiasAdminView()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                LogError("Error al inicializar UltimasNoticiasAdminView", ex);
                throw;
            }
        }

        private void InitializeComponent()
        {
            try
            {
                AvaloniaXamlLoader.Load(this);
            }
            catch (Exception ex)
            {
                LogError("Error al cargar XAML", ex);
                throw;
            }
        }

        private static void LogError(string context, Exception ex)
        {
            try
            {
                var logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "AllvaError.log");
                var message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}\n" +
                             $"Message: {ex.Message}\n" +
                             $"Inner: {ex.InnerException?.Message}\n" +
                             $"StackTrace: {ex.StackTrace}\n\n";
                System.IO.File.AppendAllText(logPath, message);
            }
            catch { }
        }

        private void CropArea_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Border border)
            {
                _isDragging = true;
                _isResizing = false;
                var canvas = this.FindControl<Canvas>("CropCanvas");
                if (canvas != null)
                {
                    _lastPosition = e.GetPosition(canvas);
                }
                e.Pointer.Capture(border);
                e.Handled = true;
            }
        }

        private void CropCanvas_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Canvas canvas)
            {
                // Si se hace clic en el canvas (no en el border), tambien iniciar arrastre
                var cropBorder = this.FindControl<Border>("CropAreaBorder");
                if (cropBorder != null)
                {
                    _isDragging = true;
                    _isResizing = false;
                    _lastPosition = e.GetPosition(canvas);
                    e.Pointer.Capture(canvas);
                    e.Handled = true;
                }
            }
        }

        private void ResizeHandle_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Border handle && handle.Tag is string corner)
            {
                _isResizing = true;
                _isDragging = false;
                _resizeHandle = corner;
                var canvas = this.FindControl<Canvas>("CropCanvas");
                if (canvas != null)
                {
                    _lastPosition = e.GetPosition(canvas);
                }
                e.Pointer.Capture(handle);
                e.Handled = true;
            }
        }

        private void CropArea_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (DataContext is UltimasNoticiasAdminViewModel vm)
            {
                var canvas = this.FindControl<Canvas>("CropCanvas");
                if (canvas == null) return;

                var currentPosition = e.GetPosition(canvas);
                var deltaX = currentPosition.X - _lastPosition.X;
                var deltaY = currentPosition.Y - _lastPosition.Y;

                if (_isResizing && !string.IsNullOrEmpty(_resizeHandle))
                {
                    vm.RedimensionarCropArea(_resizeHandle, deltaX, deltaY);
                }
                else if (_isDragging)
                {
                    vm.MoverCropArea(deltaX, deltaY);
                }

                _lastPosition = currentPosition;
                e.Handled = true;
            }
        }

        private void CropArea_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_isDragging || _isResizing)
            {
                _isDragging = false;
                _isResizing = false;
                _resizeHandle = string.Empty;
                e.Pointer.Capture(null);
                e.Handled = true;
            }
        }

        private void CropCanvas_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (DataContext is UltimasNoticiasAdminViewModel vm)
            {
                // Delta.Y > 0 = scroll up = zoom in, Delta.Y < 0 = scroll down = zoom out
                double zoomDelta = e.Delta.Y > 0 ? 0.1 : -0.1;
                vm.AjustarZoom(zoomDelta);
                e.Handled = true;
            }
        }

        private async void SubirNoticia_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is NoticiaItem noticia)
            {
                if (DataContext is UltimasNoticiasAdminViewModel vm)
                {
                    await vm.SubirNoticiaAsync(noticia);
                }
            }
        }

        private async void BajarNoticia_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is NoticiaItem noticia)
            {
                if (DataContext is UltimasNoticiasAdminViewModel vm)
                {
                    await vm.BajarNoticiaAsync(noticia);
                }
            }
        }

        private void SeleccionarIcono_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string nombreArchivo)
            {
                if (DataContext is UltimasNoticiasAdminViewModel vm)
                {
                    vm.IconoComunicacionSeleccionado = nombreArchivo;
                }
            }
        }

        private void SeleccionarColor_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string clave)
            {
                if (DataContext is UltimasNoticiasAdminViewModel vm)
                {
                    vm.ColorComunicacionSeleccionado = clave;
                }
            }
        }
    }
}
