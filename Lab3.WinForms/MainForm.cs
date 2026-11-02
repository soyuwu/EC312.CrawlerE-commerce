using System.Diagnostics;

namespace Lab3.WinForms;

public sealed class MainForm : Form
{
    private readonly ComboBox _mode = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label _inputLabel = new() { AutoSize = true };
    private readonly TextBox _input = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right };
    private readonly Button _browseInput = new() { Text = "Chọn file…" };
    private readonly TextBox _output = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right };
    private readonly Button _browseOutput = new() { Text = "Chọn thư mục…" };
    private readonly Button _start = new() { Text = "Bắt đầu" };
    private readonly Button _cancel = new() { Text = "Dừng", Enabled = false };
    private readonly ProgressBar _progress = new() { Dock = DockStyle.Top, Style = ProgressBarStyle.Marquee };
    private readonly Label _status = new() { AutoSize = true, Text = "Sẵn sàng" };
    private readonly TextBox _log = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
    private CancellationTokenSource? _cancellation;

    public MainForm()
    {
        Text = "Mallow WooCommerce Exporter";
        MinimumSize = new Size(760, 520);
        Size = new Size(900, 650);
        StartPosition = FormStartPosition.CenterScreen;
        _mode.Items.AddRange(["Một sản phẩm", "Một category", "File nhiều category"]);
        _mode.SelectedIndex = 0;
        _output.Text = Path.Combine(AppContext.BaseDirectory, "output");

        var form = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(16), ColumnCount = 3 };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        AddRow(form, 0, new Label { Text = "Chế độ:", AutoSize = true }, _mode, null);
        AddRow(form, 1, _inputLabel, _input, _browseInput);
        AddRow(form, 2, new Label { Text = "Xuất ra:", AutoSize = true }, _output, _browseOutput);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(16, 0, 16, 8) };
        var open = new Button { Text = "Mở thư mục kết quả" };
        actions.Controls.AddRange([_start, _cancel, open]);
        var status = new Panel { Dock = DockStyle.Top, Height = 58, Padding = new Padding(16, 0, 16, 8) };
        status.Controls.Add(_status);
        status.Controls.Add(_progress);
        _status.Top = 26;
        Controls.Add(_log); Controls.Add(status); Controls.Add(actions); Controls.Add(form);

        _mode.SelectedIndexChanged += (_, _) => UpdateMode();
        _browseInput.Click += (_, _) => SelectInputFile();
        _browseOutput.Click += (_, _) => SelectOutputDirectory();
        open.Click += (_, _) => OpenOutputDirectory();
        _start.Click += async (_, _) => await StartAsync();
        _cancel.Click += (_, _) => _cancellation?.Cancel();
        UpdateMode();
    }

    private static void AddRow(TableLayoutPanel panel, int row, Label label, Control input, Control? action)
    {
        label.Anchor = AnchorStyles.Left;
        panel.Controls.Add(label, 0, row);
        panel.Controls.Add(input, 1, row);
        if (action is not null) panel.Controls.Add(action, 2, row); else panel.SetColumnSpan(input, 2);
    }

    private void UpdateMode()
    {
        var fileMode = _mode.SelectedIndex == 2;
        _inputLabel.Text = fileMode ? "Danh sách:" : "URL:";
        _input.PlaceholderText = fileMode ? "Tên category|https://mallow.vn/category.html" : "https://mallow.vn/...";
        _browseInput.Visible = fileMode;
    }

    private async Task StartAsync()
    {
        if (string.IsNullOrWhiteSpace(_input.Text) || string.IsNullOrWhiteSpace(_output.Text))
        {
            MessageBox.Show(this, "Hãy nhập URL hoặc chọn file, và chọn thư mục xuất.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        SetRunning(true); _log.Clear(); _cancellation = new CancellationTokenSource();
        try
        {
            var progress = new Progress<CrawlProgress>(UpdateProgress);
            var service = new MallowCrawlerService();
            var summary = _mode.SelectedIndex switch
            {
                0 => await service.CrawlProductAsync(_input.Text.Trim(), _output.Text.Trim(), progress, _cancellation.Token),
                1 => await service.CrawlCategoryAsync(_input.Text.Trim(), _output.Text.Trim(), progress, _cancellation.Token),
                _ => await service.CrawlCategoriesAsync(_input.Text.Trim(), _output.Text.Trim(), progress, _cancellation.Token)
            };
            AppendLog($"Hoàn tất: {summary.ParentProducts} sản phẩm cha, {summary.Variations} biến thể.");
            AppendLog($"CSV: {summary.OutputPath}"); _status.Text = "Hoàn tất";
            MessageBox.Show(this, "Xuất dữ liệu thành công.", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException) { _status.Text = "Đã hủy"; AppendLog("Người dùng đã hủy tác vụ."); }
        catch (Exception ex) { _status.Text = "Có lỗi"; AppendLog($"Lỗi: {ex.Message}"); MessageBox.Show(this, ex.Message, "Không thể crawl", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { _cancellation.Dispose(); _cancellation = null; SetRunning(false); }
    }

    private void UpdateProgress(CrawlProgress update)
    {
        _status.Text = update.Message; AppendLog(update.Message);
        if (update.Total > 0) { _progress.Style = ProgressBarStyle.Continuous; _progress.Value = Math.Clamp((int)(update.Current * 100d / update.Total), 0, 100); }
        else _progress.Style = ProgressBarStyle.Marquee;
    }

    private void SetRunning(bool running)
    {
        _start.Enabled = !running; _cancel.Enabled = running; _mode.Enabled = !running; _input.Enabled = !running; _output.Enabled = !running; _browseInput.Enabled = !running; _browseOutput.Enabled = !running;
        if (running) { _progress.Style = ProgressBarStyle.Marquee; _status.Text = "Đang khởi chạy…"; }
    }

    private void SelectInputFile()
    {
        using var dialog = new OpenFileDialog { Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*" };
        if (dialog.ShowDialog(this) == DialogResult.OK) _input.Text = dialog.FileName;
    }
    private void SelectOutputDirectory()
    {
        using var dialog = new FolderBrowserDialog { InitialDirectory = Directory.Exists(_output.Text) ? _output.Text : AppContext.BaseDirectory };
        if (dialog.ShowDialog(this) == DialogResult.OK) _output.Text = dialog.SelectedPath;
    }
    private void OpenOutputDirectory()
    {
        Directory.CreateDirectory(_output.Text);
        Process.Start(new ProcessStartInfo { FileName = _output.Text, UseShellExecute = true });
    }
    private void AppendLog(string message) => _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
}
