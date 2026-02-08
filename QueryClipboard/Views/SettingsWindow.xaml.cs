using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QueryClipboard.Models;
using QueryClipboard.Services;

namespace QueryClipboard.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsService _settingsService;
        private AppSettings _settings;

        // Hotkey capture state
        private ModifierKeys _capturedModifiers = ModifierKeys.None;
        private Key _capturedKey = Key.None;

        public SettingsWindow(SettingsService settingsService)
        {
            InitializeComponent();
            _settingsService = settingsService;
            _settings = _settingsService.GetSettings();

            LoadSettings();
        }

        private void LoadSettings()
        {
            // Modo de armazenamento
            if (_settings.StorageMode == StorageMode.Json)
            {
                JsonRadioButton.IsChecked = true;
            }
            else
            {
                SqlServerRadioButton.IsChecked = true;
                ConnectionStringTextBox.Text = _settings.ConnectionString ?? string.Empty;
            }

            // Hotkey display
            UpdateHotkeyDisplay();

            // Categorias
            LoadCategories();
        }

        private void UpdateHotkeyDisplay()
        {
            var display = _settings.HotkeyModifier.Replace("Control", "Ctrl") + "+" + _settings.HotkeyKey;
            HotkeyTextBlock.Text = display;
        }

        private void LoadCategories()
        {
            CategoriesPanel.Children.Clear();

            foreach (var category in _settings.Categories)
            {
                var categoryItem = CreateCategoryItem(category);
                CategoriesPanel.Children.Add(categoryItem);
            }
        }

        private Border CreateCategoryItem(Category category)
        {
            var catColor = (Color)ColorConverter.ConvertFromString(category.Color);

            var border = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 6)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Color dot
            var colorDot = new Border
            {
                Width = 12,
                Height = 12,
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(catColor),
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(colorDot, 0);
            grid.Children.Add(colorDot);

            var nameText = new TextBlock
            {
                Text = category.Name,
                FontSize = 13,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = (SolidColorBrush)FindResource("TextPrimaryBrush"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(nameText, 1);
            grid.Children.Add(nameText);

            var deleteButton = new Button
            {
                Content = "\u2716",
                Style = (Style)FindResource("IconButtonStyle"),
                Foreground = (SolidColorBrush)FindResource("DangerBrush"),
                Width = 28,
                Height = 28,
                Tag = category,
                ToolTip = "Excluir categoria"
            };
            deleteButton.Click += DeleteCategoryButton_Click;
            Grid.SetColumn(deleteButton, 2);
            grid.Children.Add(deleteButton);

            border.Child = grid;
            return border;
        }

        private void StorageMode_Changed(object sender, RoutedEventArgs e)
        {
            if (SqlServerRadioButton?.IsChecked == true)
            {
                ConnectionStringPanel.Visibility = Visibility.Visible;
            }
            else
            {
                ConnectionStringPanel.Visibility = Visibility.Collapsed;
            }
        }

        // ========== Hotkey Capture ==========

        private void ChangeHotkeyButton_Click(object sender, RoutedEventArgs e)
        {
            _capturedModifiers = ModifierKeys.None;
            _capturedKey = Key.None;
            HotkeyCaptureTextBox.Text = "Aguardando...";
            HotkeyCapturePanel.Visibility = Visibility.Visible;
            ChangeHotkeyButton.IsEnabled = false;
            HotkeyCaptureTextBox.Focus();
        }

        private void HotkeyCaptureTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            // Ignorar teclas que sao so modifier
            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin)
            {
                // Mostrar modifiers parciais
                var mods = Keyboard.Modifiers;
                if (mods != ModifierKeys.None)
                {
                    HotkeyCaptureTextBox.Text = FormatModifiers(mods) + "+...";
                }
                return;
            }

            var modifiers = Keyboard.Modifiers;

            // Requer pelo menos um modifier
            if (modifiers == ModifierKeys.None)
            {
                HotkeyCaptureTextBox.Text = "Use ao menos um modificador (Ctrl/Alt/Shift/Win)";
                return;
            }

            _capturedModifiers = modifiers;
            _capturedKey = key;

            HotkeyCaptureTextBox.Text = FormatModifiers(modifiers) + "+" + key.ToString();
        }

        private string FormatModifiers(ModifierKeys mods)
        {
            var parts = new System.Collections.Generic.List<string>();
            if ((mods & ModifierKeys.Control) == ModifierKeys.Control) parts.Add("Ctrl");
            if ((mods & ModifierKeys.Alt) == ModifierKeys.Alt) parts.Add("Alt");
            if ((mods & ModifierKeys.Shift) == ModifierKeys.Shift) parts.Add("Shift");
            if ((mods & ModifierKeys.Windows) == ModifierKeys.Windows) parts.Add("Win");
            return string.Join("+", parts);
        }

        private string ModifiersToSettingsString(ModifierKeys mods)
        {
            var parts = new System.Collections.Generic.List<string>();
            if ((mods & ModifierKeys.Control) == ModifierKeys.Control) parts.Add("Control");
            if ((mods & ModifierKeys.Alt) == ModifierKeys.Alt) parts.Add("Alt");
            if ((mods & ModifierKeys.Shift) == ModifierKeys.Shift) parts.Add("Shift");
            if ((mods & ModifierKeys.Windows) == ModifierKeys.Windows) parts.Add("Windows");
            return string.Join("+", parts);
        }

        private void ConfirmHotkeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (_capturedKey == Key.None || _capturedModifiers == ModifierKeys.None)
            {
                MessageBox.Show("Por favor, pressione uma combinacao de teclas valida.",
                    "Validacao", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _settings.HotkeyModifier = ModifiersToSettingsString(_capturedModifiers);
            _settings.HotkeyKey = _capturedKey.ToString();

            UpdateHotkeyDisplay();
            HotkeyCapturePanel.Visibility = Visibility.Collapsed;
            ChangeHotkeyButton.IsEnabled = true;
        }

        private void CancelHotkeyButton_Click(object sender, RoutedEventArgs e)
        {
            HotkeyCapturePanel.Visibility = Visibility.Collapsed;
            ChangeHotkeyButton.IsEnabled = true;
        }

        // ========== Categories ==========

        private void AddCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CategoryEditorDialog();
            if (dialog.ShowDialog() == true)
            {
                var newCategory = new Category
                {
                    Name = dialog.CategoryName,
                    Color = dialog.CategoryColor
                };

                _settings.Categories.Add(newCategory);
                LoadCategories();
            }
        }

        private void DeleteCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var category = (Category)button.Tag;

            var result = MessageBox.Show(
                $"Deseja realmente excluir a categoria '{category.Name}'?\n\nAs queries desta categoria nao serao excluidas.",
                "Confirmar exclusao",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _settings.Categories.Remove(category);
                LoadCategories();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (SqlServerRadioButton.IsChecked == true)
            {
                if (string.IsNullOrWhiteSpace(ConnectionStringTextBox.Text))
                {
                    MessageBox.Show("Por favor, informe a Connection String para o SQL Server.",
                        "Validacao", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _settings.StorageMode = StorageMode.SqlServer;
                _settings.ConnectionString = ConnectionStringTextBox.Text.Trim();
            }
            else
            {
                _settings.StorageMode = StorageMode.Json;
                _settings.ConnectionString = null;
            }

            try
            {
                _settingsService.SaveSettings(_settings);
                MessageBox.Show("Configuracoes salvas com sucesso!\n\nReinicie o aplicativo para aplicar as mudancas.",
                    "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar configuracoes: {ex.Message}",
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    // Dialog para adicionar categoria
    public class CategoryEditorDialog : Window
    {
        private TextBox _nameTextBox;
        public string CategoryName { get; private set; } = string.Empty;
        public string CategoryColor { get; private set; } = "#667EEA";

        public CategoryEditorDialog()
        {
            Title = "Nova Categoria";
            Width = 480;
            Height = 340;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            FontFamily = new FontFamily("Segoe UI");

            var bgBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8F9FA"));
            Background = bgBrush;

            var outerGrid = new Grid();
            outerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            outerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Mini header accent
            var accentBar = new Border { Height = 4 };
            var gradient = new LinearGradientBrush();
            gradient.StartPoint = new Point(0, 0);
            gradient.EndPoint = new Point(1, 0);
            gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#667EEA"), 0));
            gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#764BA2"), 1));
            accentBar.Background = gradient;
            Grid.SetRow(accentBar, 0);
            outerGrid.Children.Add(accentBar);

            var grid = new Grid { Margin = new Thickness(24, 18, 24, 18) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(14) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(grid, 1);
            outerGrid.Children.Add(grid);

            // Nome
            var nameLabel = new TextBlock
            {
                Text = "Nome da Categoria",
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6C757D"))
            };
            Grid.SetRow(nameLabel, 0);
            grid.Children.Add(nameLabel);

            _nameTextBox = new TextBox
            {
                Padding = new Thickness(10, 8, 10, 8),
                FontSize = 14,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                BorderThickness = new Thickness(1)
            };
            Grid.SetRow(_nameTextBox, 2);
            grid.Children.Add(_nameTextBox);

            // Cor
            var colorLabel = new TextBlock
            {
                Text = "Cor",
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6C757D"))
            };
            Grid.SetRow(colorLabel, 4);
            grid.Children.Add(colorLabel);

            var colorsPanel = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };
            Grid.SetRow(colorsPanel, 6);

            var colors = new[] {
                "#667EEA", "#F44336", "#4CAF50", "#FF9800", "#9C27B0",
                "#00BCD4", "#FFC107", "#E91E63", "#3F51B5", "#009688",
                "#FF5722", "#8BC34A", "#673AB7", "#607D8B", "#CDDC39"
            };

            foreach (var color in colors)
            {
                var colorButton = new Button
                {
                    Width = 32,
                    Height = 32,
                    Margin = new Thickness(3),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                    BorderThickness = new Thickness(3),
                    BorderBrush = new SolidColorBrush(Colors.Transparent),
                    Tag = color,
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                // Round template
                colorButton.Template = CreateRoundButtonTemplate();

                colorButton.Click += (s, e) =>
                {
                    CategoryColor = (string)((Button)s).Tag;
                    foreach (Button btn in colorsPanel.Children)
                    {
                        btn.BorderBrush = new SolidColorBrush(Colors.Transparent);
                    }
                    ((Button)s).BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A2E"));
                };

                if (color == colors[0])
                {
                    colorButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A2E"));
                }

                colorsPanel.Children.Add(colorButton);
            }
            grid.Children.Add(colorsPanel);

            // Botoes
            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetRow(buttonsPanel, 8);

            var cancelButton = new Button
            {
                Content = "Cancelar",
                Width = 90,
                Height = 34,
                Margin = new Thickness(0, 0, 8, 0),
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6C757D")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            cancelButton.Click += (s, e) => { DialogResult = false; Close(); };
            buttonsPanel.Children.Add(cancelButton);

            var okButton = new Button
            {
                Content = "Criar",
                Width = 90,
                Height = 34,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#667EEA")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            okButton.Click += OkButton_Click;
            buttonsPanel.Children.Add(okButton);

            grid.Children.Add(buttonsPanel);

            Content = outerGrid;
        }

        private ControlTemplate CreateRoundButtonTemplate()
        {
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            border.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            border.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(16));
            template.VisualTree = border;
            return template;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
            {
                MessageBox.Show("Por favor, informe o nome da categoria.", "Validacao",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CategoryName = _nameTextBox.Text.Trim();
            DialogResult = true;
            Close();
        }
    }
}
