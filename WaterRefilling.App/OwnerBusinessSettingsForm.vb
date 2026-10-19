Option Strict On
Option Explicit On

Public Class OwnerBusinessSettingsForm
    Inherits Form
    Private ReadOnly businessName As New TextBox()
    Private ReadOnly businessAddress As New TextBox()
    Private ReadOnly contactNumber As New TextBox()
    Private ReadOnly businessEmail As New TextBox()

    Public Sub New()
        Text = "Business Settings"
        StartPosition = FormStartPosition.CenterParent
        MinimumSize = New Size(640, 420)
        Size = New Size(760, 480)
        Dim layout As New TableLayoutPanel With {.Dock = DockStyle.Fill, .Padding = New Padding(24), .ColumnCount = 2, .RowCount = 6}
        layout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 170))
        layout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        AddInput(layout, 0, "Business name", businessName)
        AddInput(layout, 1, "Business address", businessAddress)
        AddInput(layout, 2, "Contact number", contactNumber)
        AddInput(layout, 3, "Business email", businessEmail)
        Dim warning As New Label With {.Text = "Save is intentionally unavailable until Owner-authorized API storage is implemented. Closing this form will discard entered values.", .AutoSize = True, .MaximumSize = New Size(500, 0), .ForeColor = Color.DarkGoldenrod}
        layout.Controls.Add(warning, 1, 4)
        Dim save As New Button With {.Text = "Save settings", .AutoSize = True, .Enabled = False}
        Dim tip As New ToolTip()
        tip.SetToolTip(save, "Enable only after the API endpoint is implemented and tested.")
        layout.Controls.Add(save, 1, 5)
        Controls.Add(layout)
    End Sub

    Private Shared Sub AddInput(layout As TableLayoutPanel, row As Integer, caption As String, input As TextBox)
        input.Dock = DockStyle.Top
        layout.Controls.Add(New Label With {.Text = caption, .AutoSize = True, .Margin = New Padding(3, 8, 3, 8)}, 0, row)
        layout.Controls.Add(input, 1, row)
    End Sub
End Class
