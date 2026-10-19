Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Linq
Imports System.Net.Http
Imports System.Text
Imports System.Text.Json
Imports System.Windows.Forms
Imports System.Threading.Tasks

Public Class ChartOfAccountsForm
    Inherits Form

    Private accountsGrid As DataGridView
    Private statusLabel As Label
    Private txtCode As TextBox
    Private txtName As TextBox
    Private txtGroup As TextBox
    Private txtDescription As TextBox
    Private cboType As ComboBox
    Private chkActive As CheckBox

    Private searchInput As TextBox
    Private clearSearchButton As Button
    Private resultCountLabel As Label

    Private createButton As Button
    Private updateButton As Button
    Private deactivateButton As Button
    Private clearButton As Button
    Private refreshButton As Button

    Private allAccounts As New List(Of AccountItem)()
    Private selectedAccountId As Integer = 0
    Private isBusy As Boolean = False
    Private isLoading As Boolean = False

    Private Const ApiUrl As String = "accounts"

    Public Sub New()
        InitializeComponent()
        AddHandler Me.Load, AddressOf Form_Load
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Chart of Accounts - Aqua ARC"
        Me.ClientSize = New Size(1150, 780)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.MinimumSize = New Size(950, 650)
        Me.BackColor = Color.FromArgb(20, 35, 30)
        Me.ForeColor = Color.White
        Me.Font = New Font("Segoe UI", 10)

        Dim title As New Label With {
            .Text = "CHART OF ACCOUNTS",
            .Font = New Font("Segoe UI", 20, FontStyle.Bold),
            .ForeColor = Color.White,
            .AutoSize = True,
            .Location = New Point(20, 15)
        }
        Me.Controls.Add(title)

        Dim subtitle As New Label With {
            .Text = "Create, maintain, and deactivate accounting accounts.",
            .Font = New Font("Segoe UI", 9),
            .ForeColor = Color.LightGreen,
            .AutoSize = True,
            .Location = New Point(22, 44)
        }
        Me.Controls.Add(subtitle)

        Dim formPanel As New Panel With {
            .Location = New Point(20, 75),
            .Size = New Size(1110, 230),
            .BackColor = Color.FromArgb(28, 43, 37),
            .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        }
        Me.Controls.Add(formPanel)

        AddLabel(formPanel, "Account Code *", 15, 15)
        txtCode = AddTextBox(formPanel, 15, 40, 200)

        AddLabel(formPanel, "Account Name *", 235, 15)
        txtName = AddTextBox(formPanel, 235, 40, 300)

        AddLabel(formPanel, "Account Type *", 555, 15)

        cboType = New ComboBox With {
            .Location = New Point(555, 40),
            .Size = New Size(200, 30),
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Font = New Font("Segoe UI", 9.5F)
        }

        cboType.Items.AddRange(New Object() {
            "Asset", "Liability", "Equity", "Revenue", "Expense"
        })
        formPanel.Controls.Add(cboType)

        AddLabel(formPanel, "Account Group", 775, 15)
        txtGroup = AddTextBox(formPanel, 775, 40, 300)

        AddLabel(formPanel, "Description", 15, 85)
        txtDescription = AddTextBox(formPanel, 15, 110, 700)
        txtDescription.Height = 50
        txtDescription.Multiline = True
        txtDescription.ScrollBars = ScrollBars.Vertical

        chkActive = New CheckBox With {
            .Text = "Active",
            .Checked = True,
            .Location = New Point(740, 120),
            .AutoSize = True,
            .ForeColor = Color.White
        }
        formPanel.Controls.Add(chkActive)

        createButton = AddButton(
            formPanel, "Create Account", 15, 175, 155,
            AddressOf CreateButton_Click)

        updateButton = AddButton(
            formPanel, "Update Selected", 180, 175, 155,
            AddressOf UpdateButton_Click)

        deactivateButton = AddButton(
            formPanel, "Deactivate Selected", 345, 175, 175,
            AddressOf DeactivateButton_Click)

        clearButton = AddButton(
            formPanel, "Clear Form", 530, 175, 120,
            AddressOf ClearButton_Click)

        refreshButton = AddButton(
            formPanel, "Refresh List", 660, 175, 120,
            AddressOf RefreshButton_Click)

        ' Search controls
        searchInput = New TextBox With {
            .Location = New Point(20, 318),
            .Size = New Size(500, 30),
            .Font = New Font("Segoe UI", 10),
            .PlaceholderText = "Search code, account name, type, group, or description..."
        }
        AddHandler searchInput.TextChanged, AddressOf SearchInput_TextChanged
        Me.Controls.Add(searchInput)

        clearSearchButton = New Button With {
            .Text = "Clear Search",
            .Location = New Point(530, 316),
            .Size = New Size(120, 32),
            .BackColor = Color.FromArgb(65, 90, 78),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand,
            .Font = New Font("Segoe UI", 9, FontStyle.Bold)
        }
        clearSearchButton.FlatAppearance.BorderSize = 0
        AddHandler clearSearchButton.Click, AddressOf ClearSearchButton_Click
        Me.Controls.Add(clearSearchButton)

        resultCountLabel = New Label With {
            .Text = "0 accounts",
            .Location = New Point(665, 321),
            .Size = New Size(465, 25),
            .ForeColor = Color.LightGreen,
            .AutoEllipsis = True,
            .TextAlign = ContentAlignment.MiddleRight,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right
        }
        Me.Controls.Add(resultCountLabel)

        statusLabel = New Label With {
            .Text = "Ready",
            .Location = New Point(20, 352),
            .Size = New Size(1110, 22),
            .ForeColor = Color.LightGreen,
            .AutoEllipsis = True,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        }
        Me.Controls.Add(statusLabel)

        accountsGrid = New DataGridView With {
            .Location = New Point(20, 382),
            .Size = New Size(1110, 373),
            .Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or
                      AnchorStyles.Left Or AnchorStyles.Right,
            .BackgroundColor = Color.White,
            .ForeColor = Color.Black,
            .ReadOnly = True,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False,
            .RowHeadersVisible = False,
            .AutoGenerateColumns = False
        }

        AddGridColumn("id", "ID", "id", 8, False)
        AddGridColumn("account_code", "Account Code", "account_code", 18)
        AddGridColumn("account_name", "Account Name", "account_name", 28)
        AddGridColumn("account_type", "Account Type", "account_type", 16)
        AddGridColumn("account_group", "Account Group", "account_group", 20)
        AddGridColumn("is_active", "Active", "is_active", 10)

        accountsGrid.ColumnHeadersDefaultCellStyle.BackColor =
            Color.FromArgb(35, 130, 85)
        accountsGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
        accountsGrid.ColumnHeadersDefaultCellStyle.Font =
            New Font("Segoe UI", 10, FontStyle.Bold)
        accountsGrid.EnableHeadersVisualStyles = False
        accountsGrid.AlternatingRowsDefaultCellStyle.BackColor =
            Color.FromArgb(235, 242, 237)

        accountsGrid.DefaultCellStyle.SelectionBackColor =
            Color.FromArgb(198, 232, 211)
        accountsGrid.DefaultCellStyle.SelectionForeColor = Color.Black

        AddHandler accountsGrid.SelectionChanged,
            AddressOf AccountsGrid_SelectionChanged

        Me.Controls.Add(accountsGrid)

        updateButton.Enabled = False
        deactivateButton.Enabled = False
    End Sub

    Private Sub AddLabel(
        parent As Control,
        caption As String,
        x As Integer,
        y As Integer)

        Dim lbl As New Label With {
            .Text = caption,
            .Location = New Point(x, y),
            .AutoSize = True,
            .ForeColor = Color.White
        }

        parent.Controls.Add(lbl)
    End Sub

    Private Function AddTextBox(
        parent As Control,
        x As Integer,
        y As Integer,
        width As Integer) As TextBox

        Dim txt As New TextBox With {
            .Location = New Point(x, y),
            .Size = New Size(width, 30),
            .Font = New Font("Segoe UI", 9.5F)
        }

        parent.Controls.Add(txt)
        Return txt
    End Function

    Private Function AddButton(
        parent As Control,
        caption As String,
        x As Integer,
        y As Integer,
        width As Integer,
        handler As EventHandler) As Button

        Dim btn As New Button With {
            .Text = caption,
            .Location = New Point(x, y),
            .Size = New Size(width, 35),
            .BackColor = Color.FromArgb(35, 130, 85),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Cursor = Cursors.Hand,
            .Font = New Font("Segoe UI", 8.5F, FontStyle.Bold)
        }

        btn.FlatAppearance.BorderSize = 0
        AddHandler btn.Click, handler
        parent.Controls.Add(btn)

        Return btn
    End Function

    Private Sub AddGridColumn(
        columnName As String,
        header As String,
        propertyName As String,
        fill As Integer,
        Optional visible As Boolean = True)

        accountsGrid.Columns.Add(
            New DataGridViewTextBoxColumn With {
                .Name = columnName,
                .HeaderText = header,
                .DataPropertyName = propertyName,
                .FillWeight = fill,
                .Visible = visible
            })
    End Sub

    Private Async Sub Form_Load(sender As Object, e As EventArgs)
        Await LoadAccountsAsync()
    End Sub

    Private Async Function LoadAccountsAsync() As Task
        If isLoading OrElse isBusy Then Return

        isLoading = True
        refreshButton.Enabled = False
        statusLabel.Text = "Loading accounts..."

        Try
            ApiSession.ApplyAuthorization()

            Using response As HttpResponseMessage =
                Await ApiSession.Client.GetAsync(ApiUrl)

                Dim json As String =
                    Await response.Content.ReadAsStringAsync()

                If Not response.IsSuccessStatusCode Then
                    Throw New Exception(
                        "The API returned HTTP " &
                        CInt(response.StatusCode).ToString() & "." &
                        Environment.NewLine & json)
                End If

                Dim options As New JsonSerializerOptions With {
                    .PropertyNameCaseInsensitive = True
                }

                Dim result As AccountsResponse =
                    JsonSerializer.Deserialize(Of AccountsResponse)(
                        json, options)

                If result IsNot Nothing AndAlso result.data IsNot Nothing Then
                    allAccounts = result.data
                    ApplyAccountSearch()
                    statusLabel.Text =
                        allAccounts.Count.ToString() & " account(s) loaded."
                Else
                    allAccounts.Clear()
                    ApplyAccountSearch()
                    statusLabel.Text = "The API returned no account data."
                End If
            End Using

        Catch ex As Exception
            statusLabel.Text = "Unable to load accounts."

            MessageBox.Show(
                "Unable to load the chart of accounts." &
                Environment.NewLine & Environment.NewLine &
                ex.Message & Environment.NewLine & Environment.NewLine &
                "Check your internet connection and the deployed Laravel API on Render.",
                "Aqua ARC - API Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)
        Finally
            isLoading = False
            refreshButton.Enabled = True
            UpdateSelectionButtons()
        End Try
    End Function

    Private Sub SearchInput_TextChanged(sender As Object, e As EventArgs)
        ApplyAccountSearch()
    End Sub

    Private Sub ClearSearchButton_Click(sender As Object, e As EventArgs)
        searchInput.Clear()
        searchInput.Focus()
    End Sub

    Private Sub ApplyAccountSearch()
        If accountsGrid Is Nothing Then Return

        Dim query As String = If(searchInput Is Nothing, "", searchInput.Text.Trim())

        Dim filteredAccounts As List(Of AccountItem)

        If String.IsNullOrWhiteSpace(query) Then
            filteredAccounts = allAccounts.ToList()
        Else
            filteredAccounts = allAccounts.Where(
                Function(account)
                    Return ContainsText(account.account_code, query) OrElse
                           ContainsText(account.account_name, query) OrElse
                           ContainsText(account.account_type, query) OrElse
                           ContainsText(account.account_group, query) OrElse
                           ContainsText(account.description, query) OrElse
                           ContainsText(
                               If(account.is_active, "Active", "Inactive"),
                               query)
                End Function).ToList()
        End If

        accountsGrid.DataSource = Nothing
        accountsGrid.DataSource = filteredAccounts

        For Each row As DataGridViewRow In accountsGrid.Rows
            Dim account As AccountItem =
                TryCast(row.DataBoundItem, AccountItem)

            If account IsNot Nothing AndAlso Not account.is_active Then
                row.DefaultCellStyle.ForeColor = Color.Gray
            End If
        Next

        resultCountLabel.Text =
            filteredAccounts.Count.ToString() & " of " &
            allAccounts.Count.ToString() & " account(s)"

        If filteredAccounts.Count = 0 Then
            statusLabel.Text = "No accounts match your search."
        ElseIf Not String.IsNullOrWhiteSpace(query) Then
            statusLabel.Text = "Search results for: " & query
        End If

        ' Changing the displayed rows must not leave a stale selection.
        selectedAccountId = 0
        UpdateSelectionButtons()
    End Sub

    Private Function ContainsText(value As String, query As String) As Boolean
        Return Not String.IsNullOrEmpty(value) AndAlso
               value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
    End Function

    Private Sub AccountsGrid_SelectionChanged(sender As Object, e As EventArgs)
        If accountsGrid.CurrentRow Is Nothing OrElse
           accountsGrid.CurrentRow.DataBoundItem Is Nothing Then
            Return
        End If

        Dim item As AccountItem =
            TryCast(accountsGrid.CurrentRow.DataBoundItem, AccountItem)

        If item Is Nothing Then Return

        selectedAccountId = item.id
        txtCode.Text = If(item.account_code, "")
        txtName.Text = If(item.account_name, "")
        txtGroup.Text = If(item.account_group, "")
        txtDescription.Text = If(item.description, "")

        cboType.SelectedItem = item.account_type
        chkActive.Checked = item.is_active

        statusLabel.Text = "Selected account: " & item.account_code
        UpdateSelectionButtons()
    End Sub

    Private Async Sub CreateButton_Click(sender As Object, e As EventArgs)
        If Not ValidateForm() Then Return

        Dim payload = New With {
            .account_code = txtCode.Text.Trim(),
            .account_name = txtName.Text.Trim(),
            .account_type = cboType.Text,
            .account_group = NullIfBlank(txtGroup.Text),
            .description = NullIfBlank(txtDescription.Text),
            .is_active = chkActive.Checked
        }

        Await SendAccountRequestAsync(
            HttpMethod.Post,
            ApiUrl,
            payload,
            "Account created successfully.")
    End Sub

    Private Async Sub UpdateButton_Click(sender As Object, e As EventArgs)
        If selectedAccountId <= 0 Then
            ShowValidation("Select an account from the table first.")
            Return
        End If

        If Not ValidateForm() Then Return

        Dim payload = New With {
            .account_code = txtCode.Text.Trim(),
            .account_name = txtName.Text.Trim(),
            .account_type = cboType.Text,
            .account_group = NullIfBlank(txtGroup.Text),
            .description = NullIfBlank(txtDescription.Text),
            .is_active = chkActive.Checked
        }

        Await SendAccountRequestAsync(
            HttpMethod.Put,
            ApiUrl & "/" & selectedAccountId.ToString(),
            payload,
            "Account updated successfully.")
    End Sub

    Private Async Sub DeactivateButton_Click(sender As Object, e As EventArgs)
        If selectedAccountId <= 0 Then
            ShowValidation("Select an account from the table first.")
            Return
        End If

        If Not chkActive.Checked Then
            MessageBox.Show(
                "This account is already marked inactive.",
                "Aqua ARC",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information)
            Return
        End If

        Dim answer As DialogResult = MessageBox.Show(
            "Deactivate this account?" & Environment.NewLine &
            Environment.NewLine &
            "Account: " & txtCode.Text.Trim() & " - " &
            txtName.Text.Trim() & Environment.NewLine &
            Environment.NewLine &
            "The account record will remain in the database for " &
            "accounting history. This action does not permanently delete it." &
            Environment.NewLine & Environment.NewLine &
            "Continue?",
            "Confirm Account Deactivation",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2)

        If answer <> DialogResult.Yes Then
            statusLabel.Text = "Deactivation cancelled."
            Return
        End If

        Dim payload = New With {
            .account_code = txtCode.Text.Trim(),
            .account_name = txtName.Text.Trim(),
            .account_type = cboType.Text,
            .account_group = NullIfBlank(txtGroup.Text),
            .description = NullIfBlank(txtDescription.Text),
            .is_active = False
        }

        Await SendAccountRequestAsync(
            HttpMethod.Put,
            ApiUrl & "/" & selectedAccountId.ToString(),
            payload,
            "Account deactivated successfully.")
    End Sub

    Private Async Function SendAccountRequestAsync(
        method As HttpMethod,
        url As String,
        payload As Object,
        successMessage As String
    ) As Task

        If isBusy Then Return

        isBusy = True
        SetButtonsEnabled(False)

        Try
            statusLabel.Text = "Saving account changes..."

            Dim json As String = JsonSerializer.Serialize(payload)

            ApiSession.ApplyAuthorization()

            Using request As New HttpRequestMessage(method, url)
                request.Content = New StringContent(
                    json, Encoding.UTF8, "application/json")

                Using response As HttpResponseMessage =
                    Await ApiSession.Client.SendAsync(request)

                    Dim responseText As String =
                        Await response.Content.ReadAsStringAsync()

                    If Not response.IsSuccessStatusCode Then
                        Throw New Exception(
                            "Request failed with HTTP " &
                            CInt(response.StatusCode).ToString() & "." &
                            Environment.NewLine & responseText)
                    End If
                End Using
            End Using

            statusLabel.Text = successMessage
            ClearForm()

            MessageBox.Show(
                successMessage,
                "Aqua ARC - Account Management",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information)

        Catch ex As Exception
            statusLabel.Text = "Account operation failed."

            MessageBox.Show(
                "The account operation could not be completed." &
                Environment.NewLine & Environment.NewLine &
                ex.Message,
                "Aqua ARC - Account Management",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)
        Finally
            isBusy = False
            SetButtonsEnabled(True)
        End Try

        Await LoadAccountsAsync()
    End Function

    Private Function ValidateForm() As Boolean
        If String.IsNullOrWhiteSpace(txtCode.Text) Then
            ShowValidation("Enter an account code.")
            txtCode.Focus()
            Return False
        End If

        If String.IsNullOrWhiteSpace(txtName.Text) Then
            ShowValidation("Enter an account name.")
            txtName.Focus()
            Return False
        End If

        If cboType.SelectedIndex < 0 Then
            ShowValidation("Select an account type.")
            cboType.Focus()
            Return False
        End If

        Return True
    End Function

    Private Function NullIfBlank(value As String) As String
        If String.IsNullOrWhiteSpace(value) Then Return Nothing
        Return value.Trim()
    End Function

    Private Sub ClearButton_Click(sender As Object, e As EventArgs)
        ClearForm()
    End Sub

    Private Async Sub RefreshButton_Click(sender As Object, e As EventArgs)
        ClearForm()
        Await LoadAccountsAsync()
    End Sub

    Private Sub ClearForm()
        selectedAccountId = 0

        txtCode.Clear()
        txtName.Clear()
        txtGroup.Clear()
        txtDescription.Clear()

        cboType.SelectedIndex = -1
        chkActive.Checked = True

        accountsGrid.ClearSelection()

        statusLabel.Text = "Ready for a new account."
        UpdateSelectionButtons()
    End Sub

    Private Sub UpdateSelectionButtons()
        If updateButton Is Nothing OrElse
           deactivateButton Is Nothing OrElse
           createButton Is Nothing OrElse
           clearButton Is Nothing OrElse
           refreshButton Is Nothing Then
            Return
        End If

        Dim hasSelection As Boolean = selectedAccountId > 0

        updateButton.Enabled = hasSelection AndAlso
                               Not isBusy AndAlso
                               Not isLoading

        deactivateButton.Enabled = hasSelection AndAlso
                                   chkActive.Checked AndAlso
                                   Not isBusy AndAlso
                                   Not isLoading

        createButton.Enabled = Not isBusy AndAlso Not isLoading
        clearButton.Enabled = Not isBusy AndAlso Not isLoading
        refreshButton.Enabled = Not isBusy AndAlso Not isLoading
    End Sub

    Private Sub SetButtonsEnabled(enabled As Boolean)
        createButton.Enabled = enabled AndAlso Not isLoading
        updateButton.Enabled = enabled AndAlso
                               selectedAccountId > 0 AndAlso
                               Not isLoading
        deactivateButton.Enabled = enabled AndAlso
                                   selectedAccountId > 0 AndAlso
                                   chkActive.Checked AndAlso
                                   Not isLoading
        clearButton.Enabled = enabled AndAlso Not isLoading
        refreshButton.Enabled = enabled AndAlso Not isLoading
    End Sub

    Private Sub ShowValidation(message As String)
        MessageBox.Show(
            message,
            "Check Account Information",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning)
    End Sub

End Class

Public Class AccountsResponse
    Public Property success As Boolean
    Public Property data As List(Of AccountItem)
End Class

Public Class AccountItem
    Public Property id As Integer
    Public Property account_code As String
    Public Property account_name As String
    Public Property account_type As String
    Public Property account_group As String
    Public Property description As String
    Public Property is_active As Boolean
End Class