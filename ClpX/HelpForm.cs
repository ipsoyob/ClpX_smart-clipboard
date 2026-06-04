using System;
using System.Drawing;
using System.Security.Cryptography.Xml;
using System.Windows.Forms;

public class HelpForm : Form
{
    private Label btnVisualAltX;
    private Label btnVisualAltA;
    private Label btnVisualF1;
    private Label btnVisualDelete;
    private Label btnVisualP;
    private bool isDraggingHelp = false; // Зажата ли мышка прямо сейчас
    private Point startMousePoint = new Point(0, 0);

    public HelpForm()
    {
        this.Text = LanguageManager.GetString("reference");
        this.Size = new Size(530, 530); 
        this.StartPosition = FormStartPosition.CenterParent;


        this.FormBorderStyle = FormBorderStyle.None;

        this.BackColor = Color.FromArgb(24, 24, 35);
        this.KeyPreview = true;

        BuildPremiumInterface();
    }

    private void BuildPremiumInterface()
    {
        Label lblHeader = new Label
        {
            Text = LanguageManager.GetString("lblHeader"),
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.FromArgb(170, 175, 190),
            Location = new Point(25, 25),
            Size = new Size(480, 30),
            TextAlign = ContentAlignment.MiddleLeft
        };
        this.Controls.Add(lblHeader);

        Label lblHotkeysTitle = new Label
        {
            Text = LanguageManager.GetString("lblSecHotkeys"),
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(25, 75),
            Size = new Size(480, 25),
            TextAlign = ContentAlignment.MiddleLeft
        };
        this.Controls.Add(lblHotkeysTitle);

        btnVisualAltX = CreateKeyRow("Alt + X", LanguageManager.GetString("hk_QuickShow"), 110);
        btnVisualAltA = CreateKeyRow("Alt + A", LanguageManager.GetString("hk_Copyright"), 145);
        btnVisualF1 = CreateKeyRow("F1", LanguageManager.GetString("hk_Help"), 180);
        btnVisualDelete = CreateKeyRow("Delete", LanguageManager.GetString("hk_Delete"), 215);
        btnVisualP = CreateKeyRow("P", LanguageManager.GetString("hk_Preview"), 250);

        Label signature = new Label
        {
            Text = LanguageManager.GetString("signatureLbl"),
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.White,
            Location = new Point(40, 290),
            Size = new Size(500, 25), 
            TextAlign = ContentAlignment.MiddleLeft
        };
        this.Controls.Add(signature);

        Label lblUxTitle = new Label
        {
            Text = LanguageManager.GetString("lblSecUi"),
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(25, 330),
            Size = new Size(480, 25),
            TextAlign = ContentAlignment.MiddleLeft
        };
        this.Controls.Add(lblUxTitle);

        CreateUxRow(LanguageManager.GetString("ui_Tabs"), 365);
        CreateUxRow(LanguageManager.GetString("ui_RightClick"), 395);
        CreateUxRow(LanguageManager.GetString("ui_DoubleClick"), 425);

        Button btnClose = new Button
        {
            Text = LanguageManager.GetString("successF1"),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(24, 24, 35),
            BackColor = Color.FromArgb(0, 220, 110),
            FlatStyle = FlatStyle.Flat,
            Location = new Point(365, 455),
            Size = new Size(130, 40),
            Cursor = Cursors.Hand
        };
        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.Click += (s, e) => this.Close();
        this.Controls.Add(btnClose);

        // 1. Поймали момент нажатия мыши на форму
        this.MouseDown += (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                isDraggingHelp = true;
                startMousePoint = new Point(e.X, e.Y); // Запоминаем, где именно на форме кликнули
            }
        };

        // 2. Двигаем мышь — перемещаем окно справки
        this.MouseMove += (s, e) =>
        {
            if (isDraggingHelp)
            {
                // Высчитываем новые координаты окна относительно экрана
                Point mousePos = Control.MousePosition;
                this.Location = new Point(mousePos.X - startMousePoint.X, mousePos.Y - startMousePoint.Y);
            }
        };

        // 3. Отпустили мышь — останавливаем перетаскивание
        this.MouseUp += (s, e) =>
        {
            isDraggingHelp = false;
        };

    }

    private Label CreateKeyRow(string keyText, string descText, int topY)
    {
        Label keyLabel = new Label
        {
            Text = keyText,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(35, 35, 48),
            Location = new Point(35, topY),
            Size = new Size(100, 25),
            TextAlign = ContentAlignment.MiddleCenter
        };

        Label descLabel = new Label
        {
            Text = descText,
            Font = new Font("Segoe UI", 9, FontStyle.Regular),
            ForeColor = Color.FromArgb(150, 155, 170),
            Location = new Point(155, topY),
            Size = new Size(340, 25),
            TextAlign = ContentAlignment.MiddleLeft
        };

        this.Controls.Add(keyLabel);
        this.Controls.Add(descLabel);
        return keyLabel;
    }

    private void CreateUxRow(string text, int topY)
    {
        Label uxLabel = new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 9, FontStyle.Regular),
            ForeColor = Color.FromArgb(170, 175, 190),
            Location = new Point(35, topY),
            Size = new Size(460, 22),
            TextAlign = ContentAlignment.MiddleLeft
        };
        this.Controls.Add(uxLabel);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // Очищаем keyData от модификаторов, чтобы получить чистую клавишу (X, A, P и т.д.)
        Keys keyCode = keyData & Keys.KeyCode;
        // Проверяем, зажат ли Alt в этот момент
        bool isAltPressed = (keyData & Keys.Alt) == Keys.Alt;

        if (isAltPressed && keyCode == Keys.X)
        {
            FlashKey(btnVisualAltX);
            return true; // Глушим, чтобы не сворачивало главное окно
        }

        if (isAltPressed && keyCode == Keys.A)
        {
            FlashKey(btnVisualAltA);
            return true; // Тоже глушим на всякий случай
        }

        // Для обычных клавиш без Alt оставляем старую простую проверку
        if (keyCode == Keys.F1) { FlashKey(btnVisualF1); return true; }
        if (keyCode == Keys.Delete) { FlashKey(btnVisualDelete); }
        if (keyCode == Keys.P) { FlashKey(btnVisualP); }
        if (keyCode == Keys.Escape) { this.Close(); return true; }

        return base.ProcessCmdKey(ref msg, keyData);
    }


    private void FlashKey(Label targetLabel)
    {
        if (targetLabel == null) return;
        targetLabel.BackColor = Color.FromArgb(0, 220, 110);
        targetLabel.ForeColor = Color.FromArgb(24, 24, 35);

        System.Windows.Forms.Timer t = new System.Windows.Forms.Timer { Interval = 250 };
        t.Tick += (s, e) =>
        {
            t.Stop();
            targetLabel.BackColor = Color.FromArgb(35, 35, 48);
            targetLabel.ForeColor = Color.White;
            t.Dispose();
        };
        t.Start();
    }
}
