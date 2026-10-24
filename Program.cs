using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Mudhakkir;

internal static class Program
{
    [STAThread]
    static void Main() { ApplicationConfiguration.Initialize(); Application.Run(new ReminderForm()); }
}
// لا تنسونا من خير دعائكم
internal sealed class ReminderForm : Form
{
    static readonly Color Ink = Color.FromArgb(39, 52, 45), Green = Color.FromArgb(67, 103, 79),
        GreenLight = Color.FromArgb(229, 237, 226), Paper = Color.FromArgb(247, 246, 240),
        Card = Color.FromArgb(255, 254, 250), Muted = Color.FromArgb(112, 120, 111), Border = Color.FromArgb(224, 224, 215);
    const string RLM = "\u200F";
    readonly NotifyIcon tray;
    readonly System.Windows.Forms.Timer reminderTimer = new(), countdownTimer = new() { Interval = 1000 };
    readonly ModernSlider intervalSlider, popupDurationSlider;
    readonly Label intervalValue, popupDurationValue, status, nextReminder;
    readonly Button messagePicker, startButton, stopButton, hideButton, saveCustomButton;
    readonly Panel customPanel, popupDurationPanel;
    readonly TextBox customText;
    readonly RadioButton popupMode, balloonMode;
    readonly ContextMenuStrip messageMenu = new();
    readonly List<string> savedCustomMessages = new();
    readonly string[] messageTitles = { "سيد الاستغفار", "لا إله إلا الله وحده", "الاستغفار", "الصلاة على النبي" };
    readonly string[] messages =
    {
        "اللَّهُمَّ أنْتَ رَبِّي لا إلَهَ إلَّا أنْتَ، خَلَقْتَنِي وأَنَا عَبْدُكَ، وأَنَا علَى عَهْدِكَ ووَعْدِكَ ما اسْتَطَعْتُ، أعُوذُ بكَ مِن شَرِّ ما صَنَعْتُ، أبُوءُ لكَ بنِعْمَتِكَ عَلَيَّ، وأَبُوءُ بذَنْبِي فاغْفِرْ لي، فإنَّه لا يَغْفِرُ الذُّنُوبَ إلَّا أنْتَ",
        "لَا إِلَهَ إِلَّا اللهُ وَحْدَهُ لَا شَرِيكَ لَهُ، لَهُ الْمُلْكُ وَلَهُ الْحَمْدُ، وَهُوَ عَلَى كُلِّ شَيْءٍ قَدِيرٌ",
        "أَسْتَغْفِرُ اللَّهَ الَّذِي لَا إِلَهَ إِلَّا هُوَ الْحَيَّ الْقَيُّومَ وَأَتُوبُ إِلَيْهِ",
        "اللَّهُمَّ صَلِّ وَسَلِّمْ وَبَارِكْ عَلَى نَبِيِّنَا مُحَمَّدٍ"
    };
    string selectedMessage;
    bool customMode, shuffleMode, allowClose;
    int lastMessage = -1, secondsRemaining;
    readonly Random random = new();
    PopupForm? activePopup;

