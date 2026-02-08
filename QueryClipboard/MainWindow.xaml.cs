using Microsoft.Win32;
using Newtonsoft.Json;
using QueryClipboard.Models;
using QueryClipboard.Services;
using QueryClipboard.Views;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace QueryClipboard
{
    public partial class MainWindow : Window
    {
        private readonly IQueryRepository _repository;
        private readonly HotkeyManager _hotkeyManager;
        private readonly SettingsService _settingsService;
        private string? _selectedCategory;

        public MainWindow()
        {
            InitializeComponent();

            _settingsService = new SettingsService();
            var settings = _settingsService.GetSettings();

            if (settings.StorageMode == StorageMode.SqlServer && !string.IsNullOrEmpty(settings.ConnectionString))
            {
                try
                {
                    _repository = new SqlServerQueryRepository(settings.ConnectionString);
                }
                catch
                {
                    MessageBox.Show("Erro ao conectar ao SQL Server. Usando armazenamento JSON.",
                        "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _repository = new JsonQueryRepository();
                }
            }
            else
            {
                _repository = new JsonQueryRepository();
            }

            _hotkeyManager = new HotkeyManager();
            _hotkeyManager.HotkeyPressed += HotkeyManager_HotkeyPressed;

            LoadCategories();
            LoadQueries();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var windowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                var settings = _settingsService.GetSettings();

                var modifiers = HotkeyManager.ParseModifiers(settings.HotkeyModifier);
                var key = HotkeyManager.ParseKey(settings.HotkeyKey);

                _hotkeyManager.RegisterHotkey(windowHandle, modifiers, key);

                UpdateHotkeyHint();

                MessageBox.Show(
                    $"Query Clipboard iniciado com sucesso!\n\n" +
                    $"Pressione {settings.HotkeyModifier.Replace("+", "+")}+{settings.HotkeyKey} a qualquer momento para abrir!\n\n" +
                    "A janela vai aparecer agora para voce testar. Nas proximas vezes ela ficara escondida esperando o atalho.",
                    "Bem-vindo ao Query Clipboard!",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                ShowPopup();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao registrar atalho: {ex.Message}",
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateHotkeyHint()
        {
            var settings = _settingsService.GetSettings();
            var display = settings.HotkeyModifier.Replace("Control", "Ctrl") + "+" + settings.HotkeyKey;
            HotkeyHintText.Text = $"{display} para abrir";
        }

        private void HotkeyManager_HotkeyPressed(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (this.IsVisible)
                {
                    this.Hide();
                }
                else
                {
                    ShowPopup();
                }
            });
        }

        private void ShowPopup()
        {
            SearchTextBox.Clear();
            LoadQueries();
            this.Show();
            this.Activate();
            SearchTextBox.Focus();
        }

        private void LoadCategories()
        {
            CategoriesPanel.Children.Clear();

            var allButton = CreateCategoryButton("Todas", "#607D8B");
            allButton.Click += (s, e) => FilterByCategory(null);
            CategoriesPanel.Children.Add(allButton);

            var settings = _settingsService.GetSettings();
            foreach (var category in settings.Categories)
            {
                var button = CreateCategoryButton(category.Name, category.Color);
                button.Click += (s, e) => FilterByCategory(category.Name);
                CategoriesPanel.Children.Add(button);
            }
        }

        private Button CreateCategoryButton(string name, string color)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            var lightBrush = new SolidColorBrush(Color.FromArgb(30, brush.Color.R, brush.Color.G, brush.Color.B));

            var button = new Button
            {
                Content = name,
                Style = (Style)FindResource("CategoryPillStyle"),
                Background = lightBrush,
                Foreground = brush
            };
            return button;
        }

        private async void LoadQueries()
        {
            QueriesPanel.Children.Clear();

            var queries = string.IsNullOrEmpty(_selectedCategory)
                ? await _repository.GetAllQueriesAsync()
                : await _repository.GetQueriesByCategoryAsync(_selectedCategory);

            if (queries.Count == 0)
            {
                var emptyPanel = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 60, 0, 0)
                };
                emptyPanel.Children.Add(new TextBlock
                {
                    Text = "Nenhuma query encontrada",
                    FontSize = 16,
                    FontFamily = new FontFamily("Segoe UI"),
                    Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                emptyPanel.Children.Add(new TextBlock
                {
                    Text = "Crie uma nova query ou importe de um arquivo",
                    FontSize = 12,
                    FontFamily = new FontFamily("Segoe UI"),
                    Foreground = (SolidColorBrush)FindResource("MutedBrush"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 4, 0, 0)
                });
                QueriesPanel.Children.Add(emptyPanel);
                StatusTextBlock.Text = "0 queries";
                return;
            }

            foreach (var query in queries)
            {
                var card = CreateQueryCard(query);
                QueriesPanel.Children.Add(card);
            }

            StatusTextBlock.Text = $"{queries.Count} {(queries.Count == 1 ? "query" : "queries")}";
        }

        private Border CreateQueryCard(QueryItem query)
        {
            // Largura fixa: 2 cards por linha (~490px cada com margem, dentro de ~1000px util)
            var card = new Border
            {
                Style = (Style)FindResource("QueryCardStyle"),
                Tag = query,
                Width = 490
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var contentStack = new StackPanel();

            // Linha 1: nome + badge de categoria
            var headerStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };

            var nameText = new TextBlock
            {
                Text = query.Name,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = (SolidColorBrush)FindResource("TextPrimaryBrush"),
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 320
            };
            headerStack.Children.Add(nameText);

            var settings = _settingsService.GetSettings();
            var catColor = (Color)ColorConverter.ConvertFromString("#667EEA");
            var cat = settings.Categories.Find(c => c.Name == query.Category);
            if (cat != null)
            {
                catColor = (Color)ColorConverter.ConvertFromString(cat.Color);
            }

            var categoryBadge = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(30, catColor.R, catColor.G, catColor.B)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(7, 1, 7, 1),
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            categoryBadge.Child = new TextBlock
            {
                Text = query.Category,
                FontSize = 10,
                FontFamily = new FontFamily("Segoe UI Semibold"),
                Foreground = new SolidColorBrush(catColor)
            };
            headerStack.Children.Add(categoryBadge);
            contentStack.Children.Add(headerStack);

            // Linha 2: preview SQL compacto (1 linha)
            var previewText = query.SqlQuery.Replace("\r\n", " ").Replace("\n", " ");
            if (previewText.Length > 80) previewText = previewText.Substring(0, 80) + "...";

            var previewBorder = new Border
            {
                CornerRadius = new CornerRadius(5),
                Background = (SolidColorBrush)FindResource("CodeBgBrush"),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 0, 4)
            };
            previewBorder.Child = new TextBlock
            {
                Text = previewText,
                FontSize = 11,
                FontFamily = new FontFamily("Consolas"),
                Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextWrapping = TextWrapping.NoWrap
            };
            contentStack.Children.Add(previewBorder);

            // Linha 3: stats compactos
            contentStack.Children.Add(new TextBlock
            {
                Text = $"Usado {query.UsageCount}x \u2022 {GetRelativeTime(query.LastUsed)}",
                FontSize = 10,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = (SolidColorBrush)FindResource("MutedBrush")
            });

            Grid.SetColumn(contentStack, 0);
            grid.Children.Add(contentStack);

            // Botoes de acao (vertical, compactos)
            var actionsStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(6, 0, 0, 0)
            };

            var editButton = new Button
            {
                Content = "\u270F",
                Style = (Style)FindResource("IconButtonStyle"),
                Width = 28,
                Height = 28,
                ToolTip = "Editar"
            };
            editButton.Click += (s, e) =>
            {
                e.Handled = true;
                EditQuery(query);
            };
            actionsStack.Children.Add(editButton);

            var deleteButton = new Button
            {
                Content = "\u2716",
                Style = (Style)FindResource("IconButtonStyle"),
                Foreground = (SolidColorBrush)FindResource("DangerBrush"),
                Width = 28,
                Height = 28,
                Margin = new Thickness(0, 2, 0, 0),
                ToolTip = "Excluir"
            };
            deleteButton.Click += async (s, e) =>
            {
                e.Handled = true;
                var result = MessageBox.Show($"Deseja realmente excluir '{query.Name}'?",
                    "Confirmar exclusao", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    await _repository.DeleteQueryAsync(query.Id);
                    LoadQueries();
                }
            };
            actionsStack.Children.Add(deleteButton);

            Grid.SetColumn(actionsStack, 1);
            grid.Children.Add(actionsStack);

            card.Child = grid;

            // Clique no card = copia e fecha
            card.MouseLeftButtonUp += async (s, e) =>
            {
                if (e.Handled) return;
                await CopyQueryToClipboard(query);
            };

            return card;
        }

        private async System.Threading.Tasks.Task CopyQueryToClipboard(QueryItem query)
        {
            Clipboard.SetText(query.SqlQuery);
            await _repository.IncrementUsageAsync(query.Id);

            StatusTextBlock.Text = $"\u2713 '{query.Name}' copiada!";

            await System.Threading.Tasks.Task.Delay(500);
            this.Hide();
        }

        private string GetRelativeTime(DateTime date)
        {
            var timeSpan = DateTime.Now - date;

            if (timeSpan.TotalMinutes < 1)
                return "agora mesmo";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes}m atras";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours}h atras";
            if (timeSpan.TotalDays < 30)
                return $"{(int)timeSpan.TotalDays}d atras";

            return date.ToString("dd/MM/yyyy");
        }

        private void FilterByCategory(string? category)
        {
            _selectedCategory = category;
            LoadQueries();
        }

        private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchTerm = SearchTextBox.Text;

            QueriesPanel.Children.Clear();

            var queries = await _repository.SearchQueriesAsync(searchTerm);

            if (queries.Count == 0)
            {
                var emptyPanel = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 60, 0, 0)
                };
                emptyPanel.Children.Add(new TextBlock
                {
                    Text = "Nenhuma query encontrada",
                    FontSize = 16,
                    FontFamily = new FontFamily("Segoe UI"),
                    Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush"),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                QueriesPanel.Children.Add(emptyPanel);
                StatusTextBlock.Text = "0 queries";
                return;
            }

            foreach (var query in queries)
            {
                var card = CreateQueryCard(query);
                QueriesPanel.Children.Add(card);
            }

            StatusTextBlock.Text = $"{queries.Count} {(queries.Count == 1 ? "query" : "queries")}";
        }

        private void NewQueryButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new QueryEditorWindow(_repository);
            if (dialog.ShowDialog() == true)
            {
                LoadQueries();
            }
        }

        private void EditQuery(QueryItem query)
        {
            var dialog = new QueryEditorWindow(_repository, query);
            if (dialog.ShowDialog() == true)
            {
                LoadQueries();
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SettingsWindow(_settingsService);
            dialog.ShowDialog();

            LoadCategories();
            UpdateHotkeyHint();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            // Opcional: esconder quando perder foco
            // this.Hide();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Escape)
            {
                this.Hide();
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "JSON files (*.json)|*.json",
                    DefaultExt = ".json",
                    FileName = $"queries_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var queries = await _repository.GetAllQueriesAsync();
                    var json = JsonConvert.SerializeObject(queries, Formatting.Indented);
                    File.WriteAllText(saveDialog.FileName, json);

                    StatusTextBlock.Text = $"\u2713 {queries.Count} queries exportadas com sucesso!";
                    MessageBox.Show($"{queries.Count} queries exportadas com sucesso!\n\nArquivo: {Path.GetFileName(saveDialog.FileName)}",
                        "Exportacao Concluida", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao exportar queries: {ex.Message}",
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = "JSON files (*.json)|*.json",
                    DefaultExt = ".json"
                };

                if (openDialog.ShowDialog() == true)
                {
                    var json = File.ReadAllText(openDialog.FileName);
                    var queries = JsonConvert.DeserializeObject<System.Collections.Generic.List<QueryItem>>(json);

                    if (queries == null || queries.Count == 0)
                    {
                        MessageBox.Show("Nenhuma query encontrada no arquivo.",
                            "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var result = MessageBox.Show(
                        $"Encontradas {queries.Count} queries no arquivo.\n\n" +
                        "Clique em 'Sim' para ADICIONAR as queries existentes\n" +
                        "Clique em 'Nao' para SUBSTITUIR todas as queries",
                        "Modo de Importacao",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Cancel)
                        return;

                    if (result == MessageBoxResult.No)
                    {
                        var existingQueries = await _repository.GetAllQueriesAsync();
                        foreach (var existing in existingQueries)
                        {
                            await _repository.DeleteQueryAsync(existing.Id);
                        }
                    }

                    foreach (var query in queries)
                    {
                        query.Id = Guid.NewGuid();
                        await _repository.AddQueryAsync(query);
                    }

                    LoadQueries();
                    StatusTextBlock.Text = $"\u2713 {queries.Count} queries importadas com sucesso!";
                    MessageBox.Show($"{queries.Count} queries importadas com sucesso!",
                        "Importacao Concluida", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao importar queries: {ex.Message}",
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _hotkeyManager.UnregisterHotkey();
            base.OnClosed(e);
        }
    }
}
