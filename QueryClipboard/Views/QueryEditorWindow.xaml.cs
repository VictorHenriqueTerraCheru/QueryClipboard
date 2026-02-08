using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QueryClipboard.Models;
using QueryClipboard.Services;

namespace QueryClipboard.Views
{
    public partial class QueryEditorWindow : Window
    {
        private readonly IQueryRepository _repository;
        private readonly QueryItem? _existingQuery;

        public QueryEditorWindow(IQueryRepository repository, QueryItem? existingQuery = null)
        {
            InitializeComponent();
            _repository = repository;
            _existingQuery = existingQuery;

            LoadCategories();

            if (_existingQuery != null)
            {
                Title = "Editar Query";
                LoadQueryData();
            }
            else
            {
                Title = "Nova Query";
            }

            UpdateLineNumbers();
        }

        private async void LoadCategories()
        {
            var settingsService = new SettingsService();
            var settings = settingsService.GetSettings();

            foreach (var category in settings.Categories)
            {
                CategoryComboBox.Items.Add(category.Name);
            }

            if (CategoryComboBox.Items.Count > 0)
            {
                CategoryComboBox.SelectedIndex = 0;
            }
        }

        private void LoadQueryData()
        {
            if (_existingQuery == null) return;

            NameTextBox.Text = _existingQuery.Name;
            CategoryComboBox.Text = _existingQuery.Category;
            DescriptionTextBox.Text = _existingQuery.Description;
            QueryTextBox.Text = _existingQuery.SqlQuery;
        }

        private void UpdateLineNumbers()
        {
            var text = QueryTextBox.Text ?? "";
            var lineCount = text.Split('\n').Length;
            if (string.IsNullOrEmpty(text)) lineCount = 1;

            var numbers = string.Join("\n", Enumerable.Range(1, lineCount));
            LineNumbersBlock.Text = numbers;
            LineCountText.Text = $"{lineCount} {(lineCount == 1 ? "linha" : "linhas")}";
        }

        private void QueryTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateLineNumbers();
        }

        private void QueryTextBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            LineNumbersScroll.ScrollToVerticalOffset(e.VerticalOffset);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Por favor, informe o nome da query.", "Validacao",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(CategoryComboBox.Text))
            {
                MessageBox.Show("Por favor, informe a categoria.", "Validacao",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CategoryComboBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(QueryTextBox.Text))
            {
                MessageBox.Show("Por favor, informe a query SQL.", "Validacao",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                QueryTextBox.Focus();
                return;
            }

            try
            {
                if (_existingQuery != null)
                {
                    _existingQuery.Name = NameTextBox.Text.Trim();
                    _existingQuery.Category = CategoryComboBox.Text.Trim();
                    _existingQuery.Description = DescriptionTextBox.Text.Trim();
                    _existingQuery.SqlQuery = QueryTextBox.Text.Trim();

                    await _repository.UpdateQueryAsync(_existingQuery);
                    MessageBox.Show("Query atualizada com sucesso!", "Sucesso",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    var newQuery = new QueryItem
                    {
                        Name = NameTextBox.Text.Trim(),
                        Category = CategoryComboBox.Text.Trim(),
                        Description = DescriptionTextBox.Text.Trim(),
                        SqlQuery = QueryTextBox.Text.Trim()
                    };

                    await _repository.AddQueryAsync(newQuery);
                    MessageBox.Show("Query criada com sucesso!", "Sucesso",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar query: {ex.Message}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