    public ReminderForm()
    {
        selectedMessage = messages[0];
        Text = "Mudhakkir | مُذَكِّر";
        StartPosition = FormStartPosition.CenterScreen; Size = new Size(720, 820); MinimumSize = new Size(680, 760);
        BackColor = Paper; ForeColor = Ink; Font = new Font("Segoe UI", 10); RightToLeft = RightToLeft.Yes; RightToLeftLayout = false;
        FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false; DoubleBuffered = true;

        tray = new NotifyIcon { Icon = SystemIcons.Information, Text = "Mudhakkir - مُذَكِّر", Visible = true };
        tray.DoubleClick += (_, _) => RestoreWindow();
        var trayMenu = new ContextMenuStrip { RightToLeft = RightToLeft.Yes, Font = new Font("Segoe UI", 10) };
        trayMenu.Items.Add("فتح الإعدادات", null, (_, _) => RestoreWindow());
        trayMenu.Items.Add("بدء التذكيرات", null, (_, _) => StartReminders());
        trayMenu.Items.Add("إيقاف التذكيرات", null, (_, _) => StopReminders());
        trayMenu.Items.Add(new ToolStripSeparator()); trayMenu.Items.Add("خروج", null, (_, _) => ExitApplication()); tray.ContextMenuStrip = trayMenu;
        reminderTimer.Tick += (_, _) => ShowReminder();
        countdownTimer.Tick += (_, _) => { if (secondsRemaining > 0) secondsRemaining--; UpdateStatus(); };

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(22), BackColor = Paper, RightToLeft = RightToLeft.No };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 220)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 125)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 200)); Controls.Add(root);

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Paper, RightToLeft = RightToLeft.No };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 288));
        var brand = new Panel { Dock = DockStyle.Fill, BackColor = Paper, RightToLeft = RightToLeft.No };
        brand.Controls.Add(new Label { Text = "❧", Font = new Font("Segoe UI Symbol", 27), ForeColor = Green, AutoSize = true, Location = new Point(0, 2) });
        brand.Controls.Add(new Label { Text = RLM + "مُذَكِّر" + RLM, Font = new Font("Segoe UI", 23, FontStyle.Bold), ForeColor = Ink, AutoSize = true, Location = new Point(72, 0), RightToLeft = RightToLeft.Yes });
        brand.Controls.Add(new Label { Text = RLM + "انْشَغِلْ بِعَمَلِكَ، وَلَا تَنْسَ رَبَّك" + RLM, Font = new Font("Segoe UI", 9), ForeColor = Muted, AutoSize = false, Size = new Size(230, 24), Location = new Point(72, 57), TextAlign = ContentAlignment.MiddleRight, RightToLeft = RightToLeft.Yes });
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(0, 10, 0, 0), BackColor = Paper };
        startButton = Button("▶ بدء", Green, Color.White, 82); stopButton = Button("Ⅱ إيقاف", Color.FromArgb(233, 233, 224), Ink, 88); hideButton = Button("— إخفاء", Color.FromArgb(233, 233, 224), Ink, 88);
        startButton.Click += StartButton_Click; stopButton.Click += StopButton_Click; hideButton.Click += (_, _) => HideToTray(); actions.Controls.AddRange(new Control[] { hideButton, stopButton, startButton });
        header.Controls.Add(brand, 0, 0); header.Controls.Add(actions, 1, 0); root.Controls.Add(header, 0, 0);

        var intro = CardPanel(); intro.Controls.Add(Section("الَّذِينَ آمَنُوا وَتَطْمَئِنُّ قُلُوبُهُم بِذِكْرِ اللَّهِ ۗ أَلَا بِذِكْرِ اللَّهِ تَطْمَئِنُّ الْقُلُوبُ     ", 18, 8));
        intro.Controls.Add(new Label { Text = RLM + "                   اختر اذكارا ترافقك خلال يومك، ودع مذكّر يتولى الباقي                                                  " + RLM, AutoSize = false, Dock = DockStyle.Bottom, Height = 42, ForeColor = Muted, TextAlign = ContentAlignment.MiddleRight, RightToLeft = RightToLeft.Yes, Padding = new Padding(0, 0, 4, 0) }); root.Controls.Add(intro, 0, 1);

        var messageCard = CardPanel(); messageCard.Controls.Add(Section("الذِّكْر                                                                                              ", 18, 15));
        messagePicker = Button(messages[0] + "     ▾", Color.FromArgb(244, 244, 237), Ink, 650);
        messagePicker.Location = new Point(18, 48); messagePicker.Height = 46; messagePicker.TextAlign = ContentAlignment.MiddleRight; messagePicker.FlatAppearance.BorderSize = 1; messagePicker.FlatAppearance.BorderColor = Border; messagePicker.Font = new Font("Segoe UI", 10);
        messagePicker.Click += (_, _) => messageMenu.Show(messagePicker, new Point(0, messagePicker.Height));
        messageMenu.RightToLeft = RightToLeft.Yes; messageMenu.Font = new Font("Segoe UI", 10); messageMenu.Renderer = new ToolStripProfessionalRenderer();
        BuildMessageMenu();
        customPanel = new Panel { Location = new Point(18, 103), Size = new Size(650, 105), BackColor = Card, Visible = false };
        customPanel.Controls.Add(new Label { Text = "اكتب ذكرك", AutoSize = true, Location = new Point(0, 0), ForeColor = Muted, RightToLeft = RightToLeft.Yes });
        customText = new TextBox { Multiline = true, Location = new Point(0, 25), Size = new Size(540, 64), Font = new Font("Segoe UI", 10), BorderStyle = BorderStyle.FixedSingle, RightToLeft = RightToLeft.Yes, TextAlign = HorizontalAlignment.Right, Text = "اكتب ذكرك هنا." };
        saveCustomButton = Button("حفظ", Green, Color.White, 92); saveCustomButton.Location = new Point(550, 45); saveCustomButton.Height = 34; saveCustomButton.Click += (_, _) => SaveCustomMessage();
        customText.TextChanged += (_, _) => { if (customMode) UpdatePickerText(); }; customPanel.Controls.AddRange(new Control[] { customText, saveCustomButton }); messageCard.Controls.AddRange(new Control[] { messagePicker, customPanel }); root.Controls.Add(messageCard, 0, 2);

        var intervalCard = CardPanel(); intervalCard.Controls.Add(Section("الفترة بين الاذكار                                                                             ", 18, 13));
        intervalValue = new Label { Text = RLM + "20 دقيقة" + RLM, AutoSize = false, Width = 130, Height = 34, Location = new Point(18, 48), ForeColor = Green, Font = new Font("Segoe UI", 12, FontStyle.Bold), TextAlign = ContentAlignment.MiddleRight, RightToLeft = RightToLeft.Yes };
        intervalSlider = new ModernSlider { Minimum = 1, Maximum = 60, Value = 20, Location = new Point(168, 47), Size = new Size(470, 30), Anchor = AnchorStyles.Left | AnchorStyles.Top };
        intervalSlider.ValueChanged += (_, _) => { intervalValue.Text = RLM + $"{intervalSlider.Value} دقيقة" + RLM; if (reminderTimer.Enabled) StartReminders(); };
        intervalCard.Controls.AddRange(new Control[] { intervalValue, intervalSlider }); root.Controls.Add(intervalCard, 0, 3);

        var options = CardPanel(); options.Controls.Add(Section("طريقة ظهور الذكر                                                                          ", 18, 14));
        popupMode = new RadioButton { Text = "نافذة منبثقة متلاشية", AutoSize = true, Location = new Point(20, 52), Checked = true, RightToLeft = RightToLeft.Yes, ForeColor = Ink };
        balloonMode = new RadioButton { Text = "إشعار منطقة النظام", AutoSize = true, Location = new Point(330, 52), RightToLeft = RightToLeft.Yes, ForeColor = Ink };
        popupDurationPanel = new Panel { Location = new Point(18, 88), Size = new Size(650, 54), BackColor = GreenLight };
        popupDurationValue = new Label { Text = "مدة الظهور: 5 ثوانٍ", AutoSize = false, Width = 145, Height = 30, Location = new Point(8, 10), ForeColor = Green, Font = new Font("Segoe UI", 10, FontStyle.Bold), TextAlign = ContentAlignment.MiddleRight, RightToLeft = RightToLeft.Yes };
        popupDurationSlider = new ModernSlider { Minimum = 2, Maximum = 30, Value = 5, Location = new Point(155, 12), Size = new Size(480, 30) };
        popupDurationSlider.ValueChanged += (_, _) => popupDurationValue.Text = RLM + $"مدة الظهور: {popupDurationSlider.Value} ثوانٍ" + RLM;
        popupMode.CheckedChanged += (_, _) => popupDurationPanel.Visible = popupMode.Checked; balloonMode.CheckedChanged += (_, _) => popupDurationPanel.Visible = popupMode.Checked;
        popupDurationPanel.Controls.AddRange(new Control[] { popupDurationValue, popupDurationSlider });
        status = new Label { Text = "الحالة: متوقف", AutoSize = false, Width = 150, Height = 30, Location = new Point(20, 150), ForeColor = Green, Font = new Font("Segoe UI", 9, FontStyle.Bold), TextAlign = ContentAlignment.MiddleRight, RightToLeft = RightToLeft.Yes };
        nextReminder = new Label { Text = "ابدأ لتفعيل التذكيرات.", AutoSize = false, Width = 420, Height = 30, Location = new Point(185, 150), ForeColor = Muted, TextAlign = ContentAlignment.MiddleRight, RightToLeft = RightToLeft.Yes };
        options.Controls.AddRange(new Control[] { popupMode, balloonMode, popupDurationPanel, status, nextReminder }); root.Controls.Add(options, 0, 4);
        UpdatePickerText(); UpdateStatus(); UpdateButtonState(); FormClosing += OnFormClosing;
    }

    Panel CardPanel()
    {
        var p = new Panel { Dock = DockStyle.Fill, BackColor = Card, Padding = new Padding(16), Margin = new Padding(0, 5, 0, 5), RightToLeft = RightToLeft.No };
        p.Paint += (_, e) => { e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using var pen = new Pen(Border); var r = p.ClientRectangle; r.Width--; r.Height--; e.Graphics.DrawRoundedRectangle(pen, r, 13); };
        return p;
    }
    void BuildMessageMenu()
    {
        messageMenu.Items.Clear();
        for (var i = 0; i < messages.Length; i++)
        {
            var index = i;
            var item = new ToolStripMenuItem(messageTitles[i]) { Padding = new Padding(12, 7, 12, 7), RightToLeft = RightToLeft.Yes };
            item.Click += (_, _) => SelectPreset(messages[index]);
            messageMenu.Items.Add(item);
        }
        foreach (var saved in savedCustomMessages)
        {
            var row = new SavedMessageRow(saved);
            row.Selected += (_, _) => SelectPreset(saved);
            row.Deleted += (_, _) => { savedCustomMessages.Remove(saved); BuildMessageMenu(); };
            messageMenu.Items.Add(new ToolStripControlHost(row) { AutoSize = false, Size = new Size(640, 38), Padding = Padding.Empty });
        }
        messageMenu.Items.Add(new ToolStripSeparator());
        var customItem = new ToolStripMenuItem("ذِّكْر مخصص…") { Padding = new Padding(12, 7, 12, 7) };
        customItem.Click += (_, _) => SelectCustom();
        var shuffleItem = new ToolStripMenuItem("خلط الأذكار عشوائياً") { Padding = new Padding(12, 7, 12, 7) };
        shuffleItem.Click += (_, _) => SelectShuffle();
        messageMenu.Items.Add(customItem);
        messageMenu.Items.Add(shuffleItem);
    }
    void SaveCustomMessage()
    {
        var text = customText.Text.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;
        if (!savedCustomMessages.Contains(text)) savedCustomMessages.Add(text);
        selectedMessage = text;
        customMode = false;
        customPanel.Visible = false;
        BuildMessageMenu();
        UpdatePickerText();
    }
    Label Section(string text, int x, int y) => new() { Text = RLM + text + RLM, AutoSize = false, Width = 650, Height = 38, Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Ink, Location = new Point(x, y), TextAlign = ContentAlignment.MiddleRight, RightToLeft = RightToLeft.Yes };
    Button Button(string text, Color back, Color fore, int width)
    {
        var b = new Button { Text = RLM + text + RLM, Width = width, Height = 38, Margin = new Padding(4, 0, 4, 0), FlatStyle = FlatStyle.Flat, BackColor = back, ForeColor = fore, Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand, RightToLeft = RightToLeft.Yes, UseVisualStyleBackColor = false };
        b.FlatAppearance.BorderSize = 0; b.FlatAppearance.MouseOverBackColor = Color.FromArgb(Math.Min(255, back.R + 8), Math.Min(255, back.G + 8), Math.Min(255, back.B + 8));
        b.Region = new Region(RoundedRectangle(new Rectangle(0, 0, b.Width, b.Height), 10));
        return b;
    }
    static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath(); var diameter = radius * 2;
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure(); return path;
    }
    void SelectPreset(string msg) { customMode = shuffleMode = false; selectedMessage = msg; customPanel.Visible = false; UpdatePickerText(); }
    void SelectCustom() { customMode = true; shuffleMode = false; customPanel.Visible = true; customPanel.BringToFront(); UpdatePickerText(); }
    void SelectShuffle() { customMode = false; shuffleMode = true; customPanel.Visible = false; UpdatePickerText(); }
    void UpdatePickerText()
    {
        var text = shuffleMode ? "خلط الأذكار عشوائياً — ذكر مختلف في كل مرة" : customMode
            ? (string.IsNullOrWhiteSpace(customText.Text) ? "اكتب ذكرك المخصص" : customText.Text.Trim().Replace("\r", " ").Replace("\n", " "))
            : selectedMessage;
        if (text.Length > 58) text = text[..58] + "…";
        messagePicker.Text = RLM + text + "     ▾" + RLM;
    }
    void StartButton_Click(object? sender, EventArgs e) => StartReminders();
    void StopButton_Click(object? sender, EventArgs e) => StopReminders();
    void UpdateButtonState()
    {
        var running = reminderTimer.Enabled;
        startButton.Enabled = !running;
        stopButton.Enabled = running;
        startButton.BackColor = running ? Color.FromArgb(205, 205, 198) : Green;
        startButton.ForeColor = running ? Color.FromArgb(120, 120, 112) : Color.White;
        stopButton.BackColor = running ? Color.FromArgb(190, 70, 65) : Color.FromArgb(233, 233, 224);
        stopButton.ForeColor = running ? Color.White : Ink;
    }
    void StartReminders()
    {
        var minutes = Math.Max(1, intervalSlider.Value);
        reminderTimer.Stop();
        reminderTimer.Interval = minutes * 60000;
        secondsRemaining = minutes * 60;
        reminderTimer.Start();
        countdownTimer.Start();
        UpdateStatus();
        UpdateButtonState();
    }
    void StopReminders() { reminderTimer.Stop(); countdownTimer.Stop(); secondsRemaining = 0; UpdateStatus(); UpdateButtonState(); }
    string NextMessage() { if (customMode) return string.IsNullOrWhiteSpace(customText.Text) ?  " " : customText.Text.Trim(); if (!shuffleMode) return selectedMessage; int i; do { i = random.Next(messages.Length); } while (messages.Length > 1 && i == lastMessage); lastMessage = i; return messages[i]; }
    void ShowReminder() { secondsRemaining = intervalSlider.Value * 60; string msg = NextMessage(); if (balloonMode.Checked) { tray.BalloonTipTitle = RLM + "مُذَكِّر" + RLM; tray.BalloonTipText = RLM + msg + RLM; tray.BalloonTipIcon = ToolTipIcon.Info; tray.ShowBalloonTip(5000); } else { activePopup?.Close(); activePopup = new PopupForm(msg, Green, Paper, Ink, popupDurationSlider.Value); activePopup.Show(); } UpdateStatus(); }
    void UpdateStatus() { status.Text = RLM + (reminderTimer.Enabled ? "الحالة: يعمل" : "الحالة: متوقف") + RLM; nextReminder.Text = RLM + (reminderTimer.Enabled ? $"التذكير التالي خلال {Math.Max(0, secondsRemaining) / 60:D2}:{Math.Max(0, secondsRemaining) % 60:D2}" : "ابدأ لتفعيل التذكيرات.") + RLM; UpdateButtonState(); }
    void HideToTray() { Hide(); ShowInTaskbar = false; tray.ShowBalloonTip(1500, RLM + "مُذَكِّر" + RLM, RLM + "التطبيق يعمل في الخلفية. افتح الإعدادات من أيقونة منطقة النظام." + RLM, ToolTipIcon.Info); }
    void RestoreWindow() { Show(); ShowInTaskbar = true; WindowState = FormWindowState.Normal; Activate(); }
    void ExitApplication() { allowClose = true; reminderTimer.Stop(); countdownTimer.Stop(); tray.Visible = false; tray.Dispose(); Application.Exit(); }
    void OnFormClosing(object? sender, FormClosingEventArgs e) { if (!allowClose && e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; HideToTray(); } }
}

