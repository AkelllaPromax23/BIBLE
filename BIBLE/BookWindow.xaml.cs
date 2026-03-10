using System;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using BIBLE.Data;
using BIBLE.Models;

namespace BIBLE
{
    public partial class BookWindow : Window
    {
        private LibraryContext _context;
        private Book _currentBook;
        private bool _isEditMode;
        private bool _isUpdatingText = false;

        // Конструктор для добавления новой книги
        public BookWindow(LibraryContext context)
        {
            InitializeComponent();
            _context = context;
            _currentBook = new Book();
            _isEditMode = false;
            TitleTextBlock.Text = "Добавление новой книги";
            LoadLists();
            ISBNTextBox.TextChanged += ISBNTextBox_TextChanged;
        }

        // Конструктор для редактирования существующей книги
        public BookWindow(LibraryContext context, Book book)
        {
            InitializeComponent();
            _context = context;
            _currentBook = book;
            _isEditMode = true;
            TitleTextBlock.Text = "Редактирование книги";
            LoadLists();
            ISBNTextBox.TextChanged += ISBNTextBox_TextChanged;

            // Заполняем поля данными книги
            TitleTextBox.Text = book.Title;
            YearTextBox.Text = book.PublishYear.ToString();
            ISBNTextBox.Text = book.ISBN;
            QuantityTextBox.Text = book.QuantityInStock.ToString();

            // Загружаем выбранных авторов и жанры
            LoadSelectedItems();
        }

