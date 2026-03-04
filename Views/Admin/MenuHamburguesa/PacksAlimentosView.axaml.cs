using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Allva.Desktop.ViewModels.Admin;

namespace Allva.Desktop.Views.Admin.MenuHamburguesa
{
    public partial class PacksAlimentosView : UserControl
    {
        private bool _isDragging = false;
        private bool _isResizing = false;
        private string _resizeHandle = string.Empty;
        private Point _lastPosition;

        public PacksAlimentosView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        // ============================================
        // EVENTOS DE CROP DEL POSTER
        // ============================================

        private void PosterCropArea_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Border border)
            {
                _isDragging = true;
                _isResizing = false;
                var canvas = this.FindControl<Canvas>("PosterCropCanvas");
                if (canvas != null)
                {
                    _lastPosition = e.GetPosition(canvas);
                }
                e.Pointer.Capture(border);
                e.Handled = true;
            }
        }

        private void PosterCropCanvas_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Canvas canvas)
            {
                var cropBorder = this.FindControl<Border>("PosterCropAreaBorder");
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

        private void PosterResizeHandle_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Border handle && handle.Tag is string corner)
            {
                _isResizing = true;
                _isDragging = false;
                _resizeHandle = corner;
                var canvas = this.FindControl<Canvas>("PosterCropCanvas");
                if (canvas != null)
                {
                    _lastPosition = e.GetPosition(canvas);
                }
                e.Pointer.Capture(handle);
                e.Handled = true;
            }
        }

        private void PosterCropArea_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (DataContext is PacksAlimentosViewModel vm)
            {
                var canvas = this.FindControl<Canvas>("PosterCropCanvas");
                if (canvas == null) return;

                var currentPosition = e.GetPosition(canvas);
                var deltaX = currentPosition.X - _lastPosition.X;
                var deltaY = currentPosition.Y - _lastPosition.Y;

                if (_isResizing && !string.IsNullOrEmpty(_resizeHandle))
                {
                    vm.RedimensionarCropPoster(_resizeHandle, deltaX, deltaY);
                }
                else if (_isDragging)
                {
                    vm.MoverCropPoster(deltaX, deltaY);
                }

                _lastPosition = currentPosition;
                e.Handled = true;
            }
        }

        private void PosterCropArea_PointerReleased(object? sender, PointerReleasedEventArgs e)
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

        private void PosterCropCanvas_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (DataContext is PacksAlimentosViewModel vm)
            {
                double zoomDelta = e.Delta.Y > 0 ? 0.1 : -0.1;
                vm.AjustarZoomPoster(zoomDelta);
                e.Handled = true;
            }
        }
    }
}
