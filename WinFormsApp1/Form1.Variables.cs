using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Clpx
{
    public partial class Form1 : Form
    {
        private readonly ListView listClipboard = new ListView();
        private readonly Panel infoPanel = new Panel();
        private readonly Label lblInfo = new Label();

        private readonly TextBox txtSearch = new TextBox();
        private readonly Button btnClearDb = new Button();

        private readonly StatusStrip statusStrip = new StatusStrip();
        private readonly ToolStripStatusLabel statusLabel = new ToolStripStatusLabel();

        private readonly Panel tabPanel = new Panel();
        private readonly Button btnTabAll = new Button();
        private readonly Button btnTabTxt = new Button();
        private readonly Button btnTabImg = new Button();

        private string currentTabFilter = "ALL";

        private readonly NotifyIcon trayIcon = new NotifyIcon();
        private readonly ContextMenuStrip trayMenu = new ContextMenuStrip();
        private bool allowActualClose = false;


        // ТОЧЕЧНАЯ ЗАМЕНА В Form1.Variables.cs: Возвращаем словарь картинок
        private Dictionary<string, Image> safeThumbnails = new Dictionary<string, Image>();


        private readonly Dictionary<string, string> customNames = new Dictionary<string, string>();

        // Глобальное потокобезопасное хранилище объектов
        private readonly List<ClipboardPayload> masterRegistry = new List<ClipboardPayload>();

        private string animatedKeyId = "";
        private string deletingKeyId = "";

        private string lastText = "";
        private int lastImgWidth = 0;
        private int lastImgHeight = 0;
        private ListViewItem hoveredItem = null;
        private int imageCounter = 0;

        
        private bool isInitializing = true;


        private readonly Label lblNoResults = new Label();


        private readonly object clipboardLock = new object();
        private Form previewForm = null;

        private readonly string dataPath = Path.Combine(Application.StartupPath, "clpx_history.json");
        private readonly string mediaFolderPath = Path.Combine(Application.StartupPath, "media");

        // ИМПОРТЫ MULTIMEDIA TIMER API ДЛЯ ВЫСОКОГЕРЦОВЫХ МОНИТОРОВ (144Hz+)
        [DllImport("winmm.dll", SetLastError = true, EntryPoint = "timeSetEvent")]
        private static extern uint TimeSetEvent(uint uDelay, uint uResolution, TimerCallback lpTimeProc, UIntPtr dwUser, uint fuEvent);

        [DllImport("winmm.dll", SetLastError = true, EntryPoint = "timeKillEvent")]
        private static extern uint TimeKillEvent(uint uTimerID);

        private delegate void TimerCallback(uint uID, uint uMsg, UIntPtr dwUser, UIntPtr dw1, UIntPtr dw2);
        private TimerCallback highHzCallback;
        private uint highHzTimerId = 0;
        private int animationTicksLeft = 0;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        private static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("user32.dll")]
        private static extern bool IsClipboardFormatAvailable(uint format);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalUnlock(IntPtr hMem);

        private const int HOTKEY_ID = 9000;
        private const int MOD_ALT = 0x0001;
        private const int WM_HOTKEY = 0x0312;
        private const int WM_WINDOWPOSCHANGING = 0x0046;

        private const int WM_DRAWCLIPBOARD = 0x0308;
        private const int WM_CHANGECBCHAIN = 0x030D;

        private const int WM_HELP = 0x0053;


        private const uint CF_BITMAP = 2;
        private const uint CF_UNICODETEXT = 13;
        private const int ITEM_ROW_HEIGHT = 72;

        // Новые константы для пасхалки
        private const int HOTKEY_AUTHOR_ID = 9001; // Уникальный ID для нового хоткея
        private const int KEY_A = 0x41;            // Шестнадцатеричный код клавиши "A"

        // --- КОНСТАНТЫ И ИМПОРТЫ ДЛЯ РЕЖИМА SINGLE INSTANCE ---
        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;
        public const int SW_RESTORE = 9;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern System.IntPtr GetForegroundWindow();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(System.IntPtr hWnd, System.IntPtr ProcessId);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ShowWindow(System.IntPtr hWnd, int nCmdShow);
       


        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();




        private IntPtr nextClipboardViewer;

        private void StopHighHzTimer()
        {
            if (highHzTimerId != 0)
            {
                TimeKillEvent(highHzTimerId);
                highHzTimerId = 0;
            }
        }

        private void SetAutorun()
        {
            try
            {
                using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (rk != null)
                    {
                        string appPath = Application.ExecutablePath;
                        rk.SetValue("ClpX", $"\"{appPath}\" /min");
                    }
                }
            }
            catch { }
        }

        private void EnableDoubleBuffer(Control control)
        {
            PropertyInfo property = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance);
            if (property != null)
            {
                property.SetValue(control, true, null);
            }
        }
        // --- КОНСТАНТЫ Win32 API ДЛЯ СУБПИКСЕЛЬНОЙ ПРОКРУТКИ 144HZ+ ---
        public const int WM_VSCROLL = 0x0115;
        public const int SB_LINEDOWN = 1;
        public const int SB_LINEUP = 0;
        public const int SB_THUMBPOSITION = 4;

        private List<string> searchHistory = new List<string>();

        private Panel pnlSearchHistory;

        private Button btnLangToggle;
        private string currentLanguage = "RU";
        private string historyTitleText = "ИСТОРИЯ ПОИСКА";
        private bool isChangingLanguage = false;


        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool GetScrollInfo(System.IntPtr hwnd, int fnBar, ref SCROLLINFO lpsi);

        public const int SB_VERT = 1;
        public const int SIF_ALL = 0x001F;

        // Системная структура Win32 для точного считывания координат ползунка
        public struct SCROLLINFO
        {
            public uint cbSize;
            public uint fMask;
            public int nMin;
            public int nMax;
            public uint nPage;
            public int nPos;
            public int nTrackPos;
        }

    }
}