internal sealed class SavedMessageRow : UserControl
{
    readonly Label textLabel;
    public event EventHandler? Selected;
    public event EventHandler? Deleted;

    public SavedMessageRow(string text)
    {
        Height = 36;
        Width = 620;
        BackColor = Color.White;
        RightToLeft = RightToLeft.Yes;
        textLabel = new Label
        {
            Text = text.Length > 72 ? text[..72] + "…" : text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            RightToLeft = RightToLeft.Yes,
            Padding = new Padding(8, 0, 8, 0),
            Cursor = Cursors.Hand
        };
        var delete = new Button
        {
            Text = "×",
            Dock = DockStyle.Left,
            Width = 34,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.Firebrick,
            BackColor = Color.White,
            Cursor = Cursors.Hand
        };
        delete.FlatAppearance.BorderSize = 0;
        textLabel.Click += (_, _) => Selected?.Invoke(this, EventArgs.Empty);
        Click += (_, _) => Selected?.Invoke(this, EventArgs.Empty);
        delete.Click += (_, _) => Deleted?.Invoke(this, EventArgs.Empty);
        Controls.Add(textLabel);
        Controls.Add(delete);
    }
}

internal sealed class ModernSlider : Control
{
    int value;
    public int Minimum { get; set; } = 0;
    public int Maximum { get; set; } = 100;
    public int Value
    {
        get => value;
        set
        {
            var next = Math.Clamp(value, Minimum, Maximum);
            if (this.value == next) return;
            this.value = next;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public event EventHandler? ValueChanged;

    public ModernSlider()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        TabStop = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var track = new Rectangle(8, (Height - 6) / 2, Math.Max(1, Width - 16), 6);
        using var trackBrush = new SolidBrush(Color.FromArgb(218, 224, 216));
        using var fillBrush = new SolidBrush(Color.FromArgb(67, 103, 79));
        e.Graphics.FillRoundedRectangle(trackBrush, track, 3);
        var ratio = Maximum == Minimum ? 0 : (Value - Minimum) / (float)(Maximum - Minimum);
        var position = track.X + (int)(track.Width * ratio);
        e.Graphics.FillRoundedRectangle(fillBrush, new Rectangle(track.X, track.Y, Math.Max(1, position - track.X), track.Height), 3);
        using var knobBrush = new SolidBrush(Color.FromArgb(38, 132, 211));
        e.Graphics.FillRoundedRectangle(knobBrush, new Rectangle(position - 7, (Height - 22) / 2, 14, 22), 5);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left) { Capture = true; SetValueFromMouse(e.X); }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (Capture && e.Button == MouseButtons.Left) SetValueFromMouse(e.X);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        Capture = false;
    }

