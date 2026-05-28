using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;

namespace Clpx
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            if (!Directory.Exists(mediaFolderPath))
            {
                Directory.CreateDirectory(mediaFolderPath);
            }

            SetAutorun();
            BuildMyInterface();
            EnableDoubleBuffer(listClipboard);

            highHzCallback = new TimerCallback((id, msg, user, dw1, dw2) =>
            {
                animationTicksLeft--;
                if (animationTicksLeft <= 0)
                {
                    StopHighHzTimer();
                    animatedKeyId = "";

                    if (listClipboard.IsHandleCreated)
                    {
                        listClipboard.BeginInvoke(new Action(() =>
                        {
                            statusLabel.Text = " Ready";
                            statusLabel.ForeColor = Color.FromArgb(160, 165, 185);
                            listClipboard.Invalidate();
                        }));
                    }
                }
                else
                {
                    if (listClipboard.IsHandleCreated)
                    {
                        listClipboard.BeginInvoke(new Action(() => listClipboard.Invalidate()));
                    }
                }
                if (lblNoResults != null && listClipboard != null)
                {
                    lblNoResults.Location = new Point(
                        listClipboard.Left + (listClipboard.Width - lblNoResults.Width) / 2,
                        listClipboard.Top + (listClipboard.Height - lblNoResults.Height) / 3
                    );
                }

            });

            nextClipboardViewer = SetClipboardViewer(this.Handle);

            string[] args = Environment.GetCommandLineArgs();
            bool shouldMinimize = false;
            foreach (string arg in args)
            {
                if (arg.ToLower() == "/min")
                {
                    shouldMinimize = true;
                    break;
                }
            }

            if (shouldMinimize)
            {
                this.WindowState = FormWindowState.Minimized;
                this.Load += (s, e) => { if (s != null) this.Hide(); };
            }
        }


        private void UpdateListViewLayout()
        {
            if (lblNoResults != null && listClipboard != null)
            {
                lblNoResults.Location = new Point(
                    listClipboard.Left + (listClipboard.Width - lblNoResults.Width) / 2,
                    listClipboard.Top + (listClipboard.Height - lblNoResults.Height) / 3
                );
            }
            if (infoPanel.ClientSize.Width < 100) return;

            if (listClipboard.Columns.Count > 0)
            {
                int targetWidth = listClipboard.ClientSize.Width - 4;
                if (targetWidth < 200) targetWidth = 200;

                if (listClipboard.Columns[0].Width != targetWidth)
                {
                    listClipboard.Columns[0].Width = targetWidth;
                }
            }

            int pnlWidth = infoPanel.ClientSize.Width;

            lblInfo.Location = new Point(16, 12);
            lblInfo.Width = pnlWidth - 32;
            lblInfo.Height = 75;

            tabPanel.Location = new Point(16, 95);
            tabPanel.Width = pnlWidth - 32;
            tabPanel.Height = 32;

            int buttonWidth = 180;
            int searchWidth = pnlWidth - 32 - buttonWidth - 12;
            if (searchWidth < 100) searchWidth = 100;

            int targetHeight = 30;
            int targetY = 172;

            txtSearch.Location = new Point(16, targetY);
            txtSearch.Width = searchWidth;
            txtSearch.Height = targetHeight;

            btnClearDb.Location = new Point(txtSearch.Right + 12, targetY);
            btnClearDb.Width = buttonWidth;
            btnClearDb.Height = targetHeight;
        }

        private void ApplyFilters()
        {
            


            string query = txtSearch.Text.Trim().ToLower();
            if (query == "🔍 начните писать здесь для поиска...") query = "";

            List<ListViewItem> filtered = new List<ListViewItem>();

            lock (clipboardLock)
            {
                foreach (var item in masterRegistry)
                {
                    if (currentTabFilter == "TXT" && item.Type != "TXT") continue;
                    if (currentTabFilter == "IMG" && item.Type != "IMG") continue;

                    if (!string.IsNullOrEmpty(query))
                    {
                        string textToSearch = item.Body.ToLower();
                        if (!string.IsNullOrEmpty(item.Alias))
                        {
                            textToSearch += " " + item.Alias.ToLower();
                        }
                        if (!string.IsNullOrEmpty(item.Meta))
                        {
                            textToSearch += " " + item.Meta.ToLower();
                        }

                        if (!textToSearch.Contains(query)) continue;
                    }

                    // РОДНОЙ ИСПРАВЛЕННЫЙ МЕТОД: Привязка visual-карточки строго к её родному ID из JSON
                    ListViewItem lvi = new ListViewItem(item.Body)
                    {
                        Name = item.Type,
                        Tag = item.Meta,
                        ImageKey = item.Id
                    };
                    filtered.Add(lvi);
                }
            }

            if (listClipboard.IsHandleCreated)
            {
                listClipboard.BeginUpdate();
                listClipboard.Items.Clear();
                foreach (var item in filtered)
                {
                    listClipboard.Items.Add(item);
                }
                listClipboard.EndUpdate();
            }
           
            if (listClipboard.Items.Count == 0)
            {
                lblNoResults.Visible = true;
            }
            else
            {
                lblNoResults.Visible = false;
            }
        }

        private void ClearFullDatabase()
        {
            var result = MessageBox.Show("Вы уверены, что хотите полностью очистить историю, удалить сохраненные скриншоты и очистить системный буфер?",
                "Очистка базы данных", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

            if (result == DialogResult.Yes)
            {
                lock (clipboardLock)
                {
                    try
                    {
                        masterRegistry.Clear();
                        foreach (var img in safeThumbnails.Values) img.Dispose();
                        safeThumbnails.Clear();
                        customNames.Clear();

                        lastText = "";
                        lastImgWidth = 0;
                        lastImgHeight = 0;
                        imageCounter = 0;

                        Clipboard.Clear();

                        if (File.Exists(dataPath)) File.Delete(dataPath);

                        if (Directory.Exists(mediaFolderPath))
                        {
                            string[] files = Directory.GetFiles(mediaFolderPath);
                            foreach (string file in files)
                            {
                                File.Delete(file);
                            }
                        }
                    }
                    catch { }
                }

                if (txtSearch.Text != "🔍 Начните писать здесь для поиска...") txtSearch.Text = "🔍 Начните писать здесь для поиска...";
                ApplyFilters();

                statusLabel.Text = " 🗑️ База данных полностью очищена";
                statusLabel.ForeColor = Color.FromArgb(248, 113, 113);

                System.Windows.Forms.Timer clearStatusTimer = new System.Windows.Forms.Timer { Interval = 1000 };
                clearStatusTimer.Tick += (st, evv) =>
                {
                    clearStatusTimer.Stop();
                    statusLabel.Text = " Ready";
                    statusLabel.ForeColor = Color.FromArgb(160, 165, 185);
                    clearStatusTimer.Dispose();
                };
                clearStatusTimer.Start();

                MessageBox.Show("Локальная база данных, папка media и буфер обмена успешно очищены.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            UnregisterHotKey(this.Handle, HOTKEY_ID);
            bool success = RegisterHotKey(this.Handle, HOTKEY_ID, MOD_ALT, 0x58);

            if (!success)
            {
                this.RecreateHandle();
                RegisterHotKey(this.Handle, HOTKEY_ID, MOD_ALT, 0x58);
            }

            UnregisterHotKey(this.Handle, HOTKEY_AUTHOR_ID);
            RegisterHotKey(this.Handle, HOTKEY_AUTHOR_ID, MOD_ALT, KEY_A);

            int trueValue = 1;
            DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref trueValue, sizeof(int));
            DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref trueValue, sizeof(int));

            UpdateTabControlColors();
            UpdateListViewLayout();

            LoadData();
            ApplyFilters();
            listClipboard.Invalidate();

            // ИСПРАВЛЕНО: Разрешаем программе слушать буфер обмена после полной загрузки
            isInitializing = false;
        }


        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_HELP:
                    ShowHelpDialog();
                    return;


                case WM_HOTKEY:
                    int pressedHotkeyId = m.WParam.ToInt32();

                    if (pressedHotkeyId == HOTKEY_ID)
                    {
                        ShowAndActivateForm();
                    }
                    // ДОБАВЬТЕ ЭТОТ БЛОК: Перехват нажатия ALT + A
                    else if (pressedHotkeyId == HOTKEY_AUTHOR_ID)
                    {
                        // Подсвечиваем статус-бар красивым индиго-неоновым цветом
                        statusLabel.Text = " ⚡ made by @ipsoyob";
                        statusLabel.ForeColor = Color.FromArgb(129, 140, 248);

                        // Таймер автоматически вернет надпись "Ready" ровно через 3 секунды
                        System.Windows.Forms.Timer authorTimer = new System.Windows.Forms.Timer { Interval = 3000 };
                        authorTimer.Tick += (st, evv) =>
                        {
                            authorTimer.Stop();
                            statusLabel.Text = " Ready";
                            statusLabel.ForeColor = Color.FromArgb(160, 165, 185);
                            authorTimer.Dispose();
                        };
                        authorTimer.Start();
                    }
                    break;

                case WM_WINDOWPOSCHANGING:
                    UpdateListViewLayout();
                    break;

                case WM_DRAWCLIPBOARD:
                    OnClipboardChangedNotification();
                    SendMessage(nextClipboardViewer, m.Msg, m.WParam, m.LParam);
                    break;

                case WM_CHANGECBCHAIN:
                    if (m.WParam == nextClipboardViewer) nextClipboardViewer = m.LParam;
                    else SendMessage(nextClipboardViewer, m.Msg, m.WParam, m.LParam);
                    break;

                default:
                    base.WndProc(ref m);
                    break;
            }
        }


        private void OnClipboardChangedNotification()
        {
            if (isInitializing) return;

            lock (clipboardLock)
            {
                if (!OpenClipboard(this.Handle)) return;

                try
                {
                    if (IsClipboardFormatAvailable(CF_BITMAP))
                    {
                        IntPtr hBitmap = GetClipboardData(CF_BITMAP);
                        if (hBitmap != IntPtr.Zero)
                        {
                            using (Bitmap bmp = Image.FromHbitmap(hBitmap))
                            {
                                if (bmp.Width == lastImgWidth && bmp.Height == lastImgHeight) return;

                                if (bmp.Width != lastImgWidth || bmp.Height != lastImgHeight)
                                {
                                    lastImgWidth = bmp.Width;
                                    lastImgHeight = bmp.Height;
                                    lastText = "";

                                    int currentCount = ++imageCounter;
                                    string imgId = $"Screenshot {currentCount}";
                                    string metaInfo = $"🖼️ Скриншот ({bmp.Width}x{bmp.Height})";

                                    Image thumb = CreateHighQualityScale(bmp, 48, 48);
                                    safeThumbnails[imgId] = thumb;

                                    string filename = $"Screenshot_{currentCount}.jpg";
                                    string fullPath = Path.Combine(mediaFolderPath, filename);

                                    ImageCodecInfo jpegEncoder = GetEncoder(ImageFormat.Jpeg);
                                    if (jpegEncoder != null)
                                    {
                                        using (EncoderParameters encoderParams = new EncoderParameters(1))
                                        {
                                            encoderParams.Param = new[] { new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 85L) };
                                            bmp.Save(fullPath, jpegEncoder, encoderParams);
                                        }
                                    }

                                    ClipboardPayload payload = new ClipboardPayload
                                    {
                                        Id = "IMG_" + Guid.NewGuid().ToString("N"),
                                        Type = "IMG",
                                        Body = imgId,
                                        Meta = metaInfo,
                                        Alias = ""
                                    };
                                    AddItemToRegistry(payload);
                                }
                            }
                        }
                    }
                    else if (IsClipboardFormatAvailable(CF_UNICODETEXT))
                    {
                        IntPtr hGlobal = GetClipboardData(CF_UNICODETEXT);
                        if (hGlobal != IntPtr.Zero)
                        {
                            IntPtr lpString = GlobalLock(hGlobal);
                            if (lpString != IntPtr.Zero)
                            {
                                string currentText = Marshal.PtrToStringUni(lpString) ?? "";
                                GlobalUnlock(hGlobal);

                                string trimmedText = currentText.Trim();

                                // Если текст полностью совпадает с предыдущим скопированным — игнорируем, чтобы не плодить дубликаты
                                if (trimmedText == lastText.Trim()) return;

                                if (!string.IsNullOrEmpty(trimmedText) && trimmedText != lastText.Trim())
                                {
                                    lastText = currentText;
                                    lastImgWidth = 0;
                                    lastImgHeight = 0;

                                    // ИСПРАВЛЕНО: УДАЛЕНА СТРОКА RemoveAll(), которая вычищала историю базы при старте!

                                    ClipboardPayload payload = new ClipboardPayload
                                    {
                                        Id = "TXT_" + Guid.NewGuid().ToString("N"),
                                        Type = "TXT",
                                        Body = currentText,
                                        Meta = "",
                                        Alias = ""
                                    };
                                    AddItemToRegistry(payload);
                                }
                            }
                        }
                    }
                }
                catch { }
                finally
                {
                    CloseClipboard();
                }
            }
        }

        private void ShowAndActivateForm()
        {
            if (this.IsHandleCreated)
            {
                this.Invoke(new Action(() =>
                {
                    if (this.Visible && this.WindowState == FormWindowState.Normal && Form.ActiveForm == this)
                    {
                        this.WindowState = FormWindowState.Minimized;
                        this.Hide();
                    }
                    else
                    {
                        this.Show();
                        if (this.WindowState == FormWindowState.Minimized)
                        {
                            this.WindowState = FormWindowState.Normal;
                        }
                        SetForegroundWindow(this.Handle);
                        this.Activate();

                        System.Windows.Forms.Timer delayTimer = new System.Windows.Forms.Timer { Interval = 20 };
                        delayTimer.Tick += (s, ev) =>
                        {
                            UpdateListViewLayout();
                            listClipboard.Invalidate();
                            delayTimer.Stop();
                            delayTimer.Dispose();
                        };
                        delayTimer.Start();
                    }
                }));
            }
        }

        private void ListClipboard_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            if (e.Item == null || e.Item.ListView == null || e.Item.Index < 0) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            bool isSelected = e.Item.Selected;
            bool isHovered = e.Item == hoveredItem;
            string rawKeyId = e.Item.Text;
            string itemUid = e.Item.ImageKey;

            Rectangle cardBounds = new Rectangle(e.Bounds.X + 8, e.Bounds.Y + 4, e.Bounds.Width - 16, e.Bounds.Height - 8);

            using (SolidBrush baseBg = new SolidBrush(Color.FromArgb(14, 14, 19)))
            {
                g.FillRectangle(baseBg, e.Bounds);
            }

            int radius = 10;
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddArc(cardBounds.X, cardBounds.Y, radius, radius, 180, 90);
                path.AddArc(cardBounds.Right - radius, cardBounds.Y, radius, radius, 270, 90);
                path.AddArc(cardBounds.Right - radius, cardBounds.Bottom - radius, radius, radius, 0, 90);
                path.AddArc(cardBounds.X, cardBounds.Bottom - radius, radius, radius, 90, 90);
                path.CloseAllFigures();

                if (isSelected || isHovered)
                {
                    Color shadowColor = isSelected ? Color.FromArgb(20, 99, 102, 241) : Color.FromArgb(10, 79, 70, 229);
                    using (PathGradientBrush shadowBrush = new PathGradientBrush(path))
                    {
                        shadowBrush.CenterColor = shadowColor;
                        shadowBrush.SurroundColors = new[] { Color.Transparent };
                        g.FillPath(shadowBrush, path);
                    }
                }

                Color colorTop = Color.FromArgb(26, 26, 38);
                Color colorBottom = Color.FromArgb(18, 18, 26);

                if (itemUid == deletingKeyId) { colorTop = Color.FromArgb(110, 26, 26); colorBottom = Color.FromArgb(70, 16, 16); }
                else if (itemUid == animatedKeyId) { colorTop = Color.FromArgb(18, 85, 60); colorBottom = Color.FromArgb(12, 55, 40); }
                else if (isSelected) { colorTop = Color.FromArgb(36, 38, 64); colorBottom = Color.FromArgb(24, 25, 43); }
                else if (isHovered) { colorTop = Color.FromArgb(32, 32, 46); colorBottom = Color.FromArgb(22, 22, 32); }

                using (LinearGradientBrush bgBrush = new LinearGradientBrush(cardBounds, colorTop, colorBottom, LinearGradientMode.Vertical))
                {
                    g.FillPath(bgBrush, path);
                }

                Color glowColor = Color.FromArgb(38, 38, 52);
                if (itemUid == deletingKeyId) glowColor = Color.FromArgb(248, 113, 113);
                else if (itemUid == animatedKeyId) glowColor = Color.FromArgb(52, 211, 153);
                else if (isSelected) glowColor = Color.FromArgb(129, 140, 248);
                else if (isHovered) glowColor = Color.FromArgb(99, 102, 241);

                using (Pen glowPen = new Pen(glowColor, isSelected ? 1.5f : 1.0f))
                {
                    g.DrawPath(glowPen, path);
                }
            }

            Rectangle textBounds = new Rectangle(cardBounds.X + 16, cardBounds.Y + 18, cardBounds.Width - 32, 28);
            TextFormatFlags textFlags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.LeftAndRightPadding;

            bool hasCustomName = customNames.TryGetValue(rawKeyId, out string displayName);
            if (!hasCustomName || string.IsNullOrEmpty(displayName)) displayName = rawKeyId;
            if (e.Item.Name == "IMG")
            {
                int textX = cardBounds.X + 16;
                Image thumb = null;

                lock (clipboardLock) { safeThumbnails.TryGetValue(rawKeyId, out thumb); }

                if (thumb != null)
                {
                    int imgX = cardBounds.X + 16;
                    int imgY = cardBounds.Y + (cardBounds.Height - thumb.Height) / 2;
                    Rectangle imgRect = new Rectangle(imgX, imgY, thumb.Width, thumb.Height);

                    int imgRadius = 6;
                    using (GraphicsPath imgPath = new GraphicsPath())
                    {
                        imgPath.AddArc(imgRect.X, imgRect.Y, imgRadius, imgRadius, 180, 90);
                        imgPath.AddArc(imgRect.Right - imgRadius, imgRect.Y, imgRadius, imgRadius, 270, 90);
                        imgPath.AddArc(imgRect.Right - imgRadius, imgRect.Bottom - imgRadius, imgRadius, imgRadius, 0, 90);
                        imgPath.AddArc(imgRect.X, imgRect.Bottom - imgRadius, imgRadius, imgRadius, 90, 90);
                        imgPath.CloseAllFigures();

                        g.SetClip(imgPath);
                        g.DrawImage(thumb, imgRect);
                        g.ResetClip();

                        using (Pen imgPen = new Pen(Color.FromArgb(60, 65, 90), 1)) { g.DrawPath(imgPen, imgPath); }
                    }

                    textX = imgX + thumb.Width + 8;
                }

                textBounds = new Rectangle(textX, cardBounds.Y + (cardBounds.Height - 28) / 2, cardBounds.Right - textX - 16, 28);

                string imgMeta;
                if (hasCustomName)
                {
                    imgMeta = displayName;
                }
                else
                {
                    // ТОЧЕЧНАЯ ЗАМЕНА: Считаем номера скриншотов строго по базе данных, игнорируя текст!
                    int imgNumber = 1;
                    lock (clipboardLock)
                    {
                        // Находим индекс текущей картинки в masterRegistry среди ВСЕХ картинок
                        int totalImages = 0;
                        int currentImgPos = -1;

                        for (int i = masterRegistry.Count - 1; i >= 0; i--)
                        {
                            if (masterRegistry[i].Type == "IMG")
                            {
                                totalImages++;
                                // Нам нужен именно тот элемент, который отрисовывается прямо сейчас (совпадение по ImageKey)
                                if (masterRegistry[i].Id == e.Item.ImageKey)
                                {
                                    currentImgPos = totalImages;
                                }
                            }
                        }

                        // Если нашли позицию — присваиваем её, иначе страховочный дефолт
                        if (currentImgPos != -1) imgNumber = currentImgPos;
                    }

                    // Перезаписываем текст заголовка красивым, ровным номером
                    imgMeta = $"Скриншот №{imgNumber}";
                }

                Color textColor = isSelected ? Color.White : Color.FromArgb(165, 180, 252);
                TextRenderer.DrawText(g, "🖼️ " + imgMeta, listClipboard.Font, textBounds, textColor, textFlags);
            }

            else
            {
                if (string.IsNullOrWhiteSpace(displayName)) displayName = "[Пустой фрагмент]";

                // КЛЮЧЕВОЙ ФИКС: Срезаем все табы и пробелы, которые скопировались в начале строки кода
                displayName = displayName.TrimStart();

                // Оставляем один аккуратный пробел после двоеточия для читаемости
                string iconPrefix = displayName.Contains("{") || displayName.Contains(";") || displayName.Contains("void") ? "🖨️ Код: " : "📄 Текст: ";

                Color textColor = isSelected ? Color.White : (hasCustomName ? Color.FromArgb(129, 140, 248) : Color.FromArgb(215, 220, 235));
                TextRenderer.DrawText(g, iconPrefix + displayName, listClipboard.Font, textBounds, textColor, textFlags);
            }


        }

        private void ListClipboard_MouseMove(object sender, MouseEventArgs e)
        {
            ListViewItem item = listClipboard.GetItemAt(e.X, e.Y);
            if (item != hoveredItem)
            {
                hoveredItem = item;
                listClipboard.Invalidate();
            }
        }

        private void ListClipboard_MouseLeave(object sender, EventArgs e)
        {
            if (hoveredItem != null)
            {
                hoveredItem = null;
                listClipboard.Invalidate();
            }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid) return codec;
            }
            return null;
        }

        private void ListClipboard_DoubleClick(object sender, EventArgs e)
        {
            pnlSearchHistory.Visible = false;
            if (listClipboard.SelectedItems.Count == 0) return;
            ListViewItem lvi = listClipboard.SelectedItems[0];
            string keyId = lvi.Text;
            string itemUid = lvi.ImageKey;

            animatedKeyId = itemUid;
            statusLabel.Text = " ✓ Скопировано в буфер обмена!";
            statusLabel.ForeColor = Color.FromArgb(52, 211, 153);

            StopHighHzTimer();
            animationTicksLeft = 25;
            highHzTimerId = TimeSetEvent(16, 1, highHzCallback, UIntPtr.Zero, 1);
            listClipboard.Invalidate();

            lock (clipboardLock)
            {
                try
                {
                    if (lvi.Name == "TXT")
                    {
                        lastText = keyId;
                        lastImgWidth = 0; lastImgHeight = 0;
                        Clipboard.SetText(keyId);
                    }
                    else if (lvi.Name == "IMG")
                    {
                        string numPart = keyId.Replace("Screenshot ", "");
                        string fullPath = Path.Combine(mediaFolderPath, $"Screenshot_{numPart}.jpg");
                        if (File.Exists(fullPath))
                        {
                            using (Image img = Image.FromFile(fullPath))
                            {
                                lastImgWidth = img.Width;
                                lastImgHeight = img.Height;
                                lastText = "";
                                Clipboard.SetImage(img);
                            }
                        }
                    }
                }
                catch
                {
                    statusLabel.Text = " ❌ Ошибка копирования";
                    statusLabel.ForeColor = Color.FromArgb(239, 68, 68);
                }
            }
        }

        private void ListClipboard_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ListViewHitTestInfo hitTest = listClipboard.HitTest(e.Location);
                ListViewItem targetItem = hitTest.Item;

                if (targetItem != null)
                {
                    listClipboard.SelectedItems.Clear();
                    targetItem.Selected = true;
                    targetItem.Focused = true;

                    // 1. Инициализация меню с чистыми отступами
                    ContextMenuStrip contextMenu = new ContextMenuStrip();
                    contextMenu.BackColor = Color.FromArgb(32, 32, 36);
                    contextMenu.ForeColor = Color.FromArgb(243, 244, 246);
                    contextMenu.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
                    contextMenu.ShowImageMargin = false;
                    contextMenu.Padding = new Padding(1);

                    // 2. Создаем кастомный рендерер на базе нашего исправленного класса
                    var customRenderer = new PureRenderer();

                    // Отрисовка фона всего меню
                    customRenderer.RenderToolStripBackground += (s, ev) =>
                    {
                        using (SolidBrush brush = new SolidBrush(Color.FromArgb(32, 32, 36)))
                        {
                            ev.Graphics.FillRectangle(brush, ev.AffectedBounds);
                        }
                    };

                    // Отрисовка пунктов меню и эффекта наведения (Hover)
                    customRenderer.RenderMenuItemBackground += (s, ev) =>
                    {
                        Rectangle rect = new Rectangle(0, 0, ev.Item.Width, ev.Item.Height);
                        if (ev.Item.Selected)
                        {
                            using (SolidBrush brush = new SolidBrush(Color.FromArgb(50, 50, 56)))
                            {
                                ev.Graphics.FillRectangle(brush, rect);
                            }
                        }
                    };

                    // Линия без единого белого пикселя
                    customRenderer.RenderSeparator += (s, ev) =>
                    {
                        using (Pen pen = new Pen(Color.FromArgb(55, 55, 61), 1))
                        {
                            int y = ev.Item.Height / 2;
                            ev.Graphics.DrawLine(pen, 12, y, ev.Item.Width - 12, y);
                        }
                    };

                    // Строгая плоская рамка в 1 пиксель вокруг всего меню
                    customRenderer.RenderToolStripBorder += (s, ev) =>
                    {
                        using (Pen pen = new Pen(Color.FromArgb(62, 62, 70), 1))
                        {
                            ev.Graphics.DrawRectangle(pen, 0, 0, ev.ToolStrip.Width - 1, ev.ToolStrip.Height - 1);
                        }
                    };

                    contextMenu.Renderer = customRenderer;

                    // 3. Создаем пункты меню с увеличенными отступами
                    ToolStripMenuItem itemRename = new ToolStripMenuItem("✏️   Переименовать")
                    {
                        Padding = new Padding(4, 6, 20, 6)
                    };
                    itemRename.Click += (s, ev) => RenameSelectedElement();
                    contextMenu.Items.Add(itemRename);

                    // Разделитель
                    ToolStripSeparator separator = new ToolStripSeparator
                    {
                        Margin = new Padding(0, 2, 0, 2),
                        Height = 5,
                        AutoSize = false
                    };
                    contextMenu.Items.Add(separator);

                    ToolStripMenuItem itemDelete = new ToolStripMenuItem("❌   Удалить запись")
                    {
                        Padding = new Padding(4, 6, 20, 6)
                    };
                    itemDelete.Click += (s, ev) => DeleteSelectedElement();
                    contextMenu.Items.Add(itemDelete);

                    // Отображаем готовое меню
                    contextMenu.Show(listClipboard, e.Location);
                }
            }
        }

        // Обновленный класс-отрисовщик. 
        // Теперь он жестко контролирует вывод текста и не дает операционной системе двоить строки.
        private class PureRenderer : ToolStripRenderer
        {
            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                // Сдвигаем координаты отрисовки текста вправо на 14 пикселей
                Rectangle textRect = e.TextRectangle;
                textRect.X = 14;

                // Рисуем текст вручную
                TextRenderer.DrawText(e.Graphics, e.Text, e.Item.Font, textRect, e.TextColor, TextFormatFlags.VerticalCenter);

                // ВАЖНО: Мы НЕ вызываем base.OnRenderItemText(e), чтобы заблокировать стандартное двоение текста WinForms
            }
        }




        private void ListClipboard_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                e.Handled = true; e.SuppressKeyPress = true;
                DeleteSelectedElement();
            }
            else if (e.KeyCode == Keys.F2)
            {
                e.Handled = true; e.SuppressKeyPress = true;
                RenameSelectedElement();
            }
            else if (e.KeyCode == Keys.P)
            {
                e.Handled = true; e.SuppressKeyPress = true;

                if (previewForm != null && !previewForm.IsDisposed)
                {
                    previewForm.Close(); previewForm = null; return;
                }

                if (listClipboard.SelectedItems.Count == 0) return;
                ListViewItem lvi = listClipboard.SelectedItems[0];
                string id = lvi.Text;

                if (lvi.Name == "IMG")
                {
                    string numPart = id.Replace("Screenshot ", "");
                    string imgPath = Path.Combine(mediaFolderPath, $"Screenshot_{numPart}.jpg");

                    if (File.Exists(imgPath))
                    {
                        try
                        {
                            this.TopMost = false;
                            Image originalImg = Image.FromFile(imgPath);

                            previewForm = new Form
                            {
                                Text = "Кинотеатр ClpX",
                                FormBorderStyle = FormBorderStyle.None,
                                WindowState = FormWindowState.Maximized,
                                BackColor = Color.FromArgb(10, 10, 14),
                                ShowInTaskbar = false,
                                TopMost = true,
                                KeyPreview = true
                            };

                            PictureBox picBox = new PictureBox
                            {
                                Image = originalImg,
                                Size = originalImg.Size,
                                SizeMode = PictureBoxSizeMode.Normal,
                                Cursor = Cursors.Hand,
                                Location = new Point(
                                    (Screen.PrimaryScreen.Bounds.Width - originalImg.Width) / 2,
                                    (Screen.PrimaryScreen.Bounds.Height - originalImg.Height) / 2
                                )
                            };

                            if (originalImg.Width > Screen.PrimaryScreen.Bounds.Width ||
                                originalImg.Height > Screen.PrimaryScreen.Bounds.Height)
                            {
                                picBox.Dock = DockStyle.Fill;
                                picBox.SizeMode = PictureBoxSizeMode.Zoom;
                            }

                            picBox.Click += (s, ev) => previewForm.Close();
                            previewForm.Click += (s, ev) => previewForm.Close();

                            previewForm.KeyDown += (s, ev) =>
                            {
                                if (ev.KeyCode == Keys.Escape || ev.KeyCode == Keys.P) previewForm.Close();
                            };

                            previewForm.FormClosed += (s, ev) =>
                            {
                                picBox.Image.Dispose(); previewForm.Dispose(); previewForm = null;
                                this.TopMost = true; this.Activate();
                            };

                            previewForm.Controls.Add(picBox);
                            previewForm.Show();
                        }
                        catch
                        {
                            this.TopMost = true;
                            statusLabel.Text = " ❌ Не удалось загрузить предпросмотр";
                            statusLabel.ForeColor = Color.FromArgb(239, 68, 68);
                        }
                    }
                }
                else
                {
                    statusLabel.Text = " 💡 Предпросмотр доступен только для скриншотов!";
                    statusLabel.ForeColor = Color.FromArgb(129, 140, 248);
                }
            }
        }

        private void RenameSelectedElement()
        {
            if (listClipboard.SelectedItems.Count == 0) return;
            ListViewItem lvi = listClipboard.SelectedItems[0];
            string keyId = lvi.Text;

            customNames.TryGetValue(keyId, out string currentName);
            string initialInput = currentName ?? (lvi.Name == "TXT" ? keyId : "Скриншот");

            // 1. Создаем полностью темную кастомную форму диалога
            Form renameDialog = new Form();
            renameDialog.Text = "Переименовать";
            renameDialog.Size = new Size(350, 140); // Твои оригинальные размеры
            renameDialog.FormBorderStyle = FormBorderStyle.None; // УБИРАЕТ БЕЛУЮ ПОЛОСУ
            renameDialog.StartPosition = FormStartPosition.CenterParent;
            renameDialog.MaximizeBox = false;
            renameDialog.MinimizeBox = false;
            renameDialog.BackColor = Color.FromArgb(24, 24, 35); // Твой оригинальный цвет фона
            renameDialog.ForeColor = Color.White;
            renameDialog.TopMost = true;

            // Блокируем перетаскивание окна (используем тот же DragBlockerNativeWindow)
            var dragBlocker = new DragBlockerNativeWindow();
            renameDialog.HandleCreated += (s, e) => dragBlocker.AssignHandle(renameDialog.Handle);
            renameDialog.HandleDestroyed += (s, e) => dragBlocker.ReleaseHandle();

            // 2. Добавляем кастомный текст заголовка вместо белой полосы
            Label lblTitle = new Label
            {
                Text = "Переименовать",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(150, 150, 160),
                Location = new Point(15, 12),
                AutoSize = true
            };
            renameDialog.Controls.Add(lblTitle);

            // Кастомный крестик закрытия окна в правом верхнем углу
            Label btnClose = new Label
            {
                Text = "✕",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(150, 150, 160),
                Location = new Point(renameDialog.ClientSize.Width - 28, 10),
                Size = new Size(18, 18),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.FromArgb(239, 68, 68); // Краснеет при наведении
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = Color.FromArgb(150, 150, 160);
            btnClose.Click += (s, e) => renameDialog.DialogResult = DialogResult.Cancel;
            renameDialog.Controls.Add(btnClose);

            // 3. Текстовое поле (переместил чуть ниже из-за заголовка: Y=42 вместо Y=20)
            TextBox txtInput = new TextBox
            {
                Text = initialInput,
                Location = new Point(15, 42),
                Width = 320,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(34, 34, 48), // Твой цвет
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Автофокус и выделение текста при открытии
            renameDialog.Shown += (s, e) => { txtInput.Focus(); txtInput.SelectAll(); };

    // 4. Кнопки управления
    Button btnOk = new Button { 
        Text = "Сохранить", 
        DialogResult = DialogResult.OK, 
        Location = new Point(140, 88), 
        Size = new Size(90, 30), 
        FlatStyle = FlatStyle.Flat 
    };
    btnOk.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 80);
    btnOk.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 40, 60); // <-- СЮДА
    btnOk.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 30, 45);  // <-- СЮДА

    Button btnCancel = new Button { 
        Text = "Отмена", 
        DialogResult = DialogResult.Cancel, 
        Location = new Point(245, 88), 
        Size = new Size(90, 30), 
        FlatStyle = FlatStyle.Flat 
    };
    btnCancel.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 80);
    btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(34, 34, 48); // <-- СЮДА
    btnCancel.FlatAppearance.MouseDownBackColor = Color.FromArgb(24, 24, 35);  // <-- СЮДА

    // Собираем элементы на форме
    renameDialog.Controls.AddRange(new Control[] { txtInput, btnOk, btnCancel });
    
    // ... твой код ниже ...

            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 80);

            // Собираем элементы на форме
            renameDialog.Controls.AddRange(new Control[] { txtInput, btnOk, btnCancel });
            renameDialog.AcceptButton = btnOk;
            renameDialog.CancelButton = btnCancel;

            // Тонкая стильная рамка в 1 пиксель вокруг самого диалога, чтобы он не сливался с Windows
            renameDialog.Paint += (s, e) =>
            {
                using (Pen p = new Pen(Color.FromArgb(50, 50, 70), 1))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, renameDialog.Width - 1, renameDialog.Height - 1);
                }
            };

            // 5. Логика обработки результата (твой оригинальный код без изменений)
            if (renameDialog.ShowDialog(this) == DialogResult.OK)
            {
                string trimmed = txtInput.Text.Trim();
                lock (clipboardLock)
                {
                    if (!string.IsNullOrEmpty(trimmed)) customNames[keyId] = trimmed;
                    else customNames.Remove(keyId);

                    var registryItem = masterRegistry.Find(x => x.Id == lvi.ImageKey);
                    if (registryItem != null) registryItem.Alias = trimmed;
                }
                SaveData();
                ApplyFilters();
            }
        }


        private void DeleteSelectedElement()
        {
            if (listClipboard.SelectedItems.Count == 0) return;
            ListViewItem lvi = listClipboard.SelectedItems[0];
            string id = lvi.Text;
            string itemUid = lvi.ImageKey;

            deletingKeyId = itemUid;
            statusLabel.Text = " ❌ Запись успешно удалена!";
            statusLabel.ForeColor = Color.FromArgb(248, 113, 113);
            listClipboard.Invalidate();

            System.Windows.Forms.Timer delayDeleteTimer = new System.Windows.Forms.Timer { Interval = 350 };
            delayDeleteTimer.Tick += (s, ev) =>
            {
                delayDeleteTimer.Stop();
                lock (clipboardLock)
                {
                    if (lvi.Name == "IMG" && safeThumbnails.ContainsKey(id))
                    {
                        safeThumbnails[id].Dispose();
                        safeThumbnails.Remove(id);
                    }

                    try
                    {
                        if (lvi.Name == "IMG")
                        {
                            string numPart = id.Replace("Screenshot ", "");
                            string filePath = Path.Combine(mediaFolderPath, $"Screenshot_{numPart}.jpg");
                            if (File.Exists(filePath)) File.Delete(filePath);
                        }
                    }
                    catch { }

                    customNames.Remove(id);
                    masterRegistry.RemoveAll(x => x.Id == itemUid);
                }

                deletingKeyId = "";
                SaveData();
                ApplyFilters();
                delayDeleteTimer.Dispose();

                System.Windows.Forms.Timer clearStatusTimer = new System.Windows.Forms.Timer { Interval = 1000 };
                clearStatusTimer.Tick += (st, evv) =>
                {
                    clearStatusTimer.Stop();
                    statusLabel.Text = " Ready";
                    statusLabel.ForeColor = Color.FromArgb(160, 165, 185);
                    clearStatusTimer.Dispose();
                };
                clearStatusTimer.Start();
            };
            delayDeleteTimer.Start();
        }

        private void AddItemToRegistry(ClipboardPayload item)
        {
            if (string.IsNullOrEmpty(item.Id))
            {
                item.Id = item.Type + "_" + Guid.NewGuid().ToString("N");
            }

            masterRegistry.Insert(0, item);

            // ИСПРАВЛЕНО: Блок ограничения > 100 полностью стерт!

            SaveData();
            ApplyFilters();
        }

        private void SaveData()
        {
            try
            {
                lock (clipboardLock)
                {
                    // Исправленный метод: переводим весь список в чистую текстовую строку JSON
                    string jsonArray = JsonSerializer.Serialize(masterRegistry);

                    // Вручную собираем пуленепробиваемую структуру файла
                    string fullJsonString = "{\n  \"Counter\": " + imageCounter + ",\n  \"Data\": " + jsonArray + "\n}";

                    // Записываем текст на диск через поток StreamWriter
                    using (FileStream fs = new FileStream(dataPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                    {
                        using (StreamWriter sw = new StreamWriter(fs))
                        {
                            sw.Write(fullJsonString);
                            sw.Flush();
                        }
                    }
                }
            }
            catch { }
        }

        private void LoadData()
        {
            try
            {
                if (!File.Exists(dataPath)) return;

                // Используем безопасный поток для чтения файлов истории
                using (FileStream fs = new FileStream(dataPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (JsonDocument doc = JsonDocument.Parse(fs))
                    {
                        JsonElement root = doc.RootElement;
                        if (root.TryGetProperty("Counter", out JsonElement countProp)) imageCounter = countProp.GetInt32();

                        if (root.TryGetProperty("Data", out JsonElement dataProp))
                        {
                            var dataset = JsonSerializer.Deserialize<List<ClipboardPayload>>(dataProp.GetRawText());
                            if (dataset == null) return;

                            masterRegistry.Clear();
                            masterRegistry.AddRange(dataset);

                            foreach (var node in masterRegistry)
                            {
                                if (!string.IsNullOrEmpty(node.Alias))
                                {
                                    customNames[node.Body] = node.Alias;
                                }

                                if (node.Type == "IMG")
                                {
                                    string imgId = node.Body;
                                    string numPart = imgId.Replace("Screenshot ", "");
                                    string fullPath = Path.Combine(mediaFolderPath, $"Screenshot_{numPart}.jpg");

                                    if (File.Exists(fullPath))
                                    {
                                        byte[] bytes = File.ReadAllBytes(fullPath);
                                        using (MemoryStream ms = new MemoryStream(bytes))
                                        {
                                            using (Image fullImg = Image.FromStream(ms))
                                            {
                                                Image thumb = CreateHighQualityScale(fullImg, 48, 48);
                                                lock (clipboardLock) { safeThumbnails[imgId] = thumb; }
                                            }
                                        }
                                    }
                                }
                            }

                            // КРИТИЧЕСКИЙ ВЫПРАВЛЕННЫЙ ТРИГГЕР: Забираем точный первый элемент коллекции по индексу [0]
                            if (masterRegistry.Count > 0)
                            {
                                var topItem = masterRegistry[0]; // Исправлено: точечный выбор ячейки вместо коллекции
                                if (topItem.Type == "TXT")
                                {
                                    lastText = topItem.Body;
                                    lastImgWidth = 0;
                                    lastImgHeight = 0;
                                }
                                else if (topItem.Type == "IMG")
                                {
                                    lastText = "";
                                    lastImgWidth = 1;
                                    lastImgHeight = 1;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            finally
            {
                ApplyFilters();
                listClipboard.Invalidate();
            }
        }


        private Image CreateHighQualityScale(Image src, int maxWidth, int maxHeight)
        {
            double ratioX = (double)maxWidth / src.Width;
            double ratioY = (double)maxHeight / src.Height;
            double ratio = Math.Min(ratioX, ratioY);

            int newWidth = (int)(src.Width * ratio);
            int newHeight = (int)(src.Height * ratio);
            if (newWidth < 1) newWidth = 1;
            if (newHeight < 1) newHeight = 1;

            Bitmap bmp = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppRgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                g.Clear(Color.FromArgb(24, 24, 35));
                g.DrawImage(src, 0, 0, newWidth, newHeight);
            }
            return bmp;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Если пользователь закрывает окно крестиком, просто прячем его в трей
            if (!allowActualClose)
            {
                e.Cancel = true;
                this.Hide();
            }
            else
            {
                // Если закрытие вызвано из контекстного меню трея ("Полный выход"), чистим Win32 ресурсы
                StopHighHzTimer();
                ChangeClipboardChain(this.Handle, nextClipboardViewer);
                UnregisterHotKey(this.Handle, HOTKEY_ID);
                UnregisterHotKey(this.Handle, HOTKEY_AUTHOR_ID);
                trayIcon.Dispose();
                lock (clipboardLock)
                {
                    foreach (var img in safeThumbnails.Values) img.Dispose();
                }
            }
        }

        private void ShowHelpDialog()
        {
            // 1. Инициализация и базовая кастомизация тёмной формы
            Form helpForm = new Form();
            helpForm.Text = "Справка по использованию ClpX Ultimate";
            helpForm.ClientSize = new Size(520, 520);
            helpForm.StartPosition = FormStartPosition.CenterParent;
            helpForm.FormBorderStyle = FormBorderStyle.None; // Полностью убирает верхнюю белую полосу (заголовок)
            helpForm.MaximizeBox = false;
            helpForm.MinimizeBox = false;
            helpForm.BackColor = Color.FromArgb(26, 26, 30);
            helpForm.ForeColor = Color.FromArgb(243, 244, 246);
            helpForm.TopMost = true;

            // ==========================================
            // БЛОКИРОВКА ПЕРЕТАСКИВАНИЯ (WndProc через NativeWindow)
            // ==========================================
            var dragBlocker = new DragBlockerNativeWindow();
            helpForm.HandleCreated += (s, e) => dragBlocker.AssignHandle(helpForm.Handle);
            helpForm.HandleDestroyed += (s, e) => dragBlocker.ReleaseHandle();

            // Включаем поддержку закрытия окна по кнопкам F1 и Escape
            helpForm.KeyPreview = true;
            helpForm.KeyDown += (s, e) => { if (e.KeyCode == Keys.F1 || e.KeyCode == Keys.Escape) helpForm.Close(); };

            // Стилизация шрифтов и палитры
            Font fontTitle = new Font("Segoe UI", 11F, FontStyle.Bold);
            Font fontSection = new Font("Segoe UI", 10F, FontStyle.Bold);
            Font fontText = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            Font fontKbd = new Font("Consolas", 9F, FontStyle.Bold);

            Color colorGrayText = Color.FromArgb(168, 165, 185);
            Color colorAccent = Color.FromArgb(0, 230, 118);

            int currentY = 24;

            // ==========================================
            // 2. ШАПКА ОКНА
            // ==========================================
            Label lblHeader = new Label
            {
                Text = "⚡ МЕНЕДЖЕР БУФЕРА ОБМЕНА ClpX Ultimate v5.0",
                Font = fontTitle,
                ForeColor = colorGrayText,
                Location = new Point(24, currentY),
                AutoSize = true
            };
            helpForm.Controls.Add(lblHeader);

            currentY += 44;

            // ==========================================
            // 3. БЛОК: ГОРЯЧИЕ КЛАВИШИ
            // ==========================================
            Label lblSecHotkeys = new Label
            {
                Text = "⌨️ Горячие клавиши управления",
                Font = fontSection,
                ForeColor = Color.White,
                Location = new Point(24, currentY),
                AutoSize = true
            };
            helpForm.Controls.Add(lblSecHotkeys);

            currentY += 25;

            string[,] hotkeysData = new string[,] {
        { "Alt + X", "Быстрый вызов / скрытие главного окна" },
        { "Alt + A", "Проверка авторских прав разработчика" },
        { "F1", "Вызов данного окна справки" },
        { "Delete", "Безвозвратное удаление выбранной карточки"},
        { "P", "Предпросмотр скриншота" },
    };

            for (int i = 0; i < hotkeysData.GetLength(0); i++)
            {
                string keys = hotkeysData[i, 0];
                string desc = hotkeysData[i, 1];

                Panel pnlKbd = new Panel
                {
                    Location = new Point(28, currentY),
                    Size = new Size(105, 24),
                    BackColor = Color.FromArgb(41, 41, 46),
                    Padding = new Padding(1)
                };

                pnlKbd.Paint += (s, e) =>
                {
                    ControlPaint.DrawBorder(e.Graphics, pnlKbd.ClientRectangle,
                        Color.FromArgb(62, 62, 70), ButtonBorderStyle.Solid);
                };

                Label lblKeys = new Label
                {
                    Text = keys,
                    Font = fontKbd,
                    ForeColor = Color.White,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill
                };
                pnlKbd.Controls.Add(lblKeys);
                helpForm.Controls.Add(pnlKbd);

                Label lblDesc = new Label
                {
                    Text = desc,
                    Font = fontText,
                    ForeColor = colorGrayText,
                    Location = new Point(148, currentY + 3),
                    AutoSize = true
                };
                helpForm.Controls.Add(lblDesc);

                currentY += 34;
            }

            currentY += 12;

            // ==========================================
            // 4. БЛОК: ИНТЕРФЕЙС И UX
            // ==========================================
            Label lblSecUi = new Label
            {
                Text = "🔬 Интерфейс и UX",
                Font = fontSection,
                ForeColor = Color.White,
                Location = new Point(24, currentY),
                AutoSize = true
            };
            helpForm.Controls.Add(lblSecUi);

            currentY += 28;

            string[] uiData = new string[] {
        "Двойной клик по карточке — мгновенный возврат данных в буфер.",
        "Правый клик (ПКМ) — контекстное меню (Переименовать / Удалить).",
        "Вкладки вверху — фильтрация истории (Все / Текст / Изображения)."
    };

            foreach (string item in uiData)
            {
                Label lblBullet = new Label
                {
                    Text = "•",
                    Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                    ForeColor = colorAccent,
                    Location = new Point(28, currentY - 3),
                    Size = new Size(15, 20)
                };
                helpForm.Controls.Add(lblBullet);

                Label lblUiDesc = new Label
                {
                    Text = item,
                    Font = fontText,
                    ForeColor = colorGrayText,
                    Location = new Point(48, currentY),
                    AutoSize = true
                };
                helpForm.Controls.Add(lblUiDesc);

                currentY += 28;
            }

            currentY += 12;

            // ==========================================
            // 5. ИНФО-БЛОК (ПЛАШКА С ЛАМПОЧКОЙ)
            // ==========================================
            Panel pnlInfo = new Panel
            {
                Location = new Point(24, currentY),
                Size = new Size(472, 54),
                BackColor = Color.FromArgb(32, 32, 36)
            };

            pnlInfo.Paint += (s, e) =>
            {
                using (Pen p = new Pen(colorAccent, 4))
                {
                    e.Graphics.DrawLine(p, 0, 0, 0, pnlInfo.Height);
                }
            };

            Label lblInfoIcon = new Label
            {
                Text = "💡",
                Font = new Font("Segoe UI", 12F),
                Location = new Point(12, 16),
                Size = new Size(25, 25)
            };
            pnlInfo.Controls.Add(lblInfoIcon);

            Label lblInfoText = new Label
            {
                Text = "Программа работает в асинхронном режиме на частоте 144Hz+ без блокировок UI.",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(141, 141, 153),
                Location = new Point(42, 11),
                Size = new Size(420, 35),
                FlatStyle = FlatStyle.System
            };
            pnlInfo.Controls.Add(lblInfoText);
            helpForm.Controls.Add(pnlInfo);

            // ==========================================
            // 6. КНОПКА ЗАКРЫТИЯ (ОК)
            // ==========================================
            Button btnOk = new Button
            {
                Text = "Отлично",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Size = new Size(110, 36),
                Location = new Point(386, helpForm.ClientSize.Height - 60),
                FlatStyle = FlatStyle.Flat,
                BackColor = colorAccent,
                ForeColor = Color.FromArgb(5, 5, 5),
                Cursor = Cursors.Hand
            };
            btnOk.FlatAppearance.BorderSize = 0;

            btnOk.MouseEnter += (s, e) => btnOk.BackColor = Color.FromArgb(0, 200, 83);
            btnOk.MouseLeave += (s, e) => btnOk.BackColor = colorAccent;
            btnOk.Click += (s, e) => helpForm.Close();

            helpForm.Controls.Add(btnOk);

            // Показываем созданное окно поверх основного
            helpForm.ShowDialog();
        }

        // Вспомогательный класс для перехвата сообщений динамической формы.
        // Разместите его внутри вашего основного класса (рядом с методом ShowHelpDialog)
        private class DragBlockerNativeWindow : NativeWindow
        {
            private const int WM_NCHITTEST = 0x84;
            private const int HTCLIENT = 0x01;

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_NCHITTEST)
                {
                    m.Result = (IntPtr)HTCLIENT; // Принудительно сообщаем ОС, что курсор находится на клиентской области
                    return;
                }
                base.WndProc(ref m);
            }
        }
        private void txtSearch_Click(object sender, EventArgs e)
        {
            // При клике принудительно перерисовываем и показываем всю историю
            UpdateHistoryPanelVisuals();
        }

        private void txtSearch_Leave(object sender, EventArgs e)
        {
            // ПОЛНОСТЬЮ ОЧИСТИ ЭТОТ МЕТОД (удали код внутри), чтобы он ничего не скрывал автоматически!
        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string query = txtSearch.Text.Trim().ToLower();
                if (query != "🔍 начните писать здесь для поиска..." && !string.IsNullOrEmpty(query))
                {
                    AddToSearchHistory(query);
                }
                e.SuppressKeyPress = true;
            }
        }

        private void AddToSearchHistory(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return;

            if (searchHistory.Contains(query))
            {
                searchHistory.Remove(query);
            }

            searchHistory.Insert(0, query);

            if (searchHistory.Count > 5)
            {
                searchHistory.RemoveAt(searchHistory.Count - 1);
            }
        }
        private void UpdateHistoryPanelVisuals()
        {
            // Очищаем старые кнопки
            pnlSearchHistory.Controls.Clear();

            // Фильтруем историю: показываем только то, что начинается с текущего ввода (если пользователь уже что-то пишет)
            string currentText = txtSearch.Text.Trim().ToLower();
            if (currentText == "🔍 начните писать здесь для поиска...") currentText = "";

            var filteredHistory = searchHistory;
            if (!string.IsNullOrEmpty(currentText))
            {
                filteredHistory = searchHistory.Where(h => h.StartsWith(currentText)).ToList();
            }
            // ЕСЛИ ПОДХОДЯЩИХ ВАРИАНТОВ НЕТ — скрываем плашку и выходим
            if (filteredHistory.Count == 0)
            {
                pnlSearchHistory.Visible = false;
                return;
            }
            // Идеально подгоняем размеры и позицию под РЕАЛЬНЫЙ размер txtSearch прямо сейчас
            pnlSearchHistory.Left = txtSearch.Left;
            pnlSearchHistory.Width = txtSearch.Width;
            pnlSearchHistory.Top = txtSearch.Bottom; // Теперь это сработает, так как элемент уже на форме

            int topOffset = 0;

            foreach (string historyQuery in filteredHistory)
            {
                Button btnHistoryItem = new Button();
                btnHistoryItem.Text = "  🕒  " + historyQuery;
                btnHistoryItem.Width = pnlSearchHistory.Width;
                btnHistoryItem.Height = 28;
                btnHistoryItem.Left = 0;
                btnHistoryItem.Top = topOffset;

                btnHistoryItem.FlatStyle = FlatStyle.Flat;
                btnHistoryItem.ForeColor = Color.FromArgb(165, 180, 252);
                btnHistoryItem.BackColor = Color.FromArgb(30, 30, 42); // Цвет как у поля поиска
                btnHistoryItem.FlatAppearance.BorderSize = 0;
                btnHistoryItem.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 45, 58);
                btnHistoryItem.TextAlign = ContentAlignment.MiddleLeft;
                btnHistoryItem.Cursor = Cursors.Hand;

                btnHistoryItem.Click += (s, e) =>
                {
                    txtSearch.Text = historyQuery;
                    pnlSearchHistory.Visible = false;
                    ApplyFilters();
                };

                pnlSearchHistory.Controls.Add(btnHistoryItem);
                topOffset += 28;
            }

            // Устанавливаем точную высоту панели по кнопкам
            pnlSearchHistory.Height = topOffset;
            pnlSearchHistory.Visible = true;
            pnlSearchHistory.BringToFront();
        }

    }
}