using System;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Windows.Forms;

public static class LanguageManager
{
    private static readonly ResourceManager _resManager =
        new ResourceManager("ClpX.Properties.Labels", typeof(LanguageManager).Assembly);


    public static void SetLanguage(string langCode)
    {
        // Создаем культуру (например, "ru" или "en")
        CultureInfo culture = new CultureInfo(langCode);

        // Говорим Windows, что теперь все потоки приложения используют эту культуру
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }

    public static void ApplyLocalization(Form form)
    {
        // 1. Переводим заголовок самой формы (если для него есть ключ в файле ресурсов)
        string formTitle = _resManager.GetString(form.Name);
        if (!string.IsNullOrEmpty(formTitle))
        {
            form.Text = formTitle;
        }

        // 2. Запускаем рекурсивный обход всех элементов управления (кнопок, лейблов и т.д.)
        TranslateControls(form.Controls);
    }

    public static string GetString(string key)
    {
        // Достает строку из таблицы по ключу, возвращает null если не нашел
        return _resManager.GetString(key);
    }

    private static void TranslateControls(Control.ControlCollection controls)
    {
        foreach (Control control in controls)
        {
            // Ищем в .resx файле строку, у которой Ключ совпадает с Именем элемента (Name)
            string translatedText = _resManager.GetString(control.Name);

            // Если нашли перевод — меняем текст элемента
            if (!string.IsNullOrEmpty(translatedText))
            {
                control.Text = translatedText;
            }

            if (control.HasChildren)
            {
                TranslateControls(control.Controls);
            }
        }
    }
}
