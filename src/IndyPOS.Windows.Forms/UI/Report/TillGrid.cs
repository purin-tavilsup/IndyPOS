namespace IndyPOS.Windows.Forms.UI.Report;

/// <summary>The till's dark, read-only grid, as the designer-built report grids style it, for views built in code.</summary>
public static class TillGrid
{
    private const string FontName = "FC Subject [Non-commercial] Reg";
    private static readonly Color Background = Color.FromArgb(38, 38, 38);
    private static readonly Color HeaderBackground = Color.FromArgb(48, 48, 48);

    public static DataGridView Create(string name)
    {
        var grid = new DataGridView
        {
            Name = name,
            Dock = DockStyle.Fill,
            ReadOnly = true,
            MultiSelect = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Background,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            ColumnHeadersHeight = 50,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            EnableHeadersVisualStyles = false,
            GridColor = Color.DimGray
        };

        grid.ColumnHeadersDefaultCellStyle.BackColor = HeaderBackground;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.Gainsboro;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font(FontName, 14F);
        grid.DefaultCellStyle.BackColor = Background;
        grid.DefaultCellStyle.ForeColor = Color.Gainsboro;
        grid.DefaultCellStyle.Font = new Font(FontName, 12F);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(70, 70, 70);
        grid.DefaultCellStyle.SelectionForeColor = Color.Gainsboro;
        grid.RowTemplate.Height = 35;

        return grid;
    }

    public static void AddColumn(DataGridView grid, string header, int width, string? format = null)
    {
        var column = grid.Columns[grid.Columns.Add(header, header)];
        column.Width = width;
        column.SortMode = DataGridViewColumnSortMode.NotSortable;

        if (format is not null)
            column.DefaultCellStyle.Format = format;
    }
}
