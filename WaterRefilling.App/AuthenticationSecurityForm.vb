Option Strict On
Option Explicit On

Public Class AuthenticationSecurityForm
    Inherits Form
    Private ReadOnly accountValue As New Label()
    Private ReadOnly rolesValue As New Label()
    Private ReadOnly permissionsValue As New Label()
    Private ReadOnly statusValue As New Label()

    Public Sub New()
        Text = "Authentication & Security"
        StartPosition = FormStartPosition.CenterParent
        MinimumSize = New Size(720, 440)
        Size = New Size(820, 500)
        Dim layout As New TableLayoutPanel With {.Dock = DockStyle.Fill, .Padding = New Padding(24), .ColumnCount = 2, .RowCount = 5}
        layout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 170))
        layout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        AddPair(layout, 0, "Current account", accountValue)
        AddPair(layout, 1, "Roles", rolesValue)
        AddPair(layout, 2, "API permissions", permissionsValue)
        AddPair(layout, 3, "Session status", statusValue)
        Dim note As New Label With {
            .Text = "This starter screen reports the current session only. Account administration, password policies, MFA, session revocation, and security-event logging require API endpoints and server-side enforcement.",
            .AutoSize = True, .MaximumSize = New Size(560, 0), .ForeColor = Color.DimGray
        }
        layout.Controls.Add(note, 1, 4)
        Controls.Add(layout)
        RefreshSessionSummary()
    End Sub

    Private Shared Sub AddPair(layout As TableLayoutPanel, row As Integer, caption As String, value As Label)
        layout.Controls.Add(New Label With {.Text = caption, .AutoSize = True, .Margin = New Padding(3, 10, 3, 10)}, 0, row)
        value.AutoSize = True
        value.MaximumSize = New Size(550, 0)
        value.Margin = New Padding(3, 10, 3, 10)
        layout.Controls.Add(value, 1, row)
    End Sub

    Private Sub RefreshSessionSummary()
        accountValue.Text = "Signed-in account (name endpoint not currently exposed)"
        rolesValue.Text = String.Join(", ", ApiSession.CurrentUserRoles)
        permissionsValue.Text = If(ApiSession.CurrentUserPermissions Is Nothing, "Not loaded", String.Join(", ", ApiSession.CurrentUserPermissions))
        statusValue.Text = If(ApiSession.HasServerPermissions, "Authenticated; API permissions loaded", "Permissions not confirmed by API")
    End Sub
End Class
