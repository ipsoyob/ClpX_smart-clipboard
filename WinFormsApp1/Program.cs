using Clpx;
using System;
using System.Threading;
using System.Windows.Forms;

static class Program
{
    // Глобальный Mutex для контроля уникальности процесса в ОС Windows
    private static Mutex mutex = new Mutex(true, "ClpX_Ultimate_v5_0_Mutex_{F0616085-66E3-4A94-93BF-99AA8D6838DA}");

    // Глобальное системное событие ядра для мгновенного пробуждения первой копии
    public static EventWaitHandle WakeUpEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "ClpX_Ultimate_v5_0_Event_{F0616085-66E3-4A94-93BF-99AA8D6838DA}");

    [STAThread]
    static void Main()
    {
        // Пытаемся монопольно захватить Mutex в операционной системе
        if (mutex.WaitOne(TimeSpan.Zero, true))
        {
            // --- ПЕРВЫЙ ЗАПУСК (ОСНОВНОЙ ПРОЦЕСС) ---
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run((System.Windows.Forms.Form)new Clpx.Form1());

            // Освобождаем ресурсы при штатном выходе из приложения
            mutex.ReleaseMutex();
            WakeUpEvent.Close();
        }
        else
        {
            // --- ПОВТОРНЫЙ ЗАПУСК (ДУБЛИКАТ) ---
            // Сигнализируем первому работающему в фоне процессу: "Проснись!"
            WakeUpEvent.Set();

            // Тихо, бесшовно и мгновенно завершаем работу дубликата, освобождая ОЗУ
            Environment.Exit(0);
        }
    }
}
