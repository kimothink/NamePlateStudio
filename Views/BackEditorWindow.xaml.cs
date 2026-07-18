using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls.Primitives;
using NamePlateStudio.Helpers;
using Microsoft.Win32;
using NamePlateStudio.Models;

namespace NamePlateStudio.Views;

public partial class BackEditorWindow : Window
{
    private readonly NamePlateEntry target;
    private readonly ObservableCollection<NamePlateEntry> entries;
    private DataTable table = new();

    public bool AppliedToAll { get; private set; }

    public BackEditorWindow(NamePlateEntry target, ObservableCollection<NamePlateEntry> entries)
    {
        InitializeComponent();
        this.target = target;
        this.entries = entries;
        TitleText.Text = $"{target.NameText} - 뒷면 편집";
        BackTextBox.Text = target.BackContent;
        ImagePathTextBox.Text = target.BackImagePath;
        ImageWidthTextBox.Text = target.BackImageWidthMm.ToString("0.##", CultureInfo.CurrentCulture);
        ImageHeightTextBox.Text = target.BackImageHeightMm.ToString("0.##", CultureInfo.CurrentCulture);
        RowsTextBox.Text = target.BackTableRows.ToString(CultureInfo.CurrentCulture);
        ColumnsTextBox.Text = target.BackTableColumns.ToString(CultureInfo.CurrentCulture);
        BuildTable(target.BackTableRows, target.BackTableColumns, target.BackTableCells);
        RefreshImagePreview();
    }

    private void SelectImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "이미지 파일|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|모든 파일|*.*" };
        if (dialog.ShowDialog(this) == true)
        {
            ImagePathTextBox.Text = dialog.FileName;
            RefreshImagePreview();
        }
    }

    private void ClearImage_Click(object sender, RoutedEventArgs e)
    {
        ImagePathTextBox.Clear();
        RefreshImagePreview();
    }

    private void ResizeImageThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        var widthPixels = Math.Max(UnitConverter.MillimetersToPixels(5), ImagePreview.Width + e.HorizontalChange);
        var heightPixels = Math.Max(UnitConverter.MillimetersToPixels(5), ImagePreview.Height + e.VerticalChange);
        SetImageSize(UnitConverter.PixelsToMillimeters(widthPixels), UnitConverter.PixelsToMillimeters(heightPixels));
    }

    private void ImageSizeTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (!IsLoaded) return;
        RefreshImageSize();
    }

    private void RefreshImagePreview()
    {
        ImagePreview.Source = ImageHelper.CreateImageSource(ImagePathTextBox.Text);
        ResizableImageContainer.Visibility = ImagePreview.Source is null ? Visibility.Collapsed : Visibility.Visible;
        RefreshImageSize();
    }

    private void RefreshImageSize()
    {
        if (!double.TryParse(ImageWidthTextBox.Text, out var width) ||
            !double.TryParse(ImageHeightTextBox.Text, out var height) || width <= 0 || height <= 0)
        {
            return;
        }
        ImagePreview.Width = UnitConverter.MillimetersToPixels(width);
        ImagePreview.Height = UnitConverter.MillimetersToPixels(height);
    }

    private void SetImageSize(double widthMm, double heightMm)
    {
        ImageWidthTextBox.Text = Math.Min(150, widthMm).ToString("0.##", CultureInfo.CurrentCulture);
        ImageHeightTextBox.Text = Math.Min(55, heightMm).ToString("0.##", CultureInfo.CurrentCulture);
        RefreshImageSize();
    }

    private void BuildTable_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadTableSize(out var rows, out var columns)) return;
        BuildTable(rows, columns, ReadCells());
    }

    private void ClearTable_Click(object sender, RoutedEventArgs e)
    {
        RowsTextBox.Text = "0";
        ColumnsTextBox.Text = "0";
        BuildTable(0, 0, []);
    }

    private bool TryReadTableSize(out int rows, out int columns)
    {
        rows = 0;
        columns = 0;
        var valid = int.TryParse(RowsTextBox.Text, out rows) && int.TryParse(ColumnsTextBox.Text, out columns)
            && rows is >= 0 and <= 20 && columns is >= 0 and <= 10;
        if (!valid) System.Windows.MessageBox.Show(this, "표의 행은 0~20, 열은 0~10 사이로 입력해주세요.", "뒷면 편집");
        return valid;
    }

    private void BuildTable(int rows, int columns, IReadOnlyList<string> cells)
    {
        table = new DataTable();
        for (var column = 0; column < columns; column++) table.Columns.Add($"열 {column + 1}");
        for (var row = 0; row < rows; row++)
        {
            var dataRow = table.NewRow();
            for (var column = 0; column < columns; column++)
            {
                var index = row * columns + column;
                dataRow[column] = index < cells.Count ? cells[index] : string.Empty;
            }
            table.Rows.Add(dataRow);
        }
        TableEditor.ItemsSource = table.DefaultView;
    }

    private List<string> ReadCells()
    {
        TableEditor.CommitEdit();
        var cells = new List<string>();
        foreach (DataRow row in table.Rows)
            for (var column = 0; column < table.Columns.Count; column++)
                cells.Add(row[column]?.ToString() ?? string.Empty);
        return cells;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!double.TryParse(ImageWidthTextBox.Text, out var width) || width <= 0 ||
            !double.TryParse(ImageHeightTextBox.Text, out var height) || height <= 0)
        {
            System.Windows.MessageBox.Show(this, "그림 너비와 높이는 0보다 큰 숫자로 입력해주세요.", "뒷면 편집");
            return;
        }
        if (!TryReadTableSize(out var rows, out var columns)) return;
        if (rows != table.Rows.Count || columns != table.Columns.Count) BuildTable(rows, columns, ReadCells());
        var cells = ReadCells();
        AppliedToAll = ApplyAllCheckBox.IsChecked == true;
        foreach (var entry in AppliedToAll ? entries : [target]) Apply(entry, width, height, rows, columns, cells);
        DialogResult = true;
    }

    private void Apply(NamePlateEntry entry, double width, double height, int rows, int columns, List<string> cells)
    {
        entry.BackContent = BackTextBox.Text ?? string.Empty;
        entry.BackImagePath = ImagePathTextBox.Text ?? string.Empty;
        entry.BackImageWidthMm = width;
        entry.BackImageHeightMm = height;
        entry.BackTableRows = rows;
        entry.BackTableColumns = columns;
        entry.BackTableCells = cells.ToList();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
