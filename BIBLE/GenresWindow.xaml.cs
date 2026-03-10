using System;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using BIBLE.Data;
using BIBLE.Models;

namespace BIBLE
{
    public partial class GenresWindow : Window
    {
        private LibraryContext _context;

        public GenresWindow(LibraryContext context)
        {
            InitializeComponent();
            _context = context;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var genres = _context.Genres
                    .Include(g => g.BookGenres)
                    .ToList();
                GenresDataGrid.ItemsSource = genres;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new GenreDialogWindow(_context);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedGenre = GenresDataGrid.SelectedItem as Genre;
            if (selectedGenre == null)
            {
                MessageBox.Show("Выберите жанр для редактирования");
                return;
            }

            var dialog = new GenreDialogWindow(_context, selectedGenre);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedGenre = GenresDataGrid.SelectedItem as Genre;
            if (selectedGenre == null)
            {
                MessageBox.Show("Выберите жанр для удаления");
                return;
            }

            if (selectedGenre.BookGenres != null && selectedGenre.BookGenres.Any())
            {
                MessageBox.Show("Нельзя удалить жанр, к которому относятся книги");
                return;
            }

            var result = MessageBox.Show($"Удалить жанр '{selectedGenre.Name}'?",
                "Подтверждение", MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _context.Genres.Remove(selectedGenre);
                    _context.SaveChanges();
                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}");
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}