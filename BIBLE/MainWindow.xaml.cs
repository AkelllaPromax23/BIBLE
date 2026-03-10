using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using BIBLE.Data;
using BIBLE.Models;

namespace BIBLE
{
    public partial class MainWindow : Window
    {
        private LibraryContext _context = new LibraryContext();

        public MainWindow()
        {
            InitializeComponent();
            LoadData();
            LoadFilters();
        }

        private void LoadData()
        {
            try
            {
                var books = _context.Books
                    .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                    .Include(b => b.BookGenres)
                    .ThenInclude(bg => bg.Genre)
                    .ToList();
                BooksDataGrid.ItemsSource = books;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void LoadFilters()
        {
            try
            {
                // Загрузка жанров
                var genres = _context.Genres.ToList();
                genres.Insert(0, new Genre { Id = 0, Name = "Все жанры" });
                GenreFilterComboBox.ItemsSource = genres;

                // Загрузка авторов
                var authors = _context.Authors
                    .Select(a => new {
                        a.Id,
                        FullName = a.LastName + " " + a.FirstName
                    })
                    .ToList();

                authors.Insert(0, new { Id = 0, FullName = "Все авторы" });
                AuthorFilterComboBox.ItemsSource = authors;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки фильтров: {ex.Message}");
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var query = _context.Books
                    .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                    .Include(b => b.BookGenres)
                    .ThenInclude(bg => bg.Genre)
                    .AsQueryable();

                // Фильтр по названию
                if (!string.IsNullOrWhiteSpace(SearchTextBox.Text) &&
                    SearchTextBox.Text != "Введите название книги...")
                {
                    query = query.Where(b => b.Title.Contains(SearchTextBox.Text));
                }

                // Фильтр по жанру
                if (GenreFilterComboBox.SelectedValue != null &&
                    (int)GenreFilterComboBox.SelectedValue != 0)
                {
                    int genreId = (int)GenreFilterComboBox.SelectedValue;
                    query = query.Where(b => b.BookGenres.Any(bg => bg.GenreId == genreId));
                }

                // Фильтр по автору
                if (AuthorFilterComboBox.SelectedValue != null &&
                    (int)AuthorFilterComboBox.SelectedValue != 0)
                {
                    int authorId = (int)AuthorFilterComboBox.SelectedValue;
                    query = query.Where(b => b.BookAuthors.Any(ba => ba.AuthorId == authorId));
                }

                BooksDataGrid.ItemsSource = query.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска: {ex.Message}");
            }
        }

        private void AddBookButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new BookWindow(_context);
            window.Owner = this;
            window.ShowDialog();
            LoadData();
        }

        private void EditBookButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedBook = BooksDataGrid.SelectedItem as Book;
            if (selectedBook == null)
            {
                MessageBox.Show("Выберите книгу для редактирования");
                return;
            }

            var window = new BookWindow(_context, selectedBook);
            window.Owner = this;
            window.ShowDialog();
            LoadData();
        }

        private void DeleteBookButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedBook = BooksDataGrid.SelectedItem as Book;
            if (selectedBook == null)
            {
                MessageBox.Show("Выберите книгу для удаления");
                return;
            }

            var result = MessageBox.Show($"Удалить книгу '{selectedBook.Title}'?",
                "Подтверждение", MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _context.Books.Remove(selectedBook);
                    _context.SaveChanges();
                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}");
                }
            }
        }

        private void ManageAuthorsButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new AuthorsWindow(_context);
            window.Owner = this;
            window.ShowDialog();
            LoadFilters();
            LoadData();
        }

        private void ManageGenresButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new GenresWindow(_context);
            window.Owner = this;
            window.ShowDialog();
            LoadFilters();
            LoadData();
        }
    }
}