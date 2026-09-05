using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Button = System.Windows.Controls.Button;
using Cursors = System.Windows.Input.Cursors;

namespace PortableHub.App.Views;

public partial class ColorPickerDialog : Window
{
    private double _currentH = 217.0; // 0 to 360
    private double _currentS = 0.85;  // 0 to 1
    private double _currentV = 0.96;  // 0 to 1
    private bool _isUpdating;
    private bool _isDraggingSpectrum;
    private bool _isDraggingHue;
    private readonly string _initialHex;
    public string SelectedHex { get; private set; }

    public static readonly string[] FluentPresetColors =
    [
        // Blues & Cyans
        "#0078D4", "#0284C7", "#06B6D4", "#0D9488", "#10B981", "#059669",
        // Purples & Pinks
        "#6366F1", "#8B5CF6", "#7C3AED", "#A855F7", "#D946EF", "#EC4899",
        // Warm & Reds
        "#F43F5E", "#EF4444", "#F97316", "#EA580C", "#F59E0B", "#D97706",
        // Greens & Neutrals
        "#84CC16", "#65A30D", "#14B8A6", "#64748B", "#475569", "#1E293B"
    ];

    public ColorPickerDialog(string initialHex = "#3B82F6")
    {
        _isUpdating = true;
        InitializeComponent();
        _isUpdating = false;

        _initialHex = NormalizeHex(initialHex, "#3B82F6");
        SelectedHex = _initialHex;

        OriginalHexText.Text = _initialHex;
        OriginalColorBorder.Background = HexToBrush(_initialHex);

        BuildPresetSwatches();

        Loaded += OnLoaded;
        SpectrumContainer.SizeChanged += (s, e) => UpdateSpectrumThumb();
        HueContainer.SizeChanged += (s, e) => UpdateHueThumb();

        ApplyColorFromHex(_initialHex, updateInputs: true);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateSpectrumThumb();
        UpdateHueThumb();
    }

    private void BuildPresetSwatches()
    {
        PresetSwatchesPanel.Children.Clear();
        foreach (var hex in FluentPresetColors)
        {
            var btn = new Button
            {
                Style = (Style)FindResource("ColorSwatchButtonStyle"),
                Background = HexToBrush(hex),
                ToolTip = hex,
                Tag = hex
            };

            btn.Click += (s, e) =>
            {
                if (s is Button b && b.Tag is string clickedHex)
                {
                    ApplyColorFromHex(clickedHex, updateInputs: true);
                }
            };

            PresetSwatchesPanel.Children.Add(btn);
        }
    }

    private void ApplyColorFromHex(string hex, bool updateInputs)
    {
        hex = NormalizeHex(hex, SelectedHex);
        var c = HexToColor(hex);
        var (h, s, v) = RgbToHsv(c);
        _currentH = h;
        _currentS = s;
        _currentV = v;

        UpdateSpectrumThumb();
        UpdateHueThumb();
        UpdateColor(updateInputs: updateInputs);
    }

    private void UpdateColor(bool updateInputs = true)
    {
        if (_isUpdating) return;
        _isUpdating = true;
        try
        {
            var c = HsvToRgb(_currentH, _currentS, _currentV);
            string hex = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
            SelectedHex = hex;

            CurrentColorBorder.Background = new SolidColorBrush(c);

            RValueText.Text = c.R.ToString();
            GValueText.Text = c.G.ToString();
            BValueText.Text = c.B.ToString();

            if (updateInputs && HexInputBox != null)
            {
                HexInputBox.Text = hex;
            }

            var hueColor = HsvToRgb(_currentH, 1.0, 1.0);
            var hueBrush = new SolidColorBrush(hueColor);
            SpectrumHueRect.Fill = hueBrush;
            HueThumbInner.Fill = hueBrush;
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void UpdateSpectrumThumb()
    {
        double width = SpectrumContainer.ActualWidth;
        double height = SpectrumContainer.ActualHeight;
        if (width <= 0 || height <= 0) return;

        double x = Math.Clamp(_currentS * width, 0.0, width);
        double y = Math.Clamp((1.0 - _currentV) * height, 0.0, height);

        Canvas.SetLeft(SpectrumThumb, x);
        Canvas.SetTop(SpectrumThumb, y);
    }

    private void UpdateHueThumb()
    {
        double width = HueContainer.ActualWidth;
        if (width <= 0) return;

        double x = Math.Clamp((_currentH / 360.0) * width, 0.0, width);

        Canvas.SetLeft(HueThumb, x);
        Canvas.SetTop(HueThumb, 1);
    }

    #region Spectrum Canvas Mouse & Keyboard
    private void Spectrum_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            SpectrumContainer.Focus();
            _isDraggingSpectrum = true;
            SpectrumContainer.CaptureMouse();
            UpdateSpectrumFromPoint(e.GetPosition(SpectrumContainer));
        }
    }

