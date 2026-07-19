using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Extensions;
using IndyPOS.Application.UseCases.StoreHub.PaymentMethods;
using IndyPOS.Infrastructure.Services.StoreHub;
using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Windows.Forms.UI.Setting;

[ExcludeFromCodeCoverage]
public partial class PaymentMethodsSettingsForm : Form
{
    private readonly IStoreHubClient _storeHubClient;
    private readonly MessageForm _messageForm;
    private bool _isPopulatingGrid;

    private enum PaymentMethodColumn
    {
        Code,
        DisplayName,
        Kind,
        Enabled,
        DisplayOrder,
        Save
    }

    public PaymentMethodsSettingsForm(IStoreHubClient storeHubClient, MessageForm messageForm)
    {
        _storeHubClient = storeHubClient;
        _messageForm = messageForm;

        InitializeComponent();
        InitializeGridColumns();
    }

    private void InitializeGridColumns()
    {
        PaymentMethodsGrid.Columns.Clear();

        PaymentMethodsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "CodeColumn",
            HeaderText = "รหัส",
            ReadOnly = true,
            Width = 160
        });

        PaymentMethodsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "DisplayNameColumn",
            HeaderText = "ชื่อที่แสดง",
            ReadOnly = false,
            Width = 300
        });

        PaymentMethodsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "KindColumn",
            HeaderText = "ประเภท",
            ReadOnly = true,
            Width = 160
        });

        PaymentMethodsGrid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            Name = "EnabledColumn",
            HeaderText = "เปิดใช้งาน",
            Width = 90
        });

        PaymentMethodsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "DisplayOrderColumn",
            HeaderText = "ลำดับการแสดงผล",
            ReadOnly = false,
            Width = 140
        });

        PaymentMethodsGrid.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "SaveColumn",
            HeaderText = string.Empty,
            Text = "บันทึก",
            UseColumnTextForButtonValue = true,
            Width = 90
        });
    }

    public new async Task ShowDialog()
    {
        var loaded = await LoadPaymentMethodsAsync();

        if (!loaded)
            return;

        base.ShowDialog();
    }

    private async Task<bool> LoadPaymentMethodsAsync()
    {
        try
        {
            var methods = await _storeHubClient.GetAllPaymentMethodsAsync();

            PopulateGrid(methods);

            return true;
        }
        catch (Exception ex)
        {
            _messageForm.BringToFront();
            _messageForm.ShowDialog($"ไม่สามารถโหลดข้อมูลวิธีการชำระเงินได้ Error: {ex.Message}", "เกิดข้อผิดพลาด");

            return false;
        }
    }

    private void PopulateGrid(IReadOnlyList<PaymentMethodDto> methods)
    {
        _isPopulatingGrid = true;

        try
        {
            PaymentMethodsGrid.Rows.Clear();

            foreach (var method in methods.OrderBy(m => m.DisplayOrder))
            {
                var rowIndex = PaymentMethodsGrid.Rows.Add();
                var row = PaymentMethodsGrid.Rows[rowIndex];

                row.Cells[(int)PaymentMethodColumn.Code].Value = method.Code;
                row.Cells[(int)PaymentMethodColumn.DisplayName].Value = method.DisplayName;
                row.Cells[(int)PaymentMethodColumn.Kind].Value = method.Kind.ToString();
                row.Cells[(int)PaymentMethodColumn.Enabled].Value = method.IsEnabled;
                row.Cells[(int)PaymentMethodColumn.DisplayOrder].Value = method.DisplayOrder;
            }
        }
        finally
        {
            _isPopulatingGrid = false;
        }
    }

    private void PaymentMethodsGrid_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        // Checkbox columns only raise CellValueChanged once the edit is committed.
        // Commit immediately so toggling the checkbox takes effect on a single click.
        if (PaymentMethodsGrid.IsCurrentCellDirty && PaymentMethodsGrid.CurrentCell?.ColumnIndex == (int)PaymentMethodColumn.Enabled)
        {
            PaymentMethodsGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }

    private async void PaymentMethodsGrid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (_isPopulatingGrid || e.RowIndex < 0 || e.ColumnIndex != (int)PaymentMethodColumn.Enabled)
            return;

        var row = PaymentMethodsGrid.Rows[e.RowIndex];
        var code = (string)row.Cells[(int)PaymentMethodColumn.Code].Value!;
        var enabled = (bool)row.Cells[(int)PaymentMethodColumn.Enabled].Value!;

        try
        {
            await _storeHubClient.SetPaymentMethodEnabledAsync(code, enabled);
        }
        catch (StoreHubClientException ex)
        {
            _messageForm.BringToFront();
            _messageForm.ShowDialog($"ไม่สามารถอัพเดทสถานะได้ Error: {ex.Message}", "เกิดข้อผิดพลาด");

            await LoadPaymentMethodsAsync();
        }
        catch (Exception ex)
        {
            _messageForm.BringToFront();
            _messageForm.ShowDialog($"เกิดข้อผิดพลาดที่ไม่คาดคิด Error: {ex.Message}", "เกิดข้อผิดพลาด");

            await LoadPaymentMethodsAsync();
        }
    }

    private async void PaymentMethodsGrid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex != (int)PaymentMethodColumn.Save)
            return;

        await SaveRowAsync(e.RowIndex);
    }

    private async Task SaveRowAsync(int rowIndex)
    {
        var row = PaymentMethodsGrid.Rows[rowIndex];
        var code = (string)row.Cells[(int)PaymentMethodColumn.Code].Value!;
        var displayName = (row.Cells[(int)PaymentMethodColumn.DisplayName].Value as string ?? string.Empty).Trim();
        var displayOrderText = row.Cells[(int)PaymentMethodColumn.DisplayOrder].Value?.ToString()?.Trim() ?? string.Empty;

        if (!displayName.HasValue())
        {
            _messageForm.BringToFront();
            _messageForm.ShowDialog("กรุณาใส่ชื่อที่แสดง", "ข้อมูลไม่ถูกต้อง");

            return;
        }

        if (!int.TryParse(displayOrderText, out var displayOrder))
        {
            _messageForm.BringToFront();
            _messageForm.ShowDialog("กรุณาใส่ลำดับการแสดงผลเป็นตัวเลข", "ข้อมูลไม่ถูกต้อง");

            return;
        }

        try
        {
            await _storeHubClient.UpdatePaymentMethodDisplayAsync(code, displayName, displayOrder);
            await LoadPaymentMethodsAsync();
        }
        catch (StoreHubClientException ex)
        {
            _messageForm.BringToFront();
            _messageForm.ShowDialog($"ไม่สามารถบันทึกข้อมูลได้ Error: {ex.Message}", "เกิดข้อผิดพลาด");
        }
        catch (Exception ex)
        {
            _messageForm.BringToFront();
            _messageForm.ShowDialog($"เกิดข้อผิดพลาดที่ไม่คาดคิด Error: {ex.Message}", "เกิดข้อผิดพลาด");
        }
    }

    private async void AddCampaignButton_Click(object sender, EventArgs e)
    {
        var code = CampaignCodeTextBox.Texts.Trim();
        var displayName = CampaignDisplayNameTextBox.Texts.Trim();
        var displayOrderText = CampaignDisplayOrderTextBox.Texts.Trim();

        if (!code.HasValue() || !displayName.HasValue())
        {
            _messageForm.BringToFront();
            _messageForm.ShowDialog("กรุณาใส่รหัสและชื่อที่แสดง", "ข้อมูลไม่ถูกต้อง");

            return;
        }

        if (!int.TryParse(displayOrderText, out var displayOrder))
        {
            _messageForm.BringToFront();
            _messageForm.ShowDialog("กรุณาใส่ลำดับการแสดงผลเป็นตัวเลข", "ข้อมูลไม่ถูกต้อง");

            return;
        }

        try
        {
            await _storeHubClient.AddCampaignPaymentMethodAsync(code, displayName, displayOrder);

            CampaignCodeTextBox.Texts = string.Empty;
            CampaignDisplayNameTextBox.Texts = string.Empty;
            CampaignDisplayOrderTextBox.Texts = string.Empty;

            await LoadPaymentMethodsAsync();
        }
        catch (StoreHubClientException ex)
        {
            _messageForm.BringToFront();
            _messageForm.ShowDialog($"ไม่สามารถเพิ่มวิธีการชำระเงินได้ Error: {ex.Message}", "เกิดข้อผิดพลาด");
        }
        catch (Exception ex)
        {
            _messageForm.BringToFront();
            _messageForm.ShowDialog($"เกิดข้อผิดพลาดที่ไม่คาดคิด Error: {ex.Message}", "เกิดข้อผิดพลาด");
        }
    }

    private void CloseButton_Click(object sender, EventArgs e)
    {
        Hide();
    }
}
