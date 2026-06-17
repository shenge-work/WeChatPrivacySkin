using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using MediaColor = System.Windows.Media.Color;
using MediaBrushes = System.Windows.Media.Brushes;
using WpfButton = System.Windows.Controls.Button;
using WpfHorizontalAlignment = System.Windows.HorizontalAlignment;

namespace WeChatPrivacySkin;

public partial class ManagementWindow : Window
{
    private readonly SettingsService _settingsService;
    private readonly OverlayManager _overlayManager;
    private readonly Action _exitApplication;
    private bool _loading;

    public ManagementWindow(
        SettingsService settingsService,
        OverlayManager overlayManager,
        Action exitApplication)
    {
        _settingsService = settingsService;
        _overlayManager = overlayManager;
        _exitApplication = exitApplication;

        _loading = true;
        InitializeComponent();
        BuildPrivacyModeButtons();
        BuildThemeCards();
        BuildSkinCards();
        NavigationList.SelectedIndex = 0;
        _loading = false;

        _settingsService.SettingsChanged += SettingsService_OnSettingsChanged;
        _overlayManager.SnapshotChanged += OverlayManager_OnSnapshotChanged;
        ApplySettings(_settingsService.Current);
        ApplySnapshot(_overlayManager.LastSnapshot);
    }

