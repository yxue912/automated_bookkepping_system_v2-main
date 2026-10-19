Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports System.Net.Http
Imports System.Text.Json
Imports System.Globalization
Imports System.Threading.Tasks
Imports System.Collections.Generic
Imports System.Linq

Public Class MainForm
    Inherits Form

    Private ReadOnly primaryGreen As Color = Color.FromArgb(46, 157, 88)
    Private ReadOnly deepGreen As Color = Color.FromArgb(23, 107, 58)
    Private ReadOnly pageBackground As Color = Color.FromArgb(245, 250, 245)
    Private ReadOnly textColor As Color = Color.FromArgb(32, 51, 40)

    Private sidebar As Panel
    Private menuFlow As FlowLayoutPanel
    Private workspace As Panel
    Private pageTitle As Label

    ' Notification controls
    Private notificationButton As Panel
    Private notificationBadge As Label
    Private notificationPanel As Panel
    Private notificationMessage As Label
    Private notificationToolTip As ToolTip
    Private notificationUnread As Boolean = True
    Private notificationMessageText As String = ""

    Private transactionsExpanded As Boolean = False
    Private financialReportsExpanded As Boolean = False

    Private transactionsButton As Button
    Private financialReportsButton As Button

    Private ReadOnly transactionMenuButtons As New List(Of Button)
    Private ReadOnly financialReportMenuButtons As New List(Of Button)
    Private ReadOnly sidebarMenuButtons As New List(Of Button)
    Private ReadOnly summaryValues As New Dictionary(Of String, Label)()

    Private notificationClickFilter As NotificationOutsideClickFilter

    Private Class NotificationOutsideClickFilter
        Implements IMessageFilter

        Private ReadOnly owner As MainForm

        Public Sub New(form As MainForm)
            owner = form
        End Sub

        Public Function PreFilterMessage(ByRef m As Message) As Boolean _
            Implements IMessageFilter.PreFilterMessage

            If m.Msg <> &H201 OrElse owner.IsDisposed Then
                Return False
            End If

            If owner.NotificationPanelIsVisible() Then
                Dim screenPoint As Point = Cursor.Position

                If Not owner.NotificationBellScreenRectangle().Contains(screenPoint) AndAlso
                Not owner.NotificationPanelScreenRectangle().Contains(screenPoint) Then

                    owner.CloseNotificationPanel()
                End If
            End If

            Return False
        End Function
    End Class

