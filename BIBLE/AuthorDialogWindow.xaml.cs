using System;
using System.Linq;
using System.Windows;
using BIBLE.Data;
using BIBLE.Models;

namespace BIBLE
{
    public partial class AuthorDialogWindow : Window
    {
        private LibraryContext _context;
        private Author _currentAuthor;
        private bool _isEditMode;

        // Конструктор для добавления нового автора
        public AuthorDialogWindow(LibraryContext context)
        {
            InitializeComponent();
            _context = context;
            _currentAuthor = new Author();
            _isEditMode = false;
            TitleTextBlock.Text = "Добавление нового автора";
            BirthDatePicker.SelectedDate = DateTime.Now.AddYears(-30); // Значение по умолчанию
            CountryTextBox.TextChanged += CountryTextBox_TextChanged;
        }

        // Конструктор для редактирования существующего автора
        public AuthorDialogWindow(LibraryContext context, Author author)
        {
            InitializeComponent();
            _context = context;
            _currentAuthor = author;
            _isEditMode = true;
            TitleTextBlock.Text = "Редактирование автора";

            // Заполняем поля
            FirstNameTextBox.Text = author.FirstName;
            LastNameTextBox.Text = author.LastName;
            BirthDatePicker.SelectedDate = author.BirthDate;
            CountryTextBox.Text = author.Country;

            CountryTextBox.TextChanged += CountryTextBox_TextChanged;
        }

        // Автоматически делаем первую букву заглавной, остальные строчными
        private void CountryTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CountryTextBox.Text)) return;

            // Сохраняем позицию курсора
            int cursorPos = CountryTextBox.CaretIndex;

            // Получаем текущий текст
            string text = CountryTextBox.Text;

            // Форматируем: первая буква заглавная, остальные строчные
            if (text.Length > 0)
            {
                // Разбиваем на слова, каждое слово с большой буквы
                var words = text.Split(' ');
                for (int i = 0; i < words.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(words[i]))
                    {
                        words[i] = char.ToUpper(words[i][0]) +
                                  (words[i].Length > 1 ? words[i].Substring(1).ToLower() : "");
                    }
                }

                string formattedText = string.Join(" ", words);

                // Устанавливаем отформатированный текст
                if (formattedText != text)
                {
                    CountryTextBox.Text = formattedText;

                    // Восстанавливаем позицию курсора (приблизительно)
                    CountryTextBox.CaretIndex = Math.Min(cursorPos, formattedText.Length);
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // ========== ВАЛИДАЦИЯ ИМЕНИ ==========
                if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text))
                {
                    MessageBox.Show("Введите имя автора", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Очищаем от лишних пробелов и делаем первую букву заглавной
                string firstName = FirstNameTextBox.Text.Trim();
                firstName = char.ToUpper(firstName[0]) +
                           (firstName.Length > 1 ? firstName.Substring(1).ToLower() : "");

                // ========== ВАЛИДАЦИЯ ФАМИЛИИ ==========
                if (string.IsNullOrWhiteSpace(LastNameTextBox.Text))
                {
                    MessageBox.Show("Введите фамилию автора", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string lastName = LastNameTextBox.Text.Trim();
                lastName = char.ToUpper(lastName[0]) +
                          (lastName.Length > 1 ? lastName.Substring(1).ToLower() : "");

                // ========== ВАЛИДАЦИЯ ДАТЫ РОЖДЕНИЯ ==========
                if (BirthDatePicker.SelectedDate == null)
                {
                    MessageBox.Show("Выберите дату рождения автора", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime birthDate = BirthDatePicker.SelectedDate.Value;

                // Проверка, что дата рождения не в будущем
                if (birthDate > DateTime.Now)
                {
                    MessageBox.Show("Дата рождения не может быть в будущем", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверка на разумный возраст (не старше 120 лет)
                if (birthDate < DateTime.Now.AddYears(-120))
                {
                    MessageBox.Show("Возраст автора не может быть больше 120 лет", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ========== ВАЛИДАЦИЯ СТРАНЫ ==========
                if (string.IsNullOrWhiteSpace(CountryTextBox.Text))
                {
                    MessageBox.Show("Введите страну автора", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string country = CountryTextBox.Text.Trim();

                // Проверка, что страна не состоит только из цифр
                if (country.All(char.IsDigit))
                {
                    MessageBox.Show("Страна не может состоять только из цифр", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверка на минимальную длину
                if (country.Length < 2)
                {
                    MessageBox.Show("Название страны должно содержать хотя бы 2 символа", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ========== ПРОВЕРКА НА УНИКАЛЬНОСТЬ АВТОРА ==========
                var existingAuthor = _context.Authors
                    .FirstOrDefault(a => a.FirstName.ToLower() == firstName.ToLower()
                                      && a.LastName.ToLower() == lastName.ToLower()
                                      && a.Id != _currentAuthor.Id);

                if (existingAuthor != null)
                {
                    MessageBox.Show($"Автор {firstName} {lastName} уже существует в базе данных!",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ========== СОХРАНЕНИЕ ==========
                _currentAuthor.FirstName = firstName;
                _currentAuthor.LastName = lastName;
                _currentAuthor.BirthDate = birthDate;
                _currentAuthor.Country = country;

                if (!_isEditMode)
                {
                    _context.Authors.Add(_currentAuthor);
                }

                _context.SaveChanges();

                MessageBox.Show("Автор успешно сохранен!", "Успех",
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