    private void BuildPrivacyModeButtons()
    {
        PrivacyModePanel.Children.Clear();
        foreach (var mode in PrivacyModeCatalog.OrderedModes)
        {
            var button = new WpfButton
            {
                Tag = mode,
                Width = 198,
                Height = 86,
                Margin = new Thickness(0, 0, 12, 12),
                HorizontalContentAlignment = WpfHorizontalAlignment.Left,
                VerticalContentAlignment = VerticalAlignment.Top,
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = PrivacyModeCatalog.DisplayName(mode),
                            FontWeight = FontWeights.SemiBold,
                            FontSize = 15
                        },
                        new TextBlock
                        {
                            Text = PrivacyModeCatalog.Description(mode),
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(0, 6, 0, 0),
                            Foreground = new SolidColorBrush(MediaColor.FromRgb(91, 111, 105))
                        }
                    }
                }
            };
            button.Click += (_, _) => SetPrivacyMode(mode);
            PrivacyModePanel.Children.Add(button);
        }
    }

    private void BuildThemeCards()
    {
        ThemePanel.Children.Clear();
        foreach (var theme in ThemeCatalog.All)
        {
            var preview = new Border
            {
                Height = 76,
                CornerRadius = new CornerRadius(theme.CornerRadius),
                Background = new LinearGradientBrush(
                    theme.BackgroundColor,
                    theme.OverlayColor,
                    32),
                BorderBrush = new SolidColorBrush(theme.AccentColor),
                BorderThickness = new Thickness(2),
                Child = new TextBlock
                {
                    Text = theme.BadgeText,
                    Foreground = new SolidColorBrush(theme.PrimaryTextColor),
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = WpfHorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            var button = new WpfButton
            {
                Tag = theme.Id,
                Width = 206,
                MinHeight = 176,
                Margin = new Thickness(0, 0, 12, 12),
                HorizontalContentAlignment = WpfHorizontalAlignment.Stretch,
                Content = new StackPanel
                {
                    Children =
                    {
                        preview,
                        new TextBlock
                        {
                            Text = theme.DisplayName,
                            FontWeight = FontWeights.SemiBold,
                            FontSize = 15,
                            Margin = new Thickness(0, 12, 0, 0)
                        },
                        new TextBlock
                        {
                            Text = $"{theme.Category} · {theme.Description}",
                            TextWrapping = TextWrapping.Wrap,
                            Foreground = new SolidColorBrush(MediaColor.FromRgb(91, 111, 105)),
                            Margin = new Thickness(0, 6, 0, 0)
                        }
                    }
                }
            };
            button.Click += (_, _) => _settingsService.Update(settings => settings.ThemePackId = theme.Id);
            ThemePanel.Children.Add(button);
        }
    }

    private void BuildSkinCards()
    {
        SkinPanel.Children.Clear();
        foreach (var skin in OverlaySkinCatalog.All)
        {
            var previewCanvas = new Canvas
            {
                Width = 174,
                Height = 76
            };
            OverlaySkinRenderer.DrawPreview(previewCanvas, skin, ThemeCatalog.Get(_settingsService.Current.ThemePackId), _settingsService.Current.CustomSkinImagePath);

            var preview = new Border
            {
                Height = 76,
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(MediaColor.FromRgb(247, 251, 249)),
                BorderBrush = new SolidColorBrush(skin.PreviewAccent),
                BorderThickness = new Thickness(1.4),
                Child = previewCanvas
            };

            var button = new WpfButton
            {
                Tag = skin.Id,
                Width = 206,
                MinHeight = 176,
                Margin = new Thickness(0, 0, 12, 12),
                HorizontalContentAlignment = WpfHorizontalAlignment.Stretch,
                Content = new StackPanel
                {
                    Children =
                    {
                        preview,
                        new TextBlock
                        {
                            Text = skin.DisplayName,
                            FontWeight = FontWeights.SemiBold,
                            FontSize = 15,
                            Margin = new Thickness(0, 12, 0, 0)
                        },
                        new TextBlock
                        {
                            Text = $"{skin.Category} · {skin.Description}",
                            TextWrapping = TextWrapping.Wrap,
                            Foreground = new SolidColorBrush(MediaColor.FromRgb(91, 111, 105)),
                            Margin = new Thickness(0, 6, 0, 0)
                        }
                    }
                }
            };
            button.Click += (_, _) => ApplySkin(skin);
            SkinPanel.Children.Add(button);
        }
    }

    private void ApplySettings(AppSettings settings)
    {
        _loading = true;
        try
        {
            PrivacyEnabledCheckBox.IsChecked = settings.Privacy.Enabled;
            TopmostCheckBox.IsChecked = settings.OverlayAlwaysOnTop;
            CoverPopupsCheckBox.IsChecked = settings.Privacy.CoverPopups;
            MeetingWarningCheckBox.IsChecked = settings.Privacy.ShowMeetingWarning;
            MotionCheckBox.IsChecked = settings.DecorativeMotionEnabled;
            OpacitySlider.Value = settings.OverlayOpacity;
            OpacityValueText.Text = $"{settings.OverlayOpacity:P0}";
            BackgroundPathText.Text = string.IsNullOrWhiteSpace(settings.BackgroundImagePath)
                ? "未选择自定义背景"
                : settings.BackgroundImagePath;
            SkinPathText.Text = string.IsNullOrWhiteSpace(settings.CustomSkinImagePath)
                ? "未选择透明 PNG 皮肤"
                : settings.CustomSkinImagePath;
            OverviewModeText.Text = PrivacyModeCatalog.DisplayName(settings.Privacy.Mode);
            OverviewThemeText.Text = $"{ThemeCatalog.Get(settings.ThemePackId).DisplayName} · {OverlaySkinCatalog.Get(settings.OverlaySkinId).DisplayName}";

            StatusPill.Background = settings.Privacy.Enabled
                ? new SolidColorBrush(MediaColor.FromRgb(221, 244, 235))
                : new SolidColorBrush(MediaColor.FromRgb(232, 232, 232));
            StatusPillText.Foreground = settings.Privacy.Enabled
                ? new SolidColorBrush(MediaColor.FromRgb(35, 122, 86))
                : new SolidColorBrush(MediaColor.FromRgb(88, 88, 88));
            StatusPillText.Text = settings.Privacy.Enabled ? "保护中" : "已关闭";

            RefreshPrivacyModeButtons(settings);
            RefreshThemeCards(settings);
            BuildSkinCards();
            RefreshSkinCards(settings);
            BuildRulesList(settings);
        }
        finally
        {
            _loading = false;
        }
    }

    private void ApplySnapshot(PrivacySnapshot? snapshot)
    {
        WindowCountText.Text = snapshot?.Windows.Count.ToString() ?? "0";
        ActiveWindowText.Text = snapshot?.ActiveWindow is null
            ? "当前没有前台微信窗口；检测到的微信窗口会按当前隐私模式保护。"
            : $"当前可见窗口：{snapshot.ActiveWindow.Title} · {snapshot.ActiveWindow.Kind}";
    }

    private void RefreshPrivacyModeButtons(AppSettings settings)
    {
        foreach (WpfButton button in PrivacyModePanel.Children.OfType<WpfButton>())
        {
            var selected = button.Tag is PrivacyMode mode && mode == settings.Privacy.Mode;
            button.Background = selected
                ? new SolidColorBrush(MediaColor.FromRgb(221, 244, 235))
                : MediaBrushes.White;
            button.BorderBrush = selected
                ? new SolidColorBrush(MediaColor.FromRgb(42, 125, 97))
                : new SolidColorBrush(MediaColor.FromRgb(220, 231, 227));
        }
    }

    private void RefreshThemeCards(AppSettings settings)
    {
        foreach (WpfButton button in ThemePanel.Children.OfType<WpfButton>())
        {
            var selected = string.Equals(button.Tag as string, settings.ThemePackId, StringComparison.OrdinalIgnoreCase);
            button.BorderBrush = selected
                ? new SolidColorBrush(MediaColor.FromRgb(42, 125, 97))
                : new SolidColorBrush(MediaColor.FromRgb(220, 231, 227));
            button.BorderThickness = selected ? new Thickness(2) : new Thickness(1);
        }
    }

    private void RefreshSkinCards(AppSettings settings)
    {
        foreach (WpfButton button in SkinPanel.Children.OfType<WpfButton>())
        {
            var selected = string.Equals(button.Tag as string, settings.OverlaySkinId, StringComparison.OrdinalIgnoreCase);
            button.BorderBrush = selected
                ? new SolidColorBrush(MediaColor.FromRgb(42, 125, 97))
                : new SolidColorBrush(MediaColor.FromRgb(220, 231, 227));
            button.BorderThickness = selected ? new Thickness(2) : new Thickness(1);
        }
    }

    private void ApplySkin(OverlaySkin skin)
    {
        if (skin.RenderKind == OverlaySkinRenderKind.CustomPng && !HasValidPng(_settingsService.Current.CustomSkinImagePath))
        {
            ChooseSkinPng();
            return;
        }

        _settingsService.Update(settings => settings.OverlaySkinId = skin.Id);
    }

    private void BuildRulesList(AppSettings settings)
    {
        RulesList.Items.Clear();
        foreach (var rule in settings.WindowRules)
        {
            RulesList.Items.Add(new Border
            {
                Background = MediaBrushes.White,
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(220, 231, 227)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 0, 0, 10),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = rule.DisplayName,
                            FontWeight = FontWeights.SemiBold,
                            Foreground = new SolidColorBrush(MediaColor.FromRgb(23, 35, 34))
                        },
                        new TextBlock
                        {
                            Text = $"{rule.Kind} · {(rule.CoverWhenNotForeground ? "非当前窗口脱敏" : "仅特殊模式脱敏")}",
                            Margin = new Thickness(0, 5, 0, 0),
                            Foreground = new SolidColorBrush(MediaColor.FromRgb(96, 115, 110))
                        }
                    }
                }
            });
        }
    }

    private void SetPrivacyMode(PrivacyMode mode)
    {
        _settingsService.Update(settings =>
        {
            settings.Privacy.Enabled = true;
            settings.Privacy.Mode = mode;
        });
    }

    private void NavigationList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var tag = (NavigationList.SelectedItem as ListBoxItem)?.Tag?.ToString() ?? "Overview";
        OverviewSection.Visibility = tag == "Overview" ? Visibility.Visible : Visibility.Collapsed;
        PrivacySection.Visibility = tag == "Privacy" ? Visibility.Visible : Visibility.Collapsed;
        ThemesSection.Visibility = tag == "Themes" ? Visibility.Visible : Visibility.Collapsed;
        HotkeysSection.Visibility = tag == "Hotkeys" ? Visibility.Visible : Visibility.Collapsed;
        RulesSection.Visibility = tag == "Rules" ? Visibility.Visible : Visibility.Collapsed;
        AboutSection.Visibility = tag == "About" ? Visibility.Visible : Visibility.Collapsed;

        PageTitleText.Text = tag switch
        {
            "Privacy" => "隐私场景",
            "Themes" => "主题皮肤",
            "Hotkeys" => "快捷键",
            "Rules" => "窗口规则",
            "About" => "关于",
            _ => "总览"
        };
        PageSubtitleText.Text = tag switch
        {
            "Privacy" => "选择日常、会议、离席、净屏和专注策略",
            "Themes" => "独立选择颜色主题、外形皮肤和透明 PNG",
            "Hotkeys" => "查看全局快捷键和托盘操作",
            "Rules" => "查看微信窗口识别和脱敏覆盖范围",
            "About" => "了解安全边界和实现方式",
            _ => "查看当前保护状态和快捷操作"
        };
    }

    private void PrivacyEnabled_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _settingsService.Update(settings => settings.Privacy.Enabled = PrivacyEnabledCheckBox.IsChecked == true);
    }

    private void Topmost_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _settingsService.Update(settings => settings.OverlayAlwaysOnTop = TopmostCheckBox.IsChecked == true);
    }

    private void PrivacyOption_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _settingsService.Update(settings =>
        {
            settings.Privacy.CoverPopups = CoverPopupsCheckBox.IsChecked == true;
            settings.Privacy.CoverUtilityWindows = CoverPopupsCheckBox.IsChecked == true;
            settings.Privacy.ShowMeetingWarning = MeetingWarningCheckBox.IsChecked == true;
        });
    }

    private void Motion_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _settingsService.Update(settings => settings.DecorativeMotionEnabled = MotionCheckBox.IsChecked == true);
    }

    private void OpacitySlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loading) return;
        var value = Math.Round(OpacitySlider.Value, 2);
        OpacityValueText.Text = $"{value:P0}";
        _settingsService.Update(settings => settings.OverlayOpacity = value);
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

    private void ChooseSkinPng_OnClick(object sender, RoutedEventArgs e)
    {
        ChooseSkinPng();
    }

    private void ChooseSkinPng()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择透明 PNG 皮肤",
            Filter = "PNG 图片|*.png",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == true)
        {
            _settingsService.Update(settings =>
            {
                settings.CustomSkinImagePath = dialog.FileName;
                settings.OverlaySkinId = OverlaySkinCatalog.CustomPngSkinId;
            });
            BuildSkinCards();
            RefreshSkinCards(_settingsService.Current);
        }
    }

    private void ClearSkinPng_OnClick(object sender, RoutedEventArgs e)
    {
        _settingsService.Update(settings =>
        {
            settings.CustomSkinImagePath = null;
            if (string.Equals(settings.OverlaySkinId, OverlaySkinCatalog.CustomPngSkinId, StringComparison.OrdinalIgnoreCase))
            {
                settings.OverlaySkinId = OverlaySkinCatalog.DefaultSkinId;
            }
        });
        BuildSkinCards();
        RefreshSkinCards(_settingsService.Current);
    }

    private static bool HasValidPng(string? path)
    {
        return !string.IsNullOrWhiteSpace(path) &&
               File.Exists(path) &&
               string.Equals(Path.GetExtension(path), ".png", StringComparison.OrdinalIgnoreCase);
    }

    private void DailyMode_OnClick(object sender, RoutedEventArgs e) => SetPrivacyMode(PrivacyMode.DailyProtection);

    private void MeetingMode_OnClick(object sender, RoutedEventArgs e) => SetPrivacyMode(PrivacyMode.MeetingShare);

    private void AwayMode_OnClick(object sender, RoutedEventArgs e) => SetPrivacyMode(PrivacyMode.AwayCover);

    private void CleanMode_OnClick(object sender, RoutedEventArgs e) => SetPrivacyMode(PrivacyMode.CleanScreen);

    private void FocusMode_OnClick(object sender, RoutedEventArgs e) => SetPrivacyMode(PrivacyMode.FocusChat);

    private void Exit_OnClick(object sender, RoutedEventArgs e)
    {
        _exitApplication();
    }

    private void SettingsService_OnSettingsChanged(object? sender, AppSettings settings)
    {
        Dispatcher.Invoke(() => ApplySettings(settings));
    }

    private void OverlayManager_OnSnapshotChanged(object? sender, PrivacySnapshot snapshot)
    {
        Dispatcher.Invoke(() => ApplySnapshot(snapshot));
    }

    protected override void OnClosed(EventArgs e)
    {
        _settingsService.SettingsChanged -= SettingsService_OnSettingsChanged;
        _overlayManager.SnapshotChanged -= OverlayManager_OnSnapshotChanged;
        base.OnClosed(e);
    }
}