        private void LoadLists()
        {
            try
            {
                // Загрузка всех авторов
                var authors = _context.Authors
                    .Select(a => new {
                        a.Id,
                        FullName = a.LastName + " " + a.FirstName
                    })
                    .ToList();
                AuthorListBox.ItemsSource = authors;

                // Загрузка всех жанров
                var genres = _context.Genres.ToList();
                GenreListBox.ItemsSource = genres;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void LoadSelectedItems()
        {
            try
            {
                if (_isEditMode && _currentBook.Id > 0)
                {
                    // Загружаем полную книгу со связями
                    var bookWithLinks = _context.Books
                        .Include(b => b.BookAuthors)
                        .ThenInclude(ba => ba.Author)
                        .Include(b => b.BookGenres)
                        .ThenInclude(bg => bg.Genre)
                        .FirstOrDefault(b => b.Id == _currentBook.Id);

                    if (bookWithLinks != null)
                    {
                        // Выделяем авторов в списке
                        foreach (var item in AuthorListBox.Items)
                        {
                            dynamic author = item;
                            if (bookWithLinks.BookAuthors.Any(ba => ba.AuthorId == author.Id))
                            {
                                AuthorListBox.SelectedItems.Add(item);
                            }
                        }

                        // Выделяем жанры в списке
                        foreach (var item in GenreListBox.Items)
                        {
                            var genre = item as Genre;
                            if (genre != null && bookWithLinks.BookGenres.Any(bg => bg.GenreId == genre.Id))
                            {
                                GenreListBox.SelectedItems.Add(item);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки выбранных элементов: {ex.Message}");
            }
        }

        // Форматирование ISBN
        private void ISBNTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_isUpdatingText) return;

            _isUpdatingText = true;

            int cursorPos = ISBNTextBox.CaretIndex;
            string currentText = ISBNTextBox.Text;

            string digitsOnly = new string(currentText.Where(char.IsDigit).ToArray());

            // Форматируем ISBN: добавляем дефисы после 3,4,9,11 цифр
            string formatted = "";

            for (int i = 0; i < digitsOnly.Length; i++)
            {
                if (i == 3 || i == 4 || i == 9 || i == 11)
                {
                    formatted += "-";
                }
                formatted += digitsOnly[i];
            }

            ISBNTextBox.Text = formatted;

            // Восстанавливаем позицию курсора
            int digitsBeforeCursor = currentText.Take(cursorPos).Count(c => char.IsDigit(c));

            int newCursorPos = 0;
            int digitCount = 0;

            for (int i = 0; i < formatted.Length && digitCount < digitsBeforeCursor; i++)
            {
                newCursorPos = i + 1;
                if (char.IsDigit(formatted[i]))
                    digitCount++;
            }

            ISBNTextBox.CaretIndex = Math.Min(newCursorPos, formatted.Length);

            _isUpdatingText = false;
        }

        // НОВЫЙ МЕТОД: Проверка года книги относительно дат рождения авторов
        private bool ValidateBookYearAgainstAuthors(int bookYear)
        {
            if (AuthorListBox.SelectedItems.Count == 0) return true;

            foreach (var selectedItem in AuthorListBox.SelectedItems)
            {
                dynamic author = selectedItem;
                // Получаем полного автора из базы с датой рождения
                var fullAuthor = _context.Authors.Find(author.Id);
                if (fullAuthor != null)
                {
                    int authorBirthYear = fullAuthor.BirthDate.Year;

                    if (authorBirthYear > bookYear)
                    {
                        MessageBox.Show($"Книга не может быть написана в {bookYear} году,\n" +
                                      $"так как автор {fullAuthor.FirstName} {fullAuthor.LastName} " +
                                      $"родился только в {authorBirthYear} году.",
                                      "Ошибка хронологии",
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }

                    // Проверка на слишком юный возраст (например, если автору меньше 10 лет)
                    if (bookYear - authorBirthYear < 10 && bookYear - authorBirthYear > 0)
                    {
                        var result = MessageBox.Show(
                            $"Автору {fullAuthor.FirstName} {fullAuthor.LastName} " +
                            $"было всего {bookYear - authorBirthYear} лет, когда вышла эта книга.\n" +
                            $"Это возможно только для очень юных авторов.\n\n" +
                            $"Продолжить сохранение?",
                            "Предупреждение",
                            MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (result == MessageBoxResult.No)
                            return false;
                    }
                }
            }
            return true;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // ========== ВАЛИДАЦИЯ НАЗВАНИЯ ==========
                if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
                {
                    MessageBox.Show("Введите название книги", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ========== ВАЛИДАЦИЯ АВТОРОВ ==========
                if (AuthorListBox.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Выберите хотя бы одного автора", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ========== ВАЛИДАЦИЯ ЖАНРОВ ==========
                if (GenreListBox.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Выберите хотя бы один жанр", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ========== ВАЛИДАЦИЯ ГОДА ==========
                if (!int.TryParse(YearTextBox.Text, out int year))
                {
                    MessageBox.Show("Введите корректный год (число)", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (year < 1800 || year > DateTime.Now.Year + 1)
                {
                    MessageBox.Show($"Введите корректный год (1800-{DateTime.Now.Year + 1})", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ========== НОВАЯ ПРОВЕРКА: ХРОНОЛОГИЯ АВТОРОВ ==========
                if (!ValidateBookYearAgainstAuthors(year))
                {
                    return; // Если проверка не пройдена, выходим
                }

                // ========== ВАЛИДАЦИЯ КОЛИЧЕСТВА ==========
                if (!int.TryParse(QuantityTextBox.Text, out int quantity) || quantity < 0)
                {
                    MessageBox.Show("Введите корректное количество (неотрицательное число)", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ========== ВАЛИДАЦИЯ ISBN ==========
                string isbn = ISBNTextBox.Text.Trim();
                string isbnDigits = new string(isbn.Where(char.IsDigit).ToArray());

                if (string.IsNullOrEmpty(isbnDigits))
                {
                    MessageBox.Show("Введите ISBN", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!isbnDigits.All(char.IsDigit))
                {
                    MessageBox.Show("ISBN может содержать только цифры", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (isbnDigits.Length != 10 && isbnDigits.Length != 13)
                {
                    MessageBox.Show($"ISBN должен содержать 10 или 13 цифр. Сейчас {isbnDigits.Length} цифр.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ========== СОХРАНЕНИЕ ==========
                _currentBook.Title = TitleTextBox.Text.Trim();
                _currentBook.PublishYear = year;
                _currentBook.ISBN = isbn;
                _currentBook.QuantityInStock = quantity;

                if (!_isEditMode)
                {
                    _context.Books.Add(_currentBook);
                    _context.SaveChanges(); // Чтобы получить Id для связей
                }

                // Обновляем связи с авторами
                var existingAuthors = _context.BookAuthors.Where(ba => ba.BookId == _currentBook.Id).ToList();
                _context.BookAuthors.RemoveRange(existingAuthors);

                foreach (var selectedItem in AuthorListBox.SelectedItems)
                {
                    dynamic author = selectedItem;
                    _context.BookAuthors.Add(new BookAuthor
                    {
                        BookId = _currentBook.Id,
                        AuthorId = author.Id
                    });
                }

                // Обновляем связи с жанрами
                var existingGenres = _context.BookGenres.Where(bg => bg.BookId == _currentBook.Id).ToList();
                _context.BookGenres.RemoveRange(existingGenres);

                foreach (var selectedItem in GenreListBox.SelectedItems)
                {
                    var genre = selectedItem as Genre;
                    if (genre != null)
                    {
                        _context.BookGenres.Add(new BookGenre
                        {
                            BookId = _currentBook.Id,
                            GenreId = genre.Id
                        });
                    }
                }

                _context.SaveChanges();

                MessageBox.Show("Книга успешно сохранена!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

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