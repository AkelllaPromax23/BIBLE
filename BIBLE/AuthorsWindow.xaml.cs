using System;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using BIBLE.Data;
using BIBLE.Models;

namespace BIBLE
{
    public partial class AuthorsWindow : Window
    {
        private LibraryContext _context;

        public AuthorsWindow(LibraryContext context)
        {
            InitializeComponent();
            _context = context;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var authors = _context.Authors
                    .Include(a => a.BookAuthors)
                    .ToList();
                AuthorsDataGrid.ItemsSource = authors;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AuthorDialogWindow(_context);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedAuthor = AuthorsDataGrid.SelectedItem as Author;
            if (selectedAuthor == null)
            {
                MessageBox.Show("Выберите автора для редактирования");
                return;
            }

            var dialog = new AuthorDialogWindow(_context, selectedAuthor);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedAuthor = AuthorsDataGrid.SelectedItem as Author;
            if (selectedAuthor == null)
            {
                MessageBox.Show("Выберите автора для удаления");
                return;
            }

            if (selectedAuthor.BookAuthors != null && selectedAuthor.BookAuthors.Any())
            {
                MessageBox.Show("Нельзя удалить автора, у которого есть книги");
                return;
            }

            var result = MessageBox.Show($"Удалить автора {selectedAuthor.FirstName} {selectedAuthor.LastName}?",
                "Подтверждение", MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _context.Authors.Remove(selectedAuthor);
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