    private void Spectrum_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDraggingSpectrum && e.LeftButton == MouseButtonState.Pressed)
        {
            UpdateSpectrumFromPoint(e.GetPosition(SpectrumContainer));
        }
    }

    private void Spectrum_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDraggingSpectrum)
        {
            _isDraggingSpectrum = false;
            SpectrumContainer.ReleaseMouseCapture();
        }
    }

    private void UpdateSpectrumFromPoint(Point p)
    {
        double width = SpectrumContainer.ActualWidth;
        double height = SpectrumContainer.ActualHeight;
        if (width <= 0 || height <= 0) return;

        _currentS = Math.Clamp(p.X / width, 0.0, 1.0);
        _currentV = Math.Clamp(1.0 - (p.Y / height), 0.0, 1.0);

        UpdateSpectrumThumb();
        UpdateColor(updateInputs: true);
    }

    private void Spectrum_KeyDown(object sender, KeyEventArgs e)
    {
        double step = 0.04;
        switch (e.Key)
        {
            case Key.Left:
                _currentS = Math.Clamp(_currentS - step, 0.0, 1.0);
                break;
            case Key.Right:
                _currentS = Math.Clamp(_currentS + step, 0.0, 1.0);
                break;
            case Key.Up:
                _currentV = Math.Clamp(_currentV + step, 0.0, 1.0);
                break;
            case Key.Down:
                _currentV = Math.Clamp(_currentV - step, 0.0, 1.0);
                break;
            default:
                return;
        }
        UpdateSpectrumThumb();
        UpdateColor(updateInputs: true);
        e.Handled = true;
    }
    #endregion

    #region Hue Bar Mouse & Keyboard
    private void Hue_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            HueContainer.Focus();
            _isDraggingHue = true;
            HueContainer.CaptureMouse();
            UpdateHueFromPoint(e.GetPosition(HueContainer));
        }
    }

    private void Hue_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDraggingHue && e.LeftButton == MouseButtonState.Pressed)
        {
            UpdateHueFromPoint(e.GetPosition(HueContainer));
        }
    }

    private void Hue_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDraggingHue)
        {
            _isDraggingHue = false;
            HueContainer.ReleaseMouseCapture();
        }
    }

    private void UpdateHueFromPoint(Point p)
    {
        double width = HueContainer.ActualWidth;
        if (width <= 0) return;

        _currentH = Math.Clamp((p.X / width) * 360.0, 0.0, 359.9);

        UpdateHueThumb();
        UpdateColor(updateInputs: true);
    }

    private void Hue_KeyDown(object sender, KeyEventArgs e)
    {
        double step = 5.0;
        switch (e.Key)
        {
            case Key.Left:
                _currentH = (_currentH - step + 360.0) % 360.0;
                break;
            case Key.Right:
                _currentH = (_currentH + step) % 360.0;
                break;
            default:
                return;
        }
        UpdateHueThumb();
        UpdateColor(updateInputs: true);
        e.Handled = true;
    }
    #endregion

    private void HexInputBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating) return;
        string text = HexInputBox.Text.Trim();
        if (!text.StartsWith('#')) text = "#" + text;

        if (IsValidHex(text))
        {
            var c = HexToColor(text);
            var (h, s, v) = RgbToHsv(c);
            _currentH = h;
            _currentS = s;
            _currentV = v;

            UpdateSpectrumThumb();
            UpdateHueThumb();
            UpdateColor(updateInputs: false);
        }
    }

    private void OriginalColor_MouseDown(object sender, MouseButtonEventArgs e)
    {
        ApplyColorFromHex(_initialHex, updateInputs: true);
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    public static string? PickColor(string initialHex, Window? owner = null)
    {
        if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
        {
            return Application.Current.Dispatcher.Invoke(() => PickColor(initialHex, owner));
        }

        var dlg = new ColorPickerDialog(initialHex);
        try
        {
            var targetOwner = owner
                ?? Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive && w.IsVisible && w != dlg)
                ?? Application.Current?.MainWindow;

            if (targetOwner != null && targetOwner.IsLoaded && targetOwner != dlg)
            {
                dlg.Owner = targetOwner;
            }
            else
            {
                dlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }
        catch
        {
            dlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        bool? result = dlg.ShowDialog();
        return result == true ? dlg.SelectedHex : null;
    }

    #region Color Math Helpers
    private static bool IsValidHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return false;
        if (!hex.StartsWith('#') || hex.Length != 7) return false;
        return int.TryParse(hex.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _);
    }

    private static string NormalizeHex(string? hex, string fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;
        hex = hex.Trim();
        if (!hex.StartsWith('#')) hex = "#" + hex;
        return IsValidHex(hex) ? hex.ToUpperInvariant() : fallback;
    }

    private static Color HexToColor(string hex)
    {
        try
        {
            byte r = byte.Parse(hex.Substring(1, 2), NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(3, 2), NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(5, 2), NumberStyles.HexNumber);
            return Color.FromRgb(r, g, b);
        }
        catch
        {
            return Color.FromRgb(0x3B, 0x82, 0xF6);
        }
    }

    private static SolidColorBrush HexToBrush(string hex)
    {
        return new SolidColorBrush(HexToColor(hex));
    }

    private (double H, double S, double V) RgbToHsv(Color color)
    {
        double r = color.R / 255.0;
        double g = color.G / 255.0;
        double b = color.B / 255.0;

        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;

        double h = _currentH; // preserve current hue if grayscale (delta == 0)
        if (delta > 0.00001)
        {
            if (Math.Abs(max - r) < 0.00001)
                h = 60.0 * (((g - b) / delta) % 6.0);
            else if (Math.Abs(max - g) < 0.00001)
                h = 60.0 * (((b - r) / delta) + 2.0);
            else
                h = 60.0 * (((r - g) / delta) + 4.0);

            if (h < 0) h += 360.0;
        }

        double s = max > 0.00001 ? delta / max : 0.0;
        double v = max;

        return (h, s, v);
    }

    public static Color HsvToRgb(double h, double s, double v)
    {
        h = (h % 360.0 + 360.0) % 360.0;
        s = Math.Clamp(s, 0.0, 1.0);
        v = Math.Clamp(v, 0.0, 1.0);

        double c = v * s;
        double x = c * (1.0 - Math.Abs((h / 60.0) % 2.0 - 1.0));
        double m = v - c;

        double rPrime = 0, gPrime = 0, bPrime = 0;
        if (h < 60)
        {
            rPrime = c; gPrime = x; bPrime = 0;
        }
        else if (h < 120)
        {
            rPrime = x; gPrime = c; bPrime = 0;
        }
        else if (h < 180)
        {
            rPrime = 0; gPrime = c; bPrime = x;
        }
        else if (h < 240)
        {
            rPrime = 0; gPrime = x; bPrime = c;
        }
        else if (h < 300)
        {
            rPrime = x; gPrime = 0; bPrime = c;
        }
        else
        {
            rPrime = c; gPrime = 0; bPrime = x;
        }

        byte r = (byte)Math.Round((rPrime + m) * 255.0);
        byte g = (byte)Math.Round((gPrime + m) * 255.0);
        byte b = (byte)Math.Round((bPrime + m) * 255.0);

        return Color.FromRgb(r, g, b);
    }
    #endregion
}
