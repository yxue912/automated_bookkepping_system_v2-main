Option Strict On
Option Explicit On

Public Class OwnerSystemSettingsForm
    Inherits Form
    Public Sub New()
        Text = "System Settings"
        StartPosition = FormStartPosition.CenterParent
        Size = New Size(800, 500)
        MinimumSize = New Size(680, 420)
        Dim layout As New TableLayoutPanel With {.Dock = DockStyle.Fill, .Padding = New Padding(24), .ColumnCount = 2, .RowCount = 5}
        layout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 200))
        layout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        AddRow(layout, 0, "Application version", Application.ProductVersion)
        AddRow(layout, 1, "API base address", If(ApiSession.Client?.BaseAddress?.ToString(), "Not available"))
        AddRow(layout, 2, "Session timeout", "Must be configured and enforced by API")
        AddRow(layout, 3, "Backup status", "Verify SQLite backup process separately")
        Dim note As New Label With {.Text = "Read-only starter screen. Persistent system settings and authorized API endpoints are not implemented yet.", .AutoSize = True, .MaximumSize = New Size(520, 0), .ForeColor = Color.DimGray}
        layout.Controls.Add(note, 1, 4)
        Controls.Add(layout)
    End Sub
    Private Shared Sub AddRow(layout As TableLayoutPanel, row As Integer, caption As String, value As String)
        layout.Controls.Add(New Label With {.Text = caption, .AutoSize = True, .Margin = New Padding(3, 10, 3, 10)}, 0, row)
        layout.Controls.Add(New Label With {.Text = value, .AutoSize = True, .MaximumSize = New Size(520, 0), .Margin = New Padding(3, 10, 3, 10)}, 1, row)
    End Sub
End Class