    void SetValueFromMouse(int x)
    {
        var usableWidth = Math.Max(1, Width - 16);
        Value = Minimum + (int)Math.Round(Math.Clamp(x - 8, 0, usableWidth) / (double)usableWidth * (Maximum - Minimum));
    }
}

internal sealed class PopupForm : Form
{
    readonly System.Windows.Forms.Timer animation = new() { Interval = 25 };
    readonly int visibleTicks; bool fading; int held;
    public PopupForm(string message, Color green, Color paper, Color ink, int seconds)
    {
        visibleTicks = seconds * 40; FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; TopMost = true; StartPosition = FormStartPosition.Manual; Size = new Size(390, 150); BackColor = paper; Opacity = 0; RightToLeft = RightToLeft.Yes; RightToLeftLayout = true;
        var screen = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 800); Location = new Point(screen.Right - Width - 24, screen.Bottom - Height - 24);
        var stripe = new Panel { Dock = DockStyle.Right, Width = 6, BackColor = green };
        var title = new Label { Text = "مُذَكِّر  ❧", AutoSize = false, Height = 42, Dock = DockStyle.Top, Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = green, TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(12, 0, 16, 0), RightToLeft = RightToLeft.Yes };
        var body = new Label { Text = "\u200F" + message + "\u200F", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11), ForeColor = ink, TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(16, 4, 18, 10), RightToLeft = RightToLeft.Yes };
        var close = new Button { Text = "×", Width = 34, Height = 32, Dock = DockStyle.Left, FlatStyle = FlatStyle.Flat, BackColor = paper, ForeColor = Color.Gray }; close.FlatAppearance.BorderSize = 0; close.Click += (_, _) => fading = true;
        var top = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = paper }; top.Controls.Add(title); top.Controls.Add(close); Controls.Add(body); Controls.Add(top); Controls.Add(stripe);
        Paint += (_, e) => { using var pen = new Pen(Color.FromArgb(215, 212, 201)); e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1); };
        animation.Tick += (_, _) => { if (fading) { Opacity = Math.Max(0, Opacity - .08); if (Opacity <= 0) { animation.Stop(); Close(); } } else if (Opacity < .88) Opacity = Math.Min(.88, Opacity + .12); else if (++held >= visibleTicks) fading = true; };
        FormClosed += (_, _) => animation.Dispose(); Shown += (_, _) => animation.Start();
    }
}

internal static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics g, Brush brush, Rectangle r, int radius)
    {
        using var path = RoundedPath(r, radius);
        g.FillPath(brush, path);
    }

    public static void DrawRoundedRectangle(this Graphics g, Pen pen, Rectangle r, int radius)
    {
        using var path = RoundedPath(r, radius);
        g.DrawPath(pen, path);
    }

    static GraphicsPath RoundedPath(Rectangle r, int radius)
    {
        int d = radius * 2; var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90); path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure(); return path;
    }
}
