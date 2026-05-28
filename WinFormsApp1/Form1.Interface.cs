using System;
using System.Drawing;
using System.Windows.Forms;

namespace Clpx
{
    public partial class Form1 : Form
    {
        private void BuildMyInterface()
        {
            string primaryFont = "Segoe UI Variable Display";
            Font infoFont = new Font(primaryFont, 10.5f, FontStyle.Bold);
            if (infoFont.Name != primaryFont) infoFont = new Font("Segoe UI", 10.5f, FontStyle.Bold);

            Font listFont = new Font(primaryFont, 10.5f, FontStyle.Regular);
            if (listFont.Name != primaryFont) listFont = new Font("Segoe UI", 10.5f, FontStyle.Regular);

            Font statusFont = new Font(primaryFont, 9.5f, FontStyle.Bold);
            if (statusFont.Name != primaryFont) statusFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);

            this.Text = "ClpX Ultimate";
            this.Size = new Size(560, 780);
            this.MinimumSize = new Size(520, 600);
            this.BackColor = Color.FromArgb(14, 14, 19);
            this.TopMost = true;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;

            infoPanel.Dock = DockStyle.Top;
            infoPanel.BackColor = Color.FromArgb(22, 22, 30);
            infoPanel.Padding = new Padding(16, 12, 16, 14);
            infoPanel.Height = 220;

            lblInfo.AutoSize = false;
            lblInfo.ForeColor = Color.FromArgb(165, 180, 252);
            lblInfo.Font = infoFont;
            lblInfo.Text = "💡 Нужна помощь?\n" + "Нажми F1";
            infoPanel.Controls.Add(lblInfo);

            tabPanel.BackColor = Color.Transparent;

            BuildTabButton(btnTabAll, "⚡ Всё", 0, "ALL", infoFont);
            BuildTabButton(btnTabTxt, "📄 Текст", 130, "TXT", infoFont);
            BuildTabButton(btnTabImg, "🖼️ Картинки", 260, "IMG", infoFont);

            infoPanel.Controls.Add(tabPanel);