Public Sub New()
        InitializeDashboard()

        AddHandler AppNotifications.NotificationAdded,
            AddressOf HandleAppNotification
    End Sub

    Private Sub InitializeDashboard()
        Text = "Aqua ARC - Water Station Bookkeeping"
        StartPosition = FormStartPosition.CenterScreen
        Size = New Size(1440, 900)
        MinimumSize = New Size(1100, 700)
        BackColor = pageBackground
        Font = New Font("Segoe UI", 10)

        BuildSidebar()
        BuildHeader()
        BuildDashboard()
        SetLoginNotification()

        notificationClickFilter = New NotificationOutsideClickFilter(Me)
        Application.AddMessageFilter(notificationClickFilter)

        AddHandler Me.Shown,
            Async Sub(sender, e)
                Await LoadDashboardAsync()
            End Sub

        AddHandler Me.FormClosed,
            Sub(sender, e)
                If notificationClickFilter IsNot Nothing Then
                    Application.RemoveMessageFilter(notificationClickFilter)
                End If

                If notificationToolTip IsNot Nothing Then
                    notificationToolTip.Dispose()
                End If
            End Sub
    End Sub

    ' =========================================================
    ' SIDEBAR
    ' =========================================================

    Private Sub BuildSidebar()
        sidebar = New Panel With {
            .Dock = DockStyle.Left,
            .Width = 245,
            .BackColor = Color.White,
            .Padding = New Padding(0),
            .AutoScroll = False
        }

        Controls.Add(sidebar)

        Dim brandPanel As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 100,
            .BackColor = Color.White
        }

        sidebar.Controls.Add(brandPanel)

        Dim logo As New Panel With {
            .Location = New Point(12, 15),
            .Size = New Size(42, 42),
            .BackColor = Color.FromArgb(228, 245, 232)
        }

        MakeRounded(logo, 12)
        brandPanel.Controls.Add(logo)

        Dim logoText As New Label With {
            .Text = "AA",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .ForeColor = deepGreen,
            .AutoSize = False,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Dock = DockStyle.Fill
        }

        logo.Controls.Add(logoText)

        Dim appName As New Label With {
            .Text = "AQUA ARC" & vbCrLf & "BOOKKEEPING",
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = deepGreen,
            .Location = New Point(65, 17),
            .Size = New Size(155, 40)
        }

        brandPanel.Controls.Add(appName)

        Dim subtitle As New Label With {
            .Text = "WATER STATION BOOKKEEPING",
            .Font = New Font("Segoe UI", 7, FontStyle.Bold),
            .ForeColor = Color.Gray,
            .Location = New Point(12, 72),
            .AutoSize = True
        }

        brandPanel.Controls.Add(subtitle)

        menuFlow = New FlowLayoutPanel With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.White,
            .FlowDirection = FlowDirection.TopDown,
            .WrapContents = False,
            .AutoScroll = True,
            .Padding = New Padding(5, 7, 5, 8),
            .Margin = New Padding(0)
        }

        sidebar.Controls.Add(menuFlow)
        menuFlow.BringToFront()

        Dim userPanel As New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 100,
            .BackColor = Color.FromArgb(245, 250, 245)
        }

        sidebar.Controls.Add(userPanel)
        userPanel.BringToFront()

         Dim logoutButton As New Button With {
            .Text = "Log Out",
            .Location = New Point(12, 62),
            .Size = New Size(215, 30),
            .Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top,
            .BackColor = Color.FromArgb(253, 237, 237),
            .ForeColor = Color.Firebrick,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .Cursor = Cursors.Hand
        }

        logoutButton.FlatAppearance.BorderSize = 0

        AddHandler logoutButton.Click,
            AddressOf LogoutButton_Click

        userPanel.Height = 100
        userPanel.Controls.Add(logoutButton)
        logoutButton.BringToFront()

        Dim displayName As String = "Signed-in User"
        Dim displayRole As String = "Authorized User"

        If Not String.IsNullOrWhiteSpace(ApiSession.CurrentUserName) Then
            displayName = ApiSession.CurrentUserName
        End If

        If ApiSession.CurrentUserRoles IsNot Nothing AndAlso
           ApiSession.CurrentUserRoles.Count > 0 Then

            displayRole = GetDisplayRole(ApiSession.CurrentUserRoles)
        End If

        Dim userName As New Label With {
            .Text = displayName,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .ForeColor = textColor,
            .Location = New Point(12, 8),
            .AutoSize = True,
            .MaximumSize = New Size(220, 22)
        }

        userPanel.Controls.Add(userName)

        Dim userRole As New Label With {
            .Text = displayRole,
            .Font = New Font("Segoe UI", 8),
            .ForeColor = Color.Gray,
            .Location = New Point(12, 32),
            .AutoSize = True
        }

        userPanel.Controls.Add(userRole)

        AddSidebarButton("Dashboard")
        AddSidebarButton("Authentication & Security")
        AddSidebarButton("System Setup")
        AddSidebarButton("Master Data")
        AddSidebarButton("Fixed Assets")

        transactionsButton = CreateSidebarButton(
            "Transactions  >", False)

        AddHandler transactionsButton.Click,
            Sub(sender, e)
                transactionsExpanded = Not transactionsExpanded
                UpdateTransactionSubmenu()
            End Sub

        menuFlow.Controls.Add(transactionsButton)
        sidebarMenuButtons.Add(transactionsButton)

        Dim receiptsButton As Button =
            CreateSidebarButton("Cash Receipts Book", True)

        AddHandler receiptsButton.Click,
            Sub(sender, e)
                OpenCashReceipts()
            End Sub

        menuFlow.Controls.Add(receiptsButton)
        transactionMenuButtons.Add(receiptsButton)

        Dim disbursementsButton As Button =
            CreateSidebarButton("Cash Disbursements", True)

        AddHandler disbursementsButton.Click,
            Sub(sender, e)
                OpenCashDisbursements()
            End Sub

        menuFlow.Controls.Add(disbursementsButton)
        transactionMenuButtons.Add(disbursementsButton)

        AddSidebarButton("Journal Entries")
        AddSidebarButton("General Ledger")

        financialReportsButton = CreateSidebarButton(
            "Financial Reports  >", False)

        AddHandler financialReportsButton.Click,
            Sub(sender, e)
                financialReportsExpanded = Not financialReportsExpanded
                UpdateFinancialReportsSubmenu()
            End Sub

        menuFlow.Controls.Add(financialReportsButton)
        sidebarMenuButtons.Add(financialReportsButton)

        Dim lapsingButton As Button =
            CreateSidebarButton("Lapsing Schedule", True)

        AddHandler lapsingButton.Click,
            Sub(sender, e)
                pageTitle.Text = "Lapsing Schedule"
                ShowLapsingSchedule()
            End Sub

        menuFlow.Controls.Add(lapsingButton)
        financialReportMenuButtons.Add(lapsingButton)

        Dim trialBalanceButton As Button =
            CreateSidebarButton("Trial Balance", True)

        AddHandler trialBalanceButton.Click,
            Sub(sender, e)
                pageTitle.Text = "Trial Balance"

                Using trialBalance As New TrialBalanceForm()
                    trialBalance.ShowDialog(Me)
                End Using
            End Sub

        menuFlow.Controls.Add(trialBalanceButton)
        financialReportMenuButtons.Add(trialBalanceButton)

        AddSidebarButton("Audit Trail")
        AddSidebarButton("Settings")
        AddSidebarButton("Account Approvals")

        ApplyRoleBasedMenuVisibility()

        AddHandler menuFlow.SizeChanged,
            Sub(sender, e)
                UpdateSidebarButtonWidths()
            End Sub
    End Sub

    Private Function CreateSidebarButton(
        caption As String,
        isSubmenu As Boolean
    ) As Button

        Dim button As New Button With {
            .Text = caption,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Size = New Size(215, If(isSubmenu, 38, 43)),
            .Margin = New Padding(
                0, 0, 0, If(isSubmenu, 2, 5)),
            .Padding = New Padding(
                If(isSubmenu, 24, 8), 0, 4, 0),
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font(
                "Segoe UI",
                If(isSubmenu, 8.5F, 9.5F)),
            .Cursor = Cursors.Hand,
            .BackColor = Color.White,
            .ForeColor = Color.FromArgb(80, 100, 85),
            .Visible = Not isSubmenu,
            .UseVisualStyleBackColor = False
        }

        button.FlatAppearance.BorderSize = 0
        button.FlatAppearance.MouseOverBackColor =
            Color.FromArgb(238, 248, 240)

        Return button
    End Function

    Private Sub AddSidebarButton(caption As String)
        Dim button As Button = CreateSidebarButton(caption, False)

        If caption = "Dashboard" Then
            button.BackColor = Color.FromArgb(228, 245, 232)
            button.ForeColor = deepGreen
        End If

        AddHandler button.Click,
            Sub(sender, e)
                HandleNavigation(caption)
            End Sub

        menuFlow.Controls.Add(button)
        sidebarMenuButtons.Add(button)
    End Sub
    Private Function HasRole(roleName As String) As Boolean
        If ApiSession.CurrentUserRoles Is Nothing Then
            Return False
        End If

        For Each role As String In ApiSession.CurrentUserRoles
            If String.Equals(
                If(role, "").Trim(),
                roleName,
                StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If
        Next

        Return False
    End Function

    Private Function IsMenuAllowed(menuName As String) As Boolean
        menuName = If(menuName, "").Trim()

        ' Account approvals are available only to an Owner with user-management permission.
        If String.Equals(
            menuName,
            "Account Approvals",
            StringComparison.OrdinalIgnoreCase) Then

            Return HasRole("Owner") AndAlso
                ApiSession.HasServerPermissions AndAlso
                ApiSession.CurrentUserPermissions IsNot Nothing AndAlso
                ApiSession.CurrentUserPermissions.Contains("users.manage")
        End If

        ' Admin is limited to technical/security functions by design.
        If HasRole("Admin") Then
            Select Case menuName
                Case "Dashboard",
                     "Authentication & Security",
                     "System Setup"

                    Return True

                Case Else
                    Return False
            End Select
        End If

        ' Prefer server-provided permissions when available.
        If ApiSession.HasServerPermissions AndAlso
            ApiSession.CurrentUserPermissions IsNot Nothing Then

            Dim permissions As List(Of String) =
                ApiSession.CurrentUserPermissions

            Select Case menuName
                Case "Dashboard"
                    Return permissions.Contains("dashboard.view")

                Case "Master Data"
                    Return permissions.Contains("accounts.view")

                Case "Fixed Assets"
                    Return permissions.Contains("fixed_assets.view")

                Case "Transactions"
                    Return permissions.Contains("cash_receipts.view") OrElse
                        permissions.Contains("cash_disbursements.view")

                Case "Cash Receipts Book"
                    Return permissions.Contains("cash_receipts.view")

                Case "Cash Disbursements"
                    Return permissions.Contains("cash_disbursements.view")

                Case "Journal Entries"
                    Return permissions.Contains("journal.view")

                Case "General Ledger"
                    Return permissions.Contains("ledger.view")

                Case "Financial Reports"
                    Return permissions.Contains("financial_reports.view") OrElse
                        permissions.Contains("trial_balance.view")

                Case "Lapsing Schedule"
                    Return permissions.Contains("financial_reports.view")

                Case "Trial Balance"
                    Return permissions.Contains("trial_balance.view")

                Case "Audit Trail"
                    Return HasRole("Owner") AndAlso
                        permissions.Contains("audit_logs.view")

                Case "Settings"
                    Return HasRole("Owner") AndAlso
                        (permissions.Contains("organization.view") OrElse
                         permissions.Contains("organization.edit") OrElse
                         permissions.Contains("system_settings.view") OrElse
                         permissions.Contains("system_settings.edit"))
            End Select
        End If

        ' Conservative fallback for sessions without server permissions.
        Select Case menuName
            Case "Dashboard"
                Return HasRole("Owner") OrElse HasRole("Staff")

            Case "Master Data",
                 "Fixed Assets",
                 "Transactions",
                 "Cash Receipts Book",
                 "Cash Disbursements",
                 "Journal Entries",
                 "General Ledger",
                 "Financial Reports",
                 "Lapsing Schedule",
                 "Trial Balance"

                Return HasRole("Owner")

            Case "Audit Trail", "Settings", "Account Approvals"
                Return False

            Case Else
                Return False
        End Select
    End Function

    Private Sub ApplyRoleBasedMenuVisibility()
        If menuFlow Is Nothing Then Return

        For Each control As Control In menuFlow.Controls
            Dim button As Button = TryCast(control, Button)
            If button Is Nothing Then Continue For

            Dim menuName As String = button.Text.Trim()
            menuName = menuName.Replace("  >", "")
            menuName = menuName.Replace("  v", "")
            menuName = menuName.Trim()

            button.Visible = IsMenuAllowed(menuName)
        Next

        UpdateTransactionSubmenu()
        UpdateFinancialReportsSubmenu()
        UpdateSidebarButtonWidths()
    End Sub
        Private Sub UpdateSidebarButtonWidths()
    If menuFlow Is Nothing Then Return

    Dim availableWidth As Integer =
        Math.Max(150, menuFlow.ClientSize.Width -
                    menuFlow.Padding.Horizontal - 5)

    For Each control As Control In menuFlow.Controls
        control.Width = availableWidth
    Next
End Sub

    Private Sub UpdateTransactionSubmenu()
    For Each button As Button In transactionMenuButtons
        button.Visible =
            transactionsExpanded AndAlso
            IsMenuAllowed(button.Text.Trim())
    Next

    If transactionsButton IsNot Nothing Then
        transactionsButton.Visible =
            IsMenuAllowed("Transactions")

        transactionsButton.Text =
            If(transactionsExpanded,
            "Transactions  v",
            "Transactions  >")
    End If

    menuFlow.PerformLayout()
    menuFlow.Invalidate()
End Sub

Private Sub UpdateFinancialReportsSubmenu()
    For Each button As Button In financialReportMenuButtons
        button.Visible =
            financialReportsExpanded AndAlso
            IsMenuAllowed(button.Text.Trim())
    Next

    If financialReportsButton IsNot Nothing Then
        financialReportsButton.Visible =
            IsMenuAllowed("Financial Reports")

        financialReportsButton.Text =
            If(financialReportsExpanded,
            "Financial Reports  v",
            "Financial Reports  >")
    End If

    menuFlow.PerformLayout()
    menuFlow.Invalidate()
End Sub

    Private Sub OpenCashReceipts()
        pageTitle.Text = "Cash Receipts Book"

        Using receipts As New CashReceiptsForm()
            receipts.ShowDialog(Me)
        End Using
    End Sub

    Private Sub OpenCashDisbursements()
        pageTitle.Text = "Cash Disbursements"

        Using disbursements As New CashDisbursementsForm()
            disbursements.ShowDialog(Me)
        End Using
    End Sub

    ' =========================================================
    ' HEADER AND NOTIFICATIONS
    ' =========================================================

    Private Sub BuildHeader()
        Dim header As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 75,
            .BackColor = Color.White,
            .Padding = New Padding(25, 12, 25, 12)
        }

        Controls.Add(header)
        header.BringToFront()

        pageTitle = New Label With {
            .Text = "Dashboard",
            .Font = New Font("Segoe UI", 19, FontStyle.Bold),
            .ForeColor = textColor,
            .Location = New Point(20, 17),
            .AutoSize = True
        }

        header.Controls.Add(pageTitle)

        Dim period As New Label With {
            .Text = "Fiscal Year " & DateTime.Now.Year.ToString(),
            .Font = New Font("Segoe UI", 9),
            .ForeColor = deepGreen,
            .BackColor = Color.FromArgb(238, 248, 240),
            .TextAlign = ContentAlignment.MiddleCenter,
            .Size = New Size(145, 35),
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right,
            .Location = New Point(
                Math.Max(20, ClientSize.Width - 190), 18)
        }

        MakeRounded(period, 10)
        header.Controls.Add(period)

        notificationToolTip = New ToolTip With {
            .InitialDelay = 350,
            .ReshowDelay = 100,
            .AutoPopDelay = 4000,
            .ShowAlways = True
        }

        notificationButton = New Panel With {
            .Size = New Size(42, 38),
            .BackColor = Color.White,
            .Cursor = Cursors.Hand,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right,
            .Location = New Point(period.Left - 55, 17),
            .AccessibleName = "Notifications"
        }

        AddHandler notificationButton.Paint,
            Sub(sender, e)
                DrawBellIcon(e.Graphics, notificationButton.ClientRectangle)
            End Sub

        AddHandler notificationButton.MouseEnter,
            Sub(sender, e)
                notificationButton.BackColor =
                    Color.FromArgb(238, 248, 240)
                notificationButton.Invalidate()
            End Sub

        AddHandler notificationButton.MouseLeave,
            Sub(sender, e)
                notificationButton.BackColor = Color.White
                notificationButton.Invalidate()
            End Sub

        AddHandler notificationButton.Click,
            Sub(sender, e)
                ToggleNotificationPanel()
            End Sub

        notificationToolTip.SetToolTip(
            notificationButton,
            "View notifications")

        header.Controls.Add(notificationButton)

        notificationBadge = New Label With {
            .Text = "1",
            .Size = New Size(18, 18),
            .Location = New Point(27, 0),
            .BackColor = Color.FromArgb(210, 55, 55),
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 8, FontStyle.Bold),
            .TextAlign = ContentAlignment.MiddleCenter
        }

        MakeRounded(notificationBadge, 9)
        notificationButton.Controls.Add(notificationBadge)
        notificationBadge.BringToFront()

        notificationPanel = New Panel With {
            .Size = New Size(330, 155),
            .BackColor = Color.White,
            .BorderStyle = BorderStyle.FixedSingle,
            .Visible = False
        }

        Dim notificationHeading As New Label With {
            .Text = "Notifications",
            .Font = New Font("Segoe UI", 12, FontStyle.Bold),
            .ForeColor = deepGreen,
            .Location = New Point(15, 12),
            .AutoSize = True
        }

        notificationPanel.Controls.Add(notificationHeading)

        notificationMessage = New Label With {
            .Location = New Point(15, 45),
            .Size = New Size(295, 60),
            .Font = New Font("Segoe UI", 9),
            .ForeColor = textColor,
            .TextAlign = ContentAlignment.TopLeft
        }

        notificationPanel.Controls.Add(notificationMessage)

        Dim markReadButton As New Button With {
            .Text = "Mark as read",
            .Size = New Size(100, 30),
            .Location = New Point(205, 112),
            .BackColor = Color.FromArgb(238, 248, 240),
            .ForeColor = deepGreen,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }

        markReadButton.FlatAppearance.BorderSize = 0
        notificationPanel.Controls.Add(markReadButton)

        AddHandler markReadButton.Click,
            Sub(sender, e)
                notificationUnread = False
                UpdateNotificationBadge()
                CloseNotificationPanel()
            End Sub

        Controls.Add(notificationPanel)
        notificationPanel.BringToFront()

        AddHandler header.Resize,
            Sub(sender, e)
                period.Left = header.ClientSize.Width -
                              period.Width - 25

                notificationButton.Left = period.Left - 55
            End Sub
    End Sub

    Private Sub DrawBellIcon(
        graphics As Graphics,
        bounds As Rectangle)

        graphics.SmoothingMode = SmoothingMode.AntiAlias

        Using bellPen As New Pen(deepGreen, 2.2F)
            bellPen.StartCap = LineCap.Round
            bellPen.EndCap = LineCap.Round

            Dim centerX As Single = bounds.Width / 2.0F
            Dim topY As Single = 7.0F

            Using bellPath As New GraphicsPath()
                bellPath.AddArc(
                    centerX - 8,
                    topY,
                    16,
                    19,
                    180,
                    180)

                bellPath.AddLine(
                    centerX + 8,
                    topY + 9,
                    centerX + 8,
                    topY + 19)

                bellPath.AddLine(
                    centerX + 8,
                    topY + 19,
                    centerX + 11,
                    topY + 22)

                bellPath.AddLine(
                    centerX + 11,
                    topY + 22,
                    centerX - 11,
                    topY + 22)

                bellPath.AddLine(
                    centerX - 11,
                    topY + 22,
                    centerX - 8,
                    topY + 19)

                bellPath.AddLine(
                    centerX - 8,
                    topY + 19,
                    centerX - 8,
                    topY + 9)

                graphics.DrawPath(bellPen, bellPath)
            End Using

            graphics.DrawArc(
                bellPen,
                centerX - 4,
                topY + 18,
                8,
                6,
                0,
                180)

            graphics.DrawLine(
                bellPen,
                centerX - 3,
                topY + 27,
                centerX + 3,
                topY + 27)
        End Using
    End Sub

    Private Sub SetLoginNotification()
        Dim roleLabel As String = "Authorized User"

        If ApiSession.CurrentUserRoles IsNot Nothing AndAlso
           ApiSession.CurrentUserRoles.Count > 0 Then

            roleLabel = GetDisplayRole(ApiSession.CurrentUserRoles)
        End If

        notificationMessageText =
            "You have successfully signed in as " &
            roleLabel & "."

        If notificationMessage IsNot Nothing Then
            notificationMessage.Text = notificationMessageText
        End If

        notificationUnread = True
        UpdateNotificationBadge()
    End Sub

    Private Function GetDisplayRole(roles As List(Of String)) As String
        If roles Is Nothing OrElse roles.Count = 0 Then
            Return "Authorized User"
        End If

        Dim actualRole As String = roles(0).Trim()

        Select Case actualRole.ToLowerInvariant()
            Case "admin"
                Return "Administrator"
            Case "owner"
                Return "Owner"
            Case "staff"
                Return "Staff"
            Case Else
                Return actualRole
        End Select
    End Function

    Private Sub ToggleNotificationPanel()
        If notificationPanel Is Nothing OrElse
           notificationButton Is Nothing Then
            Return
        End If

        If notificationPanel.Visible Then
            CloseNotificationPanel()
            Return
        End If

        notificationMessage.Text = notificationMessageText

        Dim screenLocation As Point =
            notificationButton.PointToScreen(
                New Point(0, notificationButton.Height))

        notificationPanel.Location = PointToClient(screenLocation)

        notificationPanel.Left = Math.Max(
            10,
            Math.Min(
                notificationPanel.Left,
                ClientSize.Width - notificationPanel.Width - 10))

        notificationPanel.Top = Math.Max(75, notificationPanel.Top)

        notificationPanel.Visible = True
        notificationPanel.BringToFront()
    End Sub

    Private Sub UpdateNotificationBadge()
        If notificationBadge Is Nothing Then Return

        notificationBadge.Visible = notificationUnread
        notificationBadge.Text = If(notificationUnread, "1", "")
    End Sub

    Private Sub CloseNotificationPanel()
        If notificationPanel IsNot Nothing Then
            notificationPanel.Visible = False
        End If
    End Sub

    Private Function NotificationPanelIsVisible() As Boolean
        Return notificationPanel IsNot Nothing AndAlso
               notificationPanel.Visible
    End Function

    Private Function NotificationBellScreenRectangle() As Rectangle
        If notificationButton Is Nothing Then Return Rectangle.Empty

        Return notificationButton.RectangleToScreen(
            notificationButton.ClientRectangle)
    End Function

    Private Function NotificationPanelScreenRectangle() As Rectangle
        If notificationPanel Is Nothing Then Return Rectangle.Empty

        Return notificationPanel.RectangleToScreen(
            notificationPanel.ClientRectangle)
    End Function

    Private Function NotificationPanelContainsScreenPoint(
        screenPoint As Point) As Boolean

        If notificationPanel Is Nothing OrElse Not notificationPanel.Visible Then
            Return False
        End If

        Return NotificationPanelScreenRectangle().Contains(screenPoint)
    End Function

    Private Sub BuildDashboard()
        workspace = New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = pageBackground,
            .AutoScroll = True,
            .Padding = New Padding(25)
        }

        Controls.Add(workspace)
        workspace.BringToFront()

        BuildDashboardContent()

        If notificationPanel IsNot Nothing Then
            notificationPanel.BringToFront()
        End If
    End Sub

    Private Sub BuildDashboardContent()
        workspace.Controls.Clear()
        summaryValues.Clear()

        Dim welcome As New Label With {
            .Text = "Welcome to Aqua ARC!",
            .Font = New Font("Segoe UI", 23, FontStyle.Bold),
            .ForeColor = deepGreen,
            .Location = New Point(25, 20),
            .AutoSize = True
        }

        workspace.Controls.Add(welcome)

        Dim description As New Label With {
            .Text = "Monitor your finances, accounts, and transactions in one place.",
            .Font = New Font("Segoe UI", 10),
            .ForeColor = Color.FromArgb(113, 132, 119),
            .Location = New Point(28, 65),
            .AutoSize = True
        }

        workspace.Controls.Add(description)

        Dim notice As New Label With {
            .Text = "Connecting to financial data...",
            .Name = "dashboardNotice",
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .ForeColor = deepGreen,
            .BackColor = Color.FromArgb(228, 245, 232),
            .Location = New Point(28, 100),
            .Size = New Size(430, 32),
            .TextAlign = ContentAlignment.MiddleCenter
        }

        MakeRounded(notice, 8)
        workspace.Controls.Add(notice)

        AddSummaryPanel(
            "CASH BALANCE", "Loading...",
            "Available cash", 25, 155, 360, 140, True)

        AddSummaryPanel(
            "TOTAL INCOME", "Loading...",
            "Recorded income", 405, 155, 230, 140)

        AddSummaryPanel(
            "TOTAL EXPENSES", "Loading...",
            "Recorded expenses", 655, 155, 230, 140)

        AddSummaryPanel(
            "RECEIVABLES", "Loading...",
            "Outstanding amounts", 905, 155, 210, 140)

        AddSummaryPanel(
            "PAYABLES", "Loading...",
            "Amounts owed", 25, 315, 250, 125)

        AddSummaryPanel(
            "TOTAL ASSETS", "Loading...",
            "Recorded assets", 295, 315, 250, 125)

        Dim activityTitle As New Label With {
            .Text = "Recent Activity",
            .Font = New Font("Segoe UI", 17, FontStyle.Bold),
            .ForeColor = textColor,
            .Location = New Point(25, 475),
            .AutoSize = True
        }

        workspace.Controls.Add(activityTitle)

        Dim activityDescription As New Label With {
            .Text = "Your latest bookkeeping transactions will appear here.",
            .Font = New Font("Segoe UI", 9),
            .ForeColor = Color.FromArgb(113, 132, 119),
            .Location = New Point(28, 510),
            .AutoSize = True
        }

        workspace.Controls.Add(activityDescription)

        Dim activityGrid As New DataGridView With {
            .Location = New Point(25, 545),
            .Size = New Size(1060, 200),
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.None,
            .ReadOnly = True,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .RowHeadersVisible = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .AutoGenerateColumns = False
        }

        activityGrid.Columns.Add("reference", "Reference")
        activityGrid.Columns.Add("date", "Date")
        activityGrid.Columns.Add("type", "Type")
        activityGrid.Columns.Add("description", "Description")
        activityGrid.Columns.Add("amount", "Amount")
        activityGrid.Columns.Add("status", "Status")

        activityGrid.ColumnHeadersDefaultCellStyle.BackColor =
            Color.FromArgb(228, 245, 232)

        activityGrid.ColumnHeadersDefaultCellStyle.ForeColor = deepGreen
        activityGrid.EnableHeadersVisualStyles = False

        activityGrid.Rows.Add(
            "—", "—", "—",
            "Recent activity is not connected yet", "—", "—")

        workspace.Controls.Add(activityGrid)
    End Sub

    Private Sub AddSummaryPanel(
        caption As String,
        value As String,
        detail As String,
        x As Integer,
        y As Integer,
        width As Integer,
        height As Integer,
        Optional featured As Boolean = False)

        Dim panel As New Panel With {
            .Location = New Point(x, y),
            .Size = New Size(width, height),
            .BackColor = If(
                featured,
                Color.FromArgb(228, 245, 232),
                Color.White)
        }

        MakeRounded(panel, 14)
        workspace.Controls.Add(panel)

        Dim accent As New Panel With {
            .Location = New Point(0, 0),
            .Size = New Size(5, height),
            .BackColor = primaryGreen
        }

        panel.Controls.Add(accent)

        Dim title As New Label With {
            .Text = caption,
            .Location = New Point(18, 18),
            .AutoSize = True,
            .Font = New Font("Segoe UI", 9, FontStyle.Bold),
            .ForeColor = Color.FromArgb(113, 132, 119)
        }

        panel.Controls.Add(title)

        Dim amount As New Label With {
            .Text = value,
            .Location = New Point(18, 48),
            .AutoSize = True,
            .Font = New Font(
                "Segoe UI",
                If(featured, 17, 13),
                FontStyle.Bold),
            .ForeColor = deepGreen
        }

        panel.Controls.Add(amount)
        summaryValues(caption) = amount

        Dim subtext As New Label With {
            .Text = detail,
            .Location = New Point(18, height - 32),
            .AutoSize = True,
            .Font = New Font("Segoe UI", 8),
            .ForeColor = Color.Gray
        }

        panel.Controls.Add(subtext)
    End Sub

    Private Async Function LoadDashboardAsync() As Task
        Try
            ApiSession.ApplyAuthorization()

            Using response As HttpResponseMessage =
                Await ApiSession.Client.GetAsync("dashboard")

                Dim json As String =
                    Await response.Content.ReadAsStringAsync()

                If Not response.IsSuccessStatusCode Then
                    Throw New Exception(
                        "Dashboard API returned HTTP " &
                        CInt(response.StatusCode).ToString() &
                        ": " & json)
                End If

                Using document As JsonDocument =
                    JsonDocument.Parse(json)

                    Dim root As JsonElement = document.RootElement
                    Dim data As JsonElement

                    If Not root.TryGetProperty("data", data) OrElse
                       data.ValueKind <> JsonValueKind.Object Then

                        Throw New Exception(
                            "The dashboard response has no valid data object.")
                    End If

                    UpdateSummary(
                        "CASH BALANCE",
                        ReadJsonDecimal(data, "cash_balance"))

                    UpdateSummary(
                        "TOTAL INCOME",
                        ReadJsonDecimal(data, "income"))

                    UpdateSummary(
                        "TOTAL EXPENSES",
                        ReadJsonDecimal(data, "expenses"))

                    UpdateSummary(
                        "RECEIVABLES",
                        ReadJsonDecimal(data, "receivables"))

                    UpdateSummary(
                        "PAYABLES",
                        ReadJsonDecimal(data, "payables"))

                    UpdateSummary(
                        "TOTAL ASSETS",
                        ReadJsonDecimal(data, "assets"))
                End Using
            End Using

            SetDashboardNotice(
                "Financial summary loaded successfully.")

        Catch ex As Exception
            For Each amount As Label In summaryValues.Values
                amount.Text = "Unavailable"
            Next

            SetDashboardNotice(
                "Unable to connect to financial data.")

            MessageBox.Show(
                "The dashboard could not load its financial summary." &
                vbCrLf & vbCrLf & ex.Message &
                vbCrLf & vbCrLf &
                "Check that the Laravel API is running and that your account " &
                "has permission to view the dashboard.",
                "Dashboard Connection",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning)
        End Try
    End Function

    Private Sub UpdateSummary(caption As String, amount As Decimal)
        If summaryValues.ContainsKey(caption) Then
            summaryValues(caption).Text = FormatPeso(amount)
        End If
    End Sub

    Private Sub SetDashboardNotice(message As String)
        For Each control As Control In workspace.Controls
            If control.Name = "dashboardNotice" Then
                control.Text = message
                Exit For
            End If
        Next
    End Sub

    ' =========================================================
    ' NAVIGATION
    ' =========================================================

    Private Sub HandleNavigation(item As String)
        Select Case item
            Case "Dashboard"
                pageTitle.Text = "Dashboard"
                BuildDashboardContent()
                AwaitDashboardRefresh()

            Case "Master Data"
                pageTitle.Text = "Chart of Accounts"

                Using accounts As New ChartOfAccountsForm()
                    accounts.ShowDialog(Me)
                End Using

            Case "Journal Entries"
                pageTitle.Text = "Journal Entries"

                Using journal As New JournalEntriesForm()
                    journal.ShowDialog(Me)
                End Using

            Case "Fixed Assets"
                pageTitle.Text = "Fixed Assets"

                Using assets As New FixedAssetsForm()
                    assets.ShowDialog(Me)
                End Using

            Case "General Ledger"
                pageTitle.Text = "General Ledger"

                Using ledger As New GeneralLedgerForm()
                    ledger.ShowDialog(Me)
                End Using
            Case "Authentication & Security"
                If Not HasRole("Admin") Then
                    MessageBox.Show(
                        "Only Admin can access Authentication & Security.",
                        "Access Denied",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning)
                    Return
                End If

                Using security As New AuthenticationSecurityForm()
                    security.ShowDialog(Me)
                End Using

            Case "System Setup"
                If Not HasRole("Admin") Then
                    MessageBox.Show(
                        "Only Admin can access System Setup.",
                        "Access Denied",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning)
                    Return
                End If

                Using settings As New OwnerSystemSettingsForm()
                    settings.ShowDialog(Me)
                End Using

            Case "Settings"
                If Not HasRole("Owner") Then
                    MessageBox.Show(
                        "Only the Owner can access Business Settings.",
                        "Access Denied",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning)
                    Return
                End If

                Using settings As New OwnerBusinessSettingsForm()
                    settings.ShowDialog(Me)
                End Using

            Case "Audit Trail"
                If Not HasRole("Owner") OrElse
                    Not ApiSession.HasServerPermissions OrElse
                    ApiSession.CurrentUserPermissions Is Nothing OrElse
                    Not ApiSession.CurrentUserPermissions.Contains("audit_logs.view") Then

                    MessageBox.Show(
                        "Only an Owner with audit-log viewing permission can access Audit Trail.",
                        "Access Denied",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning)
                    Return
                End If

                Using audit As New AuditTrailForm()
                    audit.ShowDialog(Me)
                End Using

            Case "Account Approvals"
                If Not HasRole("Owner") OrElse
                    Not ApiSession.HasServerPermissions OrElse
                    ApiSession.CurrentUserPermissions Is Nothing OrElse
                    Not ApiSession.CurrentUserPermissions.Contains("users.manage") Then

                    MessageBox.Show(
                        "Only an authorized Owner can access account approvals.",
                        "Access Denied",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning)
                    Return
                End If

                pageTitle.Text = "Account Approvals"

                Using approvals As New PendingAccountApprovalsForm()
                    approvals.ShowDialog(Me)
                End Using

            Case Else
                MessageBox.Show(
                    item & " is not connected yet.",
                    "Navigation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information)
        End Select
    End Sub

    Private Async Sub AwaitDashboardRefresh()
        Await LoadDashboardAsync()
    End Sub

    ' =========================================================
    ' LAPSING SCHEDULE
    ' =========================================================

    Private Async Sub ShowLapsingSchedule()
        workspace.Controls.Clear()

        Dim heading As New Label With {
            .Text = "Fixed Asset Lapsing Schedule",
            .Font = New Font("Segoe UI", 22, FontStyle.Bold),
            .ForeColor = deepGreen,
            .Location = New Point(25, 20),
            .AutoSize = True
        }

        workspace.Controls.Add(heading)

        Dim description As New Label With {
            .Text = "Depreciation and net book value as of the selected reporting period end.",
            .Font = New Font("Segoe UI", 10),
            .ForeColor = Color.Gray,
            .Location = New Point(28, 65),
            .AutoSize = True
        }

        workspace.Controls.Add(description)

        Dim periodLabel As New Label With {
            .Text = "Period:",
            .Location = New Point(28, 110),
            .AutoSize = True
        }

        workspace.Controls.Add(periodLabel)

        Dim periodPicker As New ComboBox With {
            .Location = New Point(85, 105),
            .Width = 145,
            .DropDownStyle = ComboBoxStyle.DropDownList
        }

        periodPicker.Items.AddRange(
            New Object() {"Daily", "Weekly", "Monthly", "Yearly"})

        periodPicker.SelectedItem = "Daily"
        workspace.Controls.Add(periodPicker)

        Dim dateLabel As New Label With {
            .Text = "Reference date:",
            .Location = New Point(250, 110),
            .AutoSize = True
        }

        workspace.Controls.Add(dateLabel)

        Dim datePicker As New DateTimePicker With {
            .Location = New Point(355, 105),
            .Width = 180,
            .Format = DateTimePickerFormat.Short,
            .Value = DateTime.Today
        }

        workspace.Controls.Add(datePicker)

        Dim refreshButton As New Button With {
            .Text = "Load Report",
            .Location = New Point(550, 103),
            .Size = New Size(130, 35),
            .BackColor = primaryGreen,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }

        refreshButton.FlatAppearance.BorderSize = 0
        workspace.Controls.Add(refreshButton)

        Dim searchLabel As New Label With {
            .Text = "Search assets:",
            .Location = New Point(28, 157),
            .AutoSize = True
        }

        workspace.Controls.Add(searchLabel)

        Dim searchBox As New TextBox With {
            .Location = New Point(125, 152),
            .Size = New Size(260, 28)
        }

        searchBox.AccessibleName = "Search assets"
        searchBox.AccessibleDescription =
            "Search by asset code, asset name, or category."
        workspace.Controls.Add(searchBox)

        Dim clearSearchButton As New Button With {
            .Text = "Clear Search",
            .Location = New Point(395, 150),
            .Size = New Size(110, 32),
            .BackColor = Color.White,
            .ForeColor = deepGreen,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand
        }

        clearSearchButton.FlatAppearance.BorderColor = primaryGreen
        workspace.Controls.Add(clearSearchButton)

        Dim matchCountLabel As New Label With {
            .Text = "Matching assets: 0",
            .Location = New Point(520, 157),
            .AutoSize = True,
            .ForeColor = deepGreen,
            .Font = New Font("Segoe UI", 9, FontStyle.Bold)
        }

        workspace.Controls.Add(matchCountLabel)

        Dim statusLabel As New Label With {
            .Text = "Choose a reporting period and date.",
            .Location = New Point(28, 190),
            .AutoSize = True,
            .ForeColor = Color.Gray
        }

        workspace.Controls.Add(statusLabel)

        Dim reportGrid As New DataGridView With {
            .Location = New Point(25, 220),
            .Size = New Size(1100, 385),
            .Anchor = AnchorStyles.Top Or
                      AnchorStyles.Left Or
                      AnchorStyles.Right,
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.None,
            .ReadOnly = True,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .RowHeadersVisible = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False
        }

        reportGrid.Columns.Add("asset_code", "Asset Code")
        reportGrid.Columns.Add("name", "Asset Name")
        reportGrid.Columns.Add("category", "Category")
        reportGrid.Columns.Add("acquisition_date", "Acquisition Date")
        reportGrid.Columns.Add("acquisition_cost", "Cost")
        reportGrid.Columns.Add(
            "annual_depreciation", "Annual Depreciation")
        reportGrid.Columns.Add(
            "accumulated_depreciation", "Accumulated Depreciation")
        reportGrid.Columns.Add("net_book_value", "Net Book Value")

        reportGrid.ColumnHeadersDefaultCellStyle.BackColor =
            Color.FromArgb(228, 245, 232)

        reportGrid.ColumnHeadersDefaultCellStyle.ForeColor = deepGreen
        reportGrid.EnableHeadersVisualStyles = False
        reportGrid.ColumnHeadersHeight = 40
        reportGrid.RowTemplate.Height = 32

        workspace.Controls.Add(reportGrid)

        Dim loadReport As Func(Of Task) =
            Async Function()
                Dim period As String =
                    periodPicker.SelectedItem.ToString()

                Dim referenceDate As DateTime =
                    datePicker.Value.Date

                Dim range As (
                    StartDate As DateTime,
                    EndDate As DateTime
                ) = GetReportingRange(period, referenceDate)

                Await LoadLapsingScheduleAsync(
                    period,
                    range.StartDate,
                    range.EndDate,
                    reportGrid,
                    statusLabel,
                    refreshButton)

                FilterLapsingScheduleGrid(
                    reportGrid,
                    searchBox.Text,
                    matchCountLabel)
            End Function

        AddHandler refreshButton.Click,
            Async Sub(sender, e)
                Await loadReport()
            End Sub

        AddHandler periodPicker.SelectedIndexChanged,
            Sub(sender, e)
                If periodPicker.SelectedItem IsNot Nothing Then
                    statusLabel.Text =
                        "Selected period: " &
                        periodPicker.SelectedItem.ToString() &
                        ". Click Load Report to refresh."
                End If
            End Sub

        AddHandler searchBox.TextChanged,
            Sub(sender, e)
                FilterLapsingScheduleGrid(
                    reportGrid,
                    searchBox.Text,
                    matchCountLabel)
            End Sub

        AddHandler clearSearchButton.Click,
            Sub(sender, e)
                searchBox.Clear()
                FilterLapsingScheduleGrid(
                    reportGrid,
                    searchBox.Text,
                    matchCountLabel)
            End Sub

        Await loadReport()
    End Sub

    Private Function GetReportingRange(
        period As String,
        referenceDate As DateTime
    ) As (StartDate As DateTime, EndDate As DateTime)

        Select Case period
            Case "Weekly"
                Dim startDate As DateTime =
                    referenceDate.AddDays(
                        -((CInt(referenceDate.DayOfWeek) + 6) Mod 7))

                Return (startDate, startDate.AddDays(6))

            Case "Monthly"
                Dim startDate As New DateTime(
                    referenceDate.Year,
                    referenceDate.Month,
                    1)

                Return (
                    startDate,
                    startDate.AddMonths(1).AddDays(-1))

            Case "Yearly"
                Return (
                    New DateTime(referenceDate.Year, 1, 1),
                    New DateTime(referenceDate.Year, 12, 31))

            Case Else
                Return (referenceDate.Date, referenceDate.Date)
        End Select
    End Function

    Private Async Function LoadLapsingScheduleAsync(
        period As String,
        startDate As DateTime,
        endDate As DateTime,
        reportGrid As DataGridView,
        statusLabel As Label,
        refreshButton As Button
    ) As Task

        refreshButton.Enabled = False
        statusLabel.Text = "Loading report..."
        reportGrid.Rows.Clear()

        Try
            Dim url As String =
                "reports/lapsing-schedule?as_of=" &
                endDate.ToString(
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture)

            Dim totalCost As Decimal = 0D
            Dim totalAnnual As Decimal = 0D
            Dim totalAccumulated As Decimal = 0D
            Dim totalNetBookValue As Decimal = 0D
            Dim assetCount As Integer = 0

            ApiSession.ApplyAuthorization()

            Using response As HttpResponseMessage =
                Await ApiSession.Client.GetAsync(url)

                Dim json As String =
                    Await response.Content.ReadAsStringAsync()

                If Not response.IsSuccessStatusCode Then
                    Throw New Exception(
                        "API returned HTTP " &
                        CInt(response.StatusCode).ToString() &
                        ": " & json)
                End If

                Using document As JsonDocument =
                    JsonDocument.Parse(json)

                    Dim root As JsonElement = document.RootElement
                    Dim assets As JsonElement

                    If Not root.TryGetProperty("data", assets) OrElse
                       assets.ValueKind <> JsonValueKind.Array Then

                        Throw New Exception(
                            "The API response does not contain a valid data array.")
                    End If

                    For Each asset As JsonElement In assets.EnumerateArray()
                        Dim cost As Decimal =
                            ReadJsonDecimal(asset, "acquisition_cost")

                        Dim annual As Decimal =
                            ReadJsonDecimal(asset, "annual_depreciation")

                        Dim accumulated As Decimal =
                            ReadJsonDecimal(asset, "accumulated_depreciation")

                        Dim netValue As Decimal =
                            ReadJsonDecimal(asset, "net_book_value")

                        totalCost += cost
                        totalAnnual += annual
                        totalAccumulated += accumulated
                        totalNetBookValue += netValue
                        assetCount += 1

                        reportGrid.Rows.Add(
                            ReadJsonText(asset, "asset_code"),
                            ReadJsonText(asset, "name"),
                            ReadJsonText(asset, "category"),
                            ReadJsonText(asset, "acquisition_date"),
                            FormatPeso(cost),
                            FormatPeso(annual),
                            FormatPeso(accumulated),
                            FormatPeso(netValue))
                    Next
                End Using
            End Using

            If assetCount > 0 Then
                Dim totalRowIndex As Integer =
                    reportGrid.Rows.Add(
                        "",
                        "TOTAL",
                        "",
                        "",
                        FormatPeso(totalCost),
                        FormatPeso(totalAnnual),
                        FormatPeso(totalAccumulated),
                        FormatPeso(totalNetBookValue))

                With reportGrid.Rows(totalRowIndex)
                    .DefaultCellStyle.BackColor =
                        Color.FromArgb(228, 245, 232)

                    .DefaultCellStyle.ForeColor = deepGreen

                    .DefaultCellStyle.Font =
                        New Font("Segoe UI", 9, FontStyle.Bold)

                    .DefaultCellStyle.SelectionBackColor =
                        Color.FromArgb(210, 235, 216)

                    .DefaultCellStyle.SelectionForeColor = deepGreen
                End With
            End If

            statusLabel.Text =
                period & " | " &
                startDate.ToString("MMM dd, yyyy") & " - " &
                endDate.ToString("MMM dd, yyyy") &
                " | Depreciation as of " &
                endDate.ToString("MMM dd, yyyy") &
                " | Assets listed: " & assetCount.ToString()

        Catch ex As Exception
            statusLabel.Text = "Unable to load the report."

            MessageBox.Show(
                ex.Message,
                "Lapsing Schedule Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)

        Finally
            refreshButton.Enabled = True
        End Try
    End Function

    Private Sub FilterLapsingScheduleGrid(
        reportGrid As DataGridView,
        searchText As String,
        matchCountLabel As Label)

        Dim query As String = searchText.Trim()
        Dim matchingAssets As Integer = 0

        For Each row As DataGridViewRow In reportGrid.Rows
            If row.IsNewRow Then Continue For

            Dim assetName As String =
                Convert.ToString(row.Cells("name").Value)

            If String.Equals(
                assetName,
                "TOTAL",
                StringComparison.OrdinalIgnoreCase) Then

                row.Visible = True
                Continue For
            End If

            Dim assetCode As String =
                Convert.ToString(row.Cells("asset_code").Value)

            Dim category As String =
                Convert.ToString(row.Cells("category").Value)

            Dim matches As Boolean =
                query.Length = 0 OrElse
                assetCode.IndexOf(
                    query,
                    StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                assetName.IndexOf(
                    query,
                    StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                category.IndexOf(
                    query,
                    StringComparison.OrdinalIgnoreCase) >= 0

            row.Visible = matches

            If matches Then
                matchingAssets += 1
            End If
        Next

        matchCountLabel.Text =
            "Matching assets: " & matchingAssets.ToString()
    End Sub

    ' =========================================================
    ' JSON AND DISPLAY HELPERS
    ' =========================================================

    Private Function ReadJsonText(
        element As JsonElement,
        propertyName As String
    ) As String

        Dim value As JsonElement

        If Not element.TryGetProperty(propertyName, value) OrElse
           value.ValueKind = JsonValueKind.Null Then

            Return ""
        End If

        Return value.ToString()
    End Function

    Private Function ReadJsonDecimal(
        element As JsonElement,
        propertyName As String
    ) As Decimal

        Dim value As JsonElement

        If Not element.TryGetProperty(propertyName, value) OrElse
           value.ValueKind = JsonValueKind.Null Then

            Return 0D
        End If

        Dim result As Decimal

        If Decimal.TryParse(
            value.ToString(),
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            result) Then

            Return result
        End If

        Return 0D
    End Function

    Private Function FormatPeso(amount As Decimal) As String
        Return "₱" & amount.ToString(
            "#,##0.00",
            CultureInfo.GetCultureInfo("en-PH"))
    End Function

    Private Function FormatPeso(value As String) As String
        Dim amount As Decimal

        If Decimal.TryParse(
            value,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            amount) Then

            Return FormatPeso(amount)
        End If

        Return value
    End Function

    Private Sub MakeRounded(control As Control, radius As Integer)
        Dim updateRegion As Action =
            Sub()
                If control.Width <= 0 OrElse control.Height <= 0 Then
                    Return
                End If

                Using path As New GraphicsPath()
                    Dim rect As New Rectangle(
                        0, 0, control.Width, control.Height)

                    Dim diameter As Integer = Math.Min(
                        radius * 2,
                        Math.Min(rect.Width, rect.Height))

                    path.AddArc(
                        rect.Left, rect.Top,
                        diameter, diameter, 180, 90)

                    path.AddArc(
                        rect.Right - diameter, rect.Top,
                        diameter, diameter, 270, 90)

                    path.AddArc(
                        rect.Right - diameter,
                        rect.Bottom - diameter,
                        diameter, diameter, 0, 90)

                    path.AddArc(
                        rect.Left, rect.Bottom - diameter,
                        diameter, diameter, 90, 90)

                    path.CloseFigure()
                    control.Region = New Region(path)
                End Using
            End Sub

        AddHandler control.SizeChanged,
            Sub(sender, e)
                updateRegion()
            End Sub

        updateRegion()
    End Sub

Private Sub HandleAppNotification(
    notification As AppNotifications.NotificationItem)

    If Me.IsDisposed OrElse notification Is Nothing Then Return

    If Me.InvokeRequired Then
        Me.BeginInvoke(
            New Action(
                Sub()
                    HandleAppNotification(notification)
                End Sub))
        Return
    End If

    notificationMessageText =
        notification.Title & ": " & notification.Message

    notificationUnread = True

    If notificationBadge IsNot Nothing Then
        notificationBadge.Text =
            AppNotifications.GetUnreadCount().ToString()
        notificationBadge.Visible = True
    End If

    If notificationMessage IsNot Nothing Then
        notificationMessage.Text = notificationMessageText
    End If

End Sub

Private Async Sub LogoutButton_Click(
    sender As Object,
    e As EventArgs)

    Dim result As DialogResult = MessageBox.Show(
        "Are you sure you want to log out?",
        "Confirm Logout",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Question)

    If result <> DialogResult.Yes Then
        Return
    End If

    Try
        ' Attempt to invalidate the server-side API token.
        ApiSession.ApplyAuthorization()

        Using response As HttpResponseMessage =
            Await ApiSession.Client.PostAsync("logout", Nothing)

            ' Local logout will proceed even if the server
            ' does not provide a working logout endpoint.
        End Using

    Catch ex As Exception
        ' Continue with local logout if the API is unavailable.
    Finally
        ' Clear the token and all authenticated user information.
        ApiSession.Clear()

        ' Close MainForm and return control to LoginForm.
        Me.Close()
    End Try

End Sub

End Class
