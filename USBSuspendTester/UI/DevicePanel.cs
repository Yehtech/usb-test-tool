using USBSuspendTester.Models;
using USBSuspendTester.Services;

namespace USBSuspendTester.UI;

/// <summary>
/// UserControl displaying a DataGridView of connected USB devices with a Refresh button.
/// </summary>
public sealed class DevicePanel : UserControl
{
    private readonly DataGridView _grid;
    private readonly Button _btnRefresh;
    private List<USBDevice> _devices = [];

    public DevicePanel()
    {
        Dock = DockStyle.Top;
        Height = 200;
        BackColor = Constants.PanelColor;
        Padding = new Padding(8);

        // Header
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 36,
            BackColor = Constants.PanelColor,
        };

        var lblTitle = new Label
        {
            Text = "USB Devices",
            ForeColor = Constants.TextColor,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(0, 8),
        };

        _btnRefresh = new Button
        {
            Text = "Refresh",
            Width = 80,
            Height = 28,
            FlatStyle = FlatStyle.Flat,
            BackColor = Constants.BtnDefaultBg,
            ForeColor = Constants.BtnTextColor,
            Font = new Font("Segoe UI", 9f),
            Cursor = Cursors.Hand,
            Dock = DockStyle.Right,
        };
        _btnRefresh.Click += async (_, _) => await RefreshDevicesAsync();

        header.Controls.Add(lblTitle);
        header.Controls.Add(_btnRefresh);

        // Grid
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            AutoGenerateColumns = false,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            BackgroundColor = Constants.BgColor,
            GridColor = Constants.BorderColor,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Constants.BgColor,
                ForeColor = Constants.TextColor,
                SelectionBackColor = Constants.GridSelectionBg,
                SelectionForeColor = Constants.TextColor,
                Font = new Font("Segoe UI", 9f),
            },
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Constants.GridHeaderBg,
                ForeColor = Constants.TextColor,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
            },
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Constants.GridRowAltBg,
                ForeColor = Constants.TextColor,
                SelectionBackColor = Constants.GridSelectionBg,
                SelectionForeColor = Constants.TextColor,
            },
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = 30,
        };

        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "VID", HeaderText = "VID", Width = 60 },
            new DataGridViewTextBoxColumn { Name = "PID", HeaderText = "PID", Width = 60 },
            new DataGridViewTextBoxColumn { Name = "DeviceName", HeaderText = "Device Name", Width = 280 },
            new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", Width = 80 },
            new DataGridViewTextBoxColumn { Name = "InstanceId", HeaderText = "Instance ID", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill }
        );

        Controls.Add(_grid);
        Controls.Add(header);
    }

    /// <summary>
    /// Asynchronously refreshes the device list without blocking the UI thread.
    /// Preserves the currently selected device across refresh.
    /// </summary>
    public async Task RefreshDevicesAsync()
    {
        _btnRefresh.Enabled = false;
        try
        {
            // Remember current selection
            string? selectedInstanceId = GetSelectedInstanceId();

            _devices = await USBManager.EnumerateDevicesAsync();
            _grid.Rows.Clear();

            if (_devices.Count == 0)
            {
                // Show empty-state placeholder row
                int rowIdx = _grid.Rows.Add("", "", "No USB devices found", "", "");
                _grid.Rows[rowIdx].DefaultCellStyle.ForeColor = Constants.TextDimColor;
                _grid.Rows[rowIdx].DefaultCellStyle.SelectionForeColor = Constants.TextDimColor;
                return;
            }

            foreach (USBDevice device in _devices)
            {
                _grid.Rows.Add(device.Vid, device.Pid, device.Description, device.Status, device.InstanceId);
            }

            // Restore selection
            if (selectedInstanceId is not null)
            {
                foreach (DataGridViewRow row in _grid.Rows)
                {
                    if (string.Equals(
                        row.Cells["InstanceId"].Value?.ToString(),
                        selectedInstanceId,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        row.Selected = true;
                        break;
                    }
                }
            }
        }
        finally
        {
            _btnRefresh.Enabled = true;
        }
    }

    /// <summary>
    /// Synchronous wrapper for fire-and-forget callers.
    /// </summary>
    public void RefreshDevices()
    {
        _ = RefreshDevicesAsync();
    }

    /// <summary>
    /// Returns the InstanceId of the currently selected row, or null.
    /// </summary>
    private string? GetSelectedInstanceId()
    {
        if (_grid.SelectedRows.Count == 0) return null;
        return _grid.SelectedRows[0].Cells["InstanceId"].Value?.ToString();
    }

    /// <summary>
    /// Returns the currently selected USB device, or null if none selected.
    /// Uses InstanceId from the grid cell for reliable lookup.
    /// </summary>
    public USBDevice? GetSelectedDevice()
    {
        string? instanceId = GetSelectedInstanceId();
        if (string.IsNullOrEmpty(instanceId)) return null;

        return _devices.Find(d =>
            string.Equals(d.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Enables or disables user interaction with the grid and refresh button.
    /// </summary>
    public void SetInteractionEnabled(bool enabled)
    {
        _btnRefresh.Enabled = enabled;
        _grid.Enabled = enabled;
    }
}

// ============================================
// Modified by Claude Code
// Date: 2026-03-07
// Version: v1.1.0
// ============================================