            txtSearch.Font = listFont;
            txtSearch.BackColor = Color.FromArgb(30, 30, 42);
            txtSearch.ForeColor = Color.White;
            txtSearch.BorderStyle = BorderStyle.FixedSingle;
            txtSearch.Text = "🔍 Начните писать здесь для поиска...";
            txtSearch.Enter += (s, e) => { if (txtSearch.Text == "🔍 Начните писать здесь для поиска...") txtSearch.Text = ""; };
            txtSearch.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(txtSearch.Text)) txtSearch.Text = "🔍 Начните писать здесь для поиска..."; };
            txtSearch.TextChanged += (s, e) =>
            {
                ApplyFilters();
                UpdateHistoryPanelVisuals(); // Фильтруем историю на лету!
            };
            infoPanel.Controls.Add(txtSearch);

            // Создаем полоску истории строго под поиском
            pnlSearchHistory = new Panel();
            pnlSearchHistory.Visible = false;
            pnlSearchHistory.BackColor = Color.FromArgb(30, 30, 42); // Темный цвет в тон интерфейса
                                                                     // Настройки позиции панели истории
            pnlSearchHistory.Left = txtSearch.Left;
            pnlSearchHistory.Top = txtSearch.Bottom; // Ровно под нижней границей поиска
            pnlSearchHistory.Top = 180;
            pnlSearchHistory.Width = txtSearch.Width;

            // Убираем уродливые стандартные рамки, если они появятся
            pnlSearchHistory.BorderStyle = BorderStyle.None;


            // Привязываем нужные события
            txtSearch.Click += txtSearch_Click;
            txtSearch.Leave += txtSearch_Leave;
            txtSearch.KeyDown += txtSearch_KeyDown;

            // Добавляем панель истории в infoPanel, чтобы она была в том же контейнере
            infoPanel.Controls.Add(pnlSearchHistory);
            pnlSearchHistory.BringToFront();


            btnClearDb.Text = "🗑️ Стереть всё";
            btnClearDb.FlatStyle = FlatStyle.Flat;
            btnClearDb.FlatAppearance.BorderColor = Color.FromArgb(239, 68, 68);
            btnClearDb.FlatAppearance.BorderSize = 1;
            btnClearDb.ForeColor = Color.FromArgb(248, 113, 113);
            btnClearDb.Font = infoFont;
            btnClearDb.Cursor = Cursors.Hand;
            btnClearDb.Click += (s, e) => ClearFullDatabase();
            infoPanel.Controls.Add(btnClearDb);

            this.Controls.Add(infoPanel);

            listClipboard.Dock = DockStyle.Fill;
            listClipboard.BackColor = Color.FromArgb(14, 14, 19);
            listClipboard.ForeColor = Color.FromArgb(230, 230, 240);
            listClipboard.Font = listFont;
            listClipboard.BorderStyle = BorderStyle.None;
            listClipboard.View = View.Details;
            listClipboard.HeaderStyle = ColumnHeaderStyle.None;
            listClipboard.Columns.Add("Data", 520);

            ImageList itemHeightSpacer = new ImageList();
            itemHeightSpacer.ImageSize = new Size(1, ITEM_ROW_HEIGHT - 4);
            listClipboard.SmallImageList = itemHeightSpacer;

            

            listClipboard.MultiSelect = false;
            listClipboard.FullRowSelect = true;
            listClipboard.OwnerDraw = true;

            listClipboard.DrawItem += (s, e) => { e.DrawDefault = false; };
            listClipboard.DrawSubItem += (s, e) => ListClipboard_DrawSubItem(s, e);
            listClipboard.MouseMove += (s, e) => ListClipboard_MouseMove(s, e);
            listClipboard.MouseLeave += (s, e) => ListClipboard_MouseLeave(s, e);

            this.Controls.Add(listClipboard);

            statusStrip.BackColor = Color.FromArgb(22, 22, 30);
            statusStrip.RenderMode = ToolStripRenderMode.System;
            statusLabel.ForeColor = Color.FromArgb(140, 145, 160);
            statusLabel.Font = statusFont;
            statusLabel.Text = " Программа работает и запоминает ваш буфер";
            statusStrip.Items.Add(statusLabel);
            this.Controls.Add(statusStrip);

            statusStrip.BringToFront();
            infoPanel.BringToFront();
            listClipboard.BringToFront();

            try
            {
                Icon embeddedIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (embeddedIcon != null)
                {
                    this.Icon = embeddedIcon;
                    trayIcon.Icon = embeddedIcon;
                }
            }
            catch
            {
                trayIcon.Icon = SystemIcons.Application;
            }

            trayIcon.Text = "ClpX";
            trayIcon.Visible = true;
            trayIcon.DoubleClick += (s, e) => ShowAndActivateForm();

            trayMenu.Items.Add("Открыть менеджер", null, (s, e) => ShowAndActivateForm());
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("Закрыть полностью", null, (s, e) => { allowActualClose = true; this.Close(); });
            trayIcon.ContextMenuStrip = trayMenu;

            listClipboard.DoubleClick += (s, e) => ListClipboard_DoubleClick(s, e);
            listClipboard.KeyDown += (s, e) => ListClipboard_KeyDown(s, e);
            listClipboard.MouseClick += (s, e) => ListClipboard_MouseClick(s, e);

            lblNoResults.AutoSize = false;
            lblNoResults.Size = new Size(300, 30);
            lblNoResults.ForeColor = Color.FromArgb(140, 145, 160); // Приглушенный серый
            lblNoResults.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            lblNoResults.Text = "🔍 Ничего не найдено";
            lblNoResults.TextAlign = ContentAlignment.MiddleCenter;
            lblNoResults.Visible = false; // По умолчанию скрыта

            // Добавляем её прямо на форму и выводим на самый передний план
            this.Controls.Add(lblNoResults);
            lblNoResults.BringToFront();
            // ТОЧЕЧНАЯ ЗАМЕНА СТРОК 145-154
            this.SizeChanged += (s, e) =>
            {
                // 1. Центруем надпись "Ничего не найдено"
                lblNoResults.Location = new Point(
                    listClipboard.Left + (listClipboard.Width - lblNoResults.Width) / 2,
                    listClipboard.Top + (listClipboard.Height - lblNoResults.Height) / 3
                );

                // 2. ИСПРАВЛЕНО: Принудительно растягиваем карточки и обновляем их графику
                UpdateListViewLayout();
                listClipboard.Invalidate();
            };

            this.Activated += (s, e) => { UpdateListViewLayout(); listClipboard.Invalidate(); };
            this.FormClosing += (s, e) => { if (s != null) Form1_FormClosing(s, e); };
            this.Load += (s, e) => Form1_Load(s, e);


            UpdateListViewLayout();
        }

        private void BuildTabButton(Button btn, string text, int x, string filterTag, Font font)
        {
            btn.Text = text;
            btn.Size = new Size(120, 30);
            btn.Location = new Point(x, 2);
            btn.FlatStyle = FlatStyle.Flat;
            btn.Cursor = Cursors.Hand;
            btn.Font = font;
            btn.Tag = filterTag;
            btn.FlatAppearance.BorderSize = 1;
            btn.Click += (s, e) => { currentTabFilter = filterTag; UpdateTabControlColors(); ApplyFilters(); };
            tabPanel.Controls.Add(btn);
        }

        private void UpdateTabControlColors()
        {
            foreach (Control ctrl in tabPanel.Controls)
            {
                if (ctrl is Button btn && btn.Tag is string tag)
                {
                    if (tag == currentTabFilter)
                    {
                        btn.FlatAppearance.BorderColor = Color.FromArgb(99, 102, 241);
                        btn.ForeColor = Color.FromArgb(129, 140, 248);
                        btn.BackColor = Color.FromArgb(34, 35, 58);
                    }
                    else
                    {
                        btn.FlatAppearance.BorderColor = Color.FromArgb(50, 50, 65);
                        btn.ForeColor = Color.FromArgb(140, 145, 170);
                        btn.BackColor = Color.FromArgb(26, 26, 35);
                    }
                }
            }
        }
    }
}
