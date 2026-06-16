using System.Diagnostics;
using System.Windows;
using Microsoft.Win32;

namespace WeChatPrivacySkin;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settingsService;
    private bool _loading;

    public SettingsWindow(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _loading = true;
        InitializeComponent();
        LoadThemeItems();
        _loading = false;
        ApplySettings(_settingsService.Current);
        _settingsService.SettingsChanged += SettingsService_OnSettingsChanged;
    }

    private void LoadThemeItems()
    {
        ThemeComboBox.Items.Clear();
        foreach (var theme in ThemeCatalog.All)
        {
            ThemeComboBox.Items.Add(new ThemeOption(theme, ThemeCatalog.DisplayName(theme)));
        }
        ThemeComboBox.DisplayMemberPath = nameof(ThemeOption.DisplayName);
    }

    private void SettingsService_OnSettingsChanged(object? sender, AppSettings settings)
    {
        Dispatcher.Invoke(() => ApplySettings(settings));
    }

    private void ApplySettings(AppSettings settings)
    {
        _loading = true;
        try
        {
            PrivacyCheckBox.IsChecked = settings.PrivacyEnabled;
            TopmostCheckBox.IsChecked = settings.OverlayAlwaysOnTop;
            OpacitySlider.Value = settings.OverlayOpacity;
            OpacityValueText.Text = $"{settings.OverlayOpacity:P0}";
            BackgroundPathText.Text = string.IsNullOrWhiteSpace(settings.BackgroundImagePath)
                ? "未选择"
                : settings.BackgroundImagePath;

            foreach (var item in ThemeComboBox.Items.OfType<ThemeOption>())
            {
                if (item.Theme == settings.Theme)
                {
                    ThemeComboBox.SelectedItem = item;
                    break;
                }
            }
        }
        finally
        {
            _loading = false;
        }
    }

    private void PrivacyCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        _settingsService.Update(settings => settings.PrivacyEnabled = PrivacyCheckBox.IsChecked == true);
    }

    private void ThemeComboBox_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_loading || ThemeComboBox.SelectedItem is not ThemeOption selected)
        {
            return;
        }

        _settingsService.Update(settings => settings.Theme = selected.Theme);
    }

    private void OpacitySlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loading)
        {
            return;
        }

        var value = Math.Round(OpacitySlider.Value, 2);
        OpacityValueText.Text = $"{value:P0}";
        _settingsService.Update(settings => settings.OverlayOpacity = value);
    }

    private void TopmostCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        _settingsService.Update(settings => settings.OverlayAlwaysOnTop = TopmostCheckBox.IsChecked == true);
    }

    private void ChooseBackground_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择遮罩背景图片",
            Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.webp|所有文件|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == true)
        {
            _settingsService.Update(settings => settings.BackgroundImagePath = dialog.FileName);
        }
    }

    private void ClearBackground_OnClick(object sender, RoutedEventArgs e)
    {
        _settingsService.Update(settings => settings.BackgroundImagePath = null);
    }

    private void OpenSettingsFolder_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = _settingsService.SettingsDirectory,
            UseShellExecute = true
        });
    }

    private void Close_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _settingsService.SettingsChanged -= SettingsService_OnSettingsChanged;
        base.OnClosed(e);
    }

    private sealed record ThemeOption(SkinTheme Theme, string DisplayName);
}
