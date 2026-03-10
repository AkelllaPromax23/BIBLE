using System;
using System.Linq;
using System.Windows;
using BIBLE.Data;
using BIBLE.Models;

namespace BIBLE
{
    public partial class GenreDialogWindow : Window
    {
        private LibraryContext _context;
        private Genre _currentGenre;
        private bool _isEditMode;

        public GenreDialogWindow(LibraryContext context)
        {
            InitializeComponent();
            _context = context;
            _currentGenre = new Genre();
            _isEditMode = false;
            TitleTextBlock.Text = "Добавление нового жанра";
        }

        public GenreDialogWindow(LibraryContext context, Genre genre)
        {
            InitializeComponent();
            _context = context;
            _currentGenre = genre;
            _isEditMode = true;
            TitleTextBlock.Text = "Редактирование жанра";

            NameTextBox.Text = genre.Name;
            DescriptionTextBox.Text = genre.Description;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(NameTextBox.Text))
                {
                    MessageBox.Show("Введите название жанра", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string newName = NameTextBox.Text.Trim();

                // ПРОВЕРКА НА УНИКАЛЬНОСТЬ (без учета регистра)
                var existingGenre = _context.Genres
                    .FirstOrDefault(g => g.Name.ToLower() == newName.ToLower()
                                      && g.Id != _currentGenre.Id);

                if (existingGenre != null)
                {
                    MessageBox.Show($"Жанр '{newName}' уже существует!\n\n" +
                                  "Пожалуйста, введите другое название.",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Заполняем жанр данными
                _currentGenre.Name = newName;
                _currentGenre.Description = DescriptionTextBox.Text?.Trim() ?? "";

                if (!_isEditMode)
                {
                    _context.Genres.Add(_currentGenre);
                }

                _context.SaveChanges();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
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