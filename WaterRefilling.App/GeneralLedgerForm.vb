Imports System.Net.Http
Imports System.Text.Json
Imports System.Drawing
Imports System.Linq
Imports System.Threading.Tasks
Imports System.Windows.Forms

Public Class GeneralLedgerForm
    Inherits Form

    Private ReadOnly accountCombo As New ComboBox()
    Private ReadOnly startDatePicker As New DateTimePicker()
    Private ReadOnly endDatePicker As New DateTimePicker()
    Private ReadOnly searchButton As New Button()
    Private ReadOnly clearSearchButton As New Button()
    Private ReadOnly searchInput As New TextBox()
    Private ReadOnly resultCountLabel As New Label()
    Private ReadOnly accountsGrid As New DataGridView()
    Private ReadOnly entriesGrid As New DataGridView()
    Private ReadOnly statusLabel As New Label()

    Private ReadOnly jsonOptions As New JsonSerializerOptions With {
        .PropertyNameCaseInsensitive = True,
        .PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    }

    Private accounts As New List(Of LedgerAccountItem)()
    Private displayedAccounts As New List(Of LedgerAccountItem)()
    Private selectedAccountCode As String = Nothing
    Private isLoading As Boolean = False

    Public Sub New()
        Me.Text = "General Ledger"
        Me.Size = New Size(1200, 780)
        Me.MinimumSize = New Size(950, 600)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.FromArgb(245, 249, 246)
        Me.Font = New Font("Segoe UI", 9.0F)

        BuildInterface()

        AddHandler Me.Load, AddressOf LoadAccountsAsync
        AddHandler searchButton.Click, AddressOf SearchButtonClicked
        AddHandler clearSearchButton.Click, AddressOf ClearSearchButtonClicked
        AddHandler searchInput.TextChanged, AddressOf SearchInput_TextChanged
        AddHandler searchInput.KeyDown, AddressOf SearchInput_KeyDown
        AddHandler accountsGrid.SelectionChanged, AddressOf AccountSelectionChanged
    End Sub

    Private Sub BuildInterface()
        Dim root As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 5,
            .Padding = New Padding(16),
            .BackColor = Me.BackColor
        }

        root.RowStyles.Add(New RowStyle(SizeType.Absolute, 55))
        root.RowStyles.Add(New RowStyle(SizeType.Absolute, 100))
        root.RowStyles.Add(New RowStyle(SizeType.Absolute, 42))
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 48))
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 52))

        Dim title As New Label With {
            .Text = "General Ledger",
            .Dock = DockStyle.Fill,
            .Font = New Font("Segoe UI", 20, FontStyle.Bold),
            .ForeColor = Color.FromArgb(37, 91, 64),
            .TextAlign = ContentAlignment.MiddleLeft
        }

        Dim filters As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 4,
            .RowCount = 2,
            .Padding = New Padding(0, 5, 0, 5)
        }

        filters.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 40))
        filters.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 20))
        filters.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 20))
        filters.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 20))

        filters.RowStyles.Add(New RowStyle(SizeType.Absolute, 25))
        filters.RowStyles.Add(New RowStyle(SizeType.Absolute, 35))

        Dim accountLabel As New Label With {
            .Text = "Account",
            .Dock = DockStyle.Fill
        }

        Dim startLabel As New Label With {
            .Text = "Start Date",
            .Dock = DockStyle.Fill
        }

        Dim endLabel As New Label With {
            .Text = "End Date",
            .Dock = DockStyle.Fill
        }

        accountCombo.Dock = DockStyle.Fill
        accountCombo.DropDownStyle = ComboBoxStyle.DropDownList

        startDatePicker.Dock = DockStyle.Fill
        startDatePicker.Format = DateTimePickerFormat.Short
        startDatePicker.Value = New Date(Date.Today.Year, 1, 1)

        endDatePicker.Dock = DockStyle.Fill
        endDatePicker.Format = DateTimePickerFormat.Short
        endDatePicker.Value = Date.Today

        searchButton.Text = "Load Ledger"
        searchButton.Dock = DockStyle.Fill
        searchButton.BackColor = Color.FromArgb(45, 125, 82)
        searchButton.ForeColor = Color.White
        searchButton.FlatStyle = FlatStyle.Flat
        searchButton.FlatAppearance.BorderSize = 0
        searchButton.Cursor = Cursors.Hand

        filters.Controls.Add(accountLabel, 0, 0)
        filters.Controls.Add(startLabel, 1, 0)
        filters.Controls.Add(endLabel, 2, 0)

        filters.Controls.Add(accountCombo, 0, 1)
        filters.Controls.Add(startDatePicker, 1, 1)
        filters.Controls.Add(endDatePicker, 2, 1)
        filters.Controls.Add(searchButton, 3, 1)

        Dim searchPanel As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 3,
            .RowCount = 1,
            .Padding = New Padding(0, 4, 0, 4)
        }

        searchPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        searchPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 110))
        searchPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 180))

        searchInput.Dock = DockStyle.Fill
        searchInput.Font = New Font("Segoe UI", 9.5F)
        searchInput.PlaceholderText =
            "Search account, journal number, or description..."

        clearSearchButton.Text = "Clear Search"
        clearSearchButton.Dock = DockStyle.Fill
        clearSearchButton.BackColor = Color.FromArgb(100, 116, 106)
        clearSearchButton.ForeColor = Color.White
        clearSearchButton.FlatStyle = FlatStyle.Flat
        clearSearchButton.FlatAppearance.BorderSize = 0
        clearSearchButton.Cursor = Cursors.Hand

        resultCountLabel.Dock = DockStyle.Fill
        resultCountLabel.TextAlign = ContentAlignment.MiddleRight
        resultCountLabel.ForeColor = Color.FromArgb(55, 90, 65)
        resultCountLabel.Text = "0 accounts | 0 entries"
        resultCountLabel.AutoEllipsis = True

        searchPanel.Controls.Add(searchInput, 0, 0)
        searchPanel.Controls.Add(clearSearchButton, 1, 0)
        searchPanel.Controls.Add(resultCountLabel, 2, 0)

        ConfigureGrid(accountsGrid)
        ConfigureGrid(entriesGrid)

        statusLabel.Dock = DockStyle.Fill
        statusLabel.TextAlign = ContentAlignment.MiddleLeft
        statusLabel.ForeColor = Color.FromArgb(55, 90, 65)
        statusLabel.Text = "Choose an account and load the ledger."
        statusLabel.AutoEllipsis = True

        Dim accountPanel As New Panel With {.Dock = DockStyle.Fill}

        Dim accountTitle As New Label With {
            .Text = "Account Balances",
            .Dock = DockStyle.Top,
            .Height = 28,
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .ForeColor = Color.FromArgb(37, 91, 64)
        }

        accountPanel.Controls.Add(accountsGrid)
        accountPanel.Controls.Add(accountTitle)
        accountsGrid.BringToFront()

        Dim entryPanel As New Panel With {.Dock = DockStyle.Fill}

        Dim entryTitle As New Label With {
            .Text = "Posted Transactions",
            .Dock = DockStyle.Top,
            .Height = 28,
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .ForeColor = Color.FromArgb(37, 91, 64)
        }

        entryPanel.Controls.Add(entriesGrid)
        entryPanel.Controls.Add(entryTitle)
        entriesGrid.BringToFront()

        root.Controls.Add(title, 0, 0)
        root.Controls.Add(filters, 0, 1)
        root.Controls.Add(searchPanel, 0, 2)
        root.Controls.Add(accountPanel, 0, 3)
        root.Controls.Add(entryPanel, 0, 4)

        Me.Controls.Add(root)

        Dim footer As New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 28,
            .BackColor = Me.BackColor
        }

        footer.Controls.Add(statusLabel)
        Me.Controls.Add(footer)
        footer.BringToFront()
    End Sub

    Private Sub ConfigureGrid(grid As DataGridView)
        grid.Dock = DockStyle.Fill
        grid.ReadOnly = True
        grid.AllowUserToAddRows = False
        grid.AllowUserToDeleteRows = False
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        grid.MultiSelect = False
        grid.RowHeadersVisible = False
        grid.BackgroundColor = Color.White
        grid.BorderStyle = BorderStyle.FixedSingle
        grid.EnableHeadersVisualStyles = False
        grid.ColumnHeadersDefaultCellStyle.BackColor =
            Color.FromArgb(224, 240, 229)
        grid.ColumnHeadersDefaultCellStyle.ForeColor =
            Color.FromArgb(35, 70, 48)
        grid.ColumnHeadersDefaultCellStyle.Font =
            New Font("Segoe UI", 9, FontStyle.Bold)
        grid.AlternatingRowsDefaultCellStyle.BackColor =
            Color.FromArgb(248, 251, 249)
    End Sub

    Private Async Sub LoadAccountsAsync(
        sender As Object,
        e As EventArgs)

        Await LoadLedgerAsync()
    End Sub

    Private Async Sub SearchButtonClicked(
        sender As Object,
        e As EventArgs)

        Await LoadLedgerAsync()
    End Sub

    Private Sub SearchInput_KeyDown(
        sender As Object,
        e As KeyEventArgs)

        If e.KeyCode <> Keys.Enter Then Return

        e.SuppressKeyPress = True
        ApplySearch()
    End Sub

    Private Sub SearchInput_TextChanged(
        sender As Object,
        e As EventArgs)

        ApplySearch()
    End Sub

    Private Sub ClearSearchButtonClicked(
        sender As Object,
        e As EventArgs)

        searchInput.Clear()
        searchInput.Focus()
        ApplySearch()
    End Sub

    Private Async Function LoadLedgerAsync() As Task
        If isLoading Then Return

        If endDatePicker.Value.Date < startDatePicker.Value.Date Then
            MessageBox.Show(
                "End date must be on or after start date.",
                "Invalid Date Range",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning)
            Return
        End If

        isLoading = True
        searchButton.Enabled = False
        statusLabel.Text = "Loading general ledger..."

        Try
            ApiSession.ApplyAuthorization()

            Dim url As String =
                "general-ledger?start_date=" &
                startDatePicker.Value.ToString("yyyy-MM-dd") &
                "&end_date=" &
                endDatePicker.Value.ToString("yyyy-MM-dd")

            If accountCombo.SelectedValue IsNot Nothing Then
                Dim accountId As Integer

                If Integer.TryParse(
                    accountCombo.SelectedValue.ToString(),
                    accountId) AndAlso accountId > 0 Then

                    url &= "&account_id=" & accountId.ToString()
                End If
            End If

            Using response As HttpResponseMessage =
                Await ApiSession.Client.GetAsync(url)

                Dim body As String =
                    Await response.Content.ReadAsStringAsync()

                If Not response.IsSuccessStatusCode Then
                    statusLabel.Text =
                        "API error: " &
                        CInt(response.StatusCode).ToString() &
                        " - " & body
                    Return
                End If

                Dim result =
                    JsonSerializer.Deserialize(
                        Of ApiResponse(Of List(Of LedgerAccountItem)))(
                            body, jsonOptions)

                If result Is Nothing OrElse result.Data Is Nothing Then
                    accounts = New List(Of LedgerAccountItem)()
                    displayedAccounts = New List(Of LedgerAccountItem)()
                    accountsGrid.DataSource = Nothing
                    entriesGrid.DataSource = Nothing
                    UpdateResultCount()
                    statusLabel.Text = "The API returned no ledger data."
                    Return
                End If

                accounts = result.Data

                ' Populate the account selector once from the API response.
                If accountCombo.DataSource Is Nothing Then
                    Dim accountChoices = accounts.Select(
                        Function(a) New AccountChoice With {
                            .Id = a.AccountId,
                            .DisplayName =
                                a.AccountCode & " - " & a.AccountName
                        }).ToList()

                    accountCombo.DataSource = accountChoices
                    accountCombo.DisplayMember = "DisplayName"
                    accountCombo.ValueMember = "Id"
                End If

                ApplySearch()

                statusLabel.Text =
                    $"Loaded ledger for " &
                    $"{startDatePicker.Value:yyyy-MM-dd} to " &
                    $"{endDatePicker.Value:yyyy-MM-dd}."
            End Using

        Catch ex As Exception
            statusLabel.Text = "Error: " & ex.Message
        Finally
            isLoading = False
            searchButton.Enabled = True
        End Try
    End Function

    Private Sub ApplySearch()
        Dim query As String = searchInput.Text.Trim()

        If String.IsNullOrWhiteSpace(query) Then
            displayedAccounts = accounts.ToList()
        Else
            displayedAccounts = accounts.Where(
                Function(a)
                    Return ContainsText(a.AccountCode, query) OrElse
                        ContainsText(a.AccountName, query) OrElse
                        ContainsText(a.AccountType, query) OrElse
                        ContainsText(a.AccountId.ToString(), query) OrElse
                        ContainsText(a.OpeningBalance, query) OrElse
                        ContainsText(a.TotalDebits, query) OrElse
                        ContainsText(a.TotalCredits, query) OrElse
                        ContainsText(a.ClosingBalance, query) OrElse
                    (a.Entries IsNot Nothing AndAlso
                            a.Entries.Any(
                                Function(entry)
                                    Return ContainsText(entry.JournalNumber, query) OrElse
                                        ContainsText(entry.JournalDate, query) OrElse
                                        ContainsText(entry.EntryDescription, query) OrElse
                                        ContainsText(entry.LineDescription, query)
                                End Function))
                End Function).ToList()
        End If

        Dim accountRows = displayedAccounts.Select(
            Function(a) New With {
                .AccountCode = a.AccountCode,
                .AccountName = a.AccountName,
                .AccountType = a.AccountType,
                .TotalDebits = ToDecimal(a.TotalDebits),
                .TotalCredits = ToDecimal(a.TotalCredits),
                .ClosingBalance = ToDecimal(a.ClosingBalance)
            }).ToList()

        accountsGrid.DataSource = Nothing
        accountsGrid.DataSource = accountRows

        SetHeader(accountsGrid, "AccountCode", "Account Code")
        SetHeader(accountsGrid, "AccountName", "Account Name")
        SetHeader(accountsGrid, "AccountType", "Account Type")
        FormatMoneyColumn(accountsGrid, "TotalDebits", "Total Debits")
        FormatMoneyColumn(accountsGrid, "TotalCredits", "Total Credits")
        FormatMoneyColumn(accountsGrid, "ClosingBalance", "Closing Balance")

        If displayedAccounts.Count > 0 Then
            Dim targetAccount As LedgerAccountItem = Nothing

            If Not String.IsNullOrWhiteSpace(selectedAccountCode) Then
                targetAccount = displayedAccounts.FirstOrDefault(
                    Function(a) a.AccountCode = selectedAccountCode)
            End If

            If targetAccount Is Nothing Then
                targetAccount = displayedAccounts(0)
            End If

            selectedAccountCode = targetAccount.AccountCode

            For Each row As DataGridViewRow In accountsGrid.Rows
                If row.Cells("AccountCode").Value IsNot Nothing AndAlso
                   row.Cells("AccountCode").Value.ToString() =
                       targetAccount.AccountCode Then

                    row.Selected = True

                    If row.Cells.Count > 0 Then
                        accountsGrid.CurrentCell = row.Cells(0)
                    End If

                    Exit For
                End If
            Next

            ShowSelectedAccountEntries()
        Else
            selectedAccountCode = Nothing
            entriesGrid.DataSource = Nothing
        End If

        UpdateResultCount()
    End Sub

    Private Sub UpdateResultCount()
        Dim totalEntries As Integer = 0
        Dim query As String = searchInput.Text.Trim()

        For Each account As LedgerAccountItem In displayedAccounts
            If account.Entries Is Nothing Then Continue For

            If String.IsNullOrWhiteSpace(query) Then
                totalEntries += account.Entries.Count
            Else
                totalEntries += account.Entries.Where(
                    Function(entry)
                        Return ContainsText(account.AccountCode, query) OrElse
                               ContainsText(account.AccountName, query) OrElse
                               ContainsText(entry.JournalNumber, query) OrElse
                               ContainsText(entry.JournalDate, query) OrElse
                               ContainsText(entry.EntryDescription, query) OrElse
                               ContainsText(entry.LineDescription, query)
                    End Function).Count()
            End If
        Next

        resultCountLabel.Text =
            displayedAccounts.Count.ToString() & " account(s) | " &
            totalEntries.ToString() & " matching entry/entries"
    End Sub

    Private Sub AccountSelectionChanged(
        sender As Object,
        e As EventArgs)

        If accountsGrid.CurrentRow Is Nothing Then Return
        If Not accountsGrid.Columns.Contains("AccountCode") Then Return

        Dim codeValue = accountsGrid.CurrentRow.Cells("AccountCode").Value
        If codeValue Is Nothing Then Return

        selectedAccountCode = codeValue.ToString()
        ShowSelectedAccountEntries()
    End Sub

    Private Sub ShowSelectedAccountEntries()
        If accountsGrid.CurrentRow Is Nothing Then Return
        If Not accountsGrid.Columns.Contains("AccountCode") Then Return

        Dim codeValue = accountsGrid.CurrentRow.Cells("AccountCode").Value
        If codeValue Is Nothing Then Return

        Dim code As String = codeValue.ToString()

        Dim selectedAccount = displayedAccounts.FirstOrDefault(
            Function(a) a.AccountCode = code)

        If selectedAccount Is Nothing Then Return

        selectedAccountCode = selectedAccount.AccountCode

        Dim query As String = searchInput.Text.Trim()
        Dim entries As List(Of LedgerEntryItem) =
            If(selectedAccount.Entries, New List(Of LedgerEntryItem)())

        Dim filteredEntries As List(Of LedgerEntryItem) = entries

        If Not String.IsNullOrWhiteSpace(query) Then
            filteredEntries = entries.Where(
                Function(entry)
                    Return ContainsText(selectedAccount.AccountCode, query) OrElse
                        ContainsText(selectedAccount.AccountName, query) OrElse
                        ContainsText(entry.JournalNumber, query) OrElse
                        ContainsText(entry.JournalDate, query) OrElse
                        ContainsText(entry.EntryDescription, query) OrElse
                        ContainsText(entry.LineDescription, query)
                End Function).ToList()
        End If

        Dim entryRows = filteredEntries.Select(
                Function(entry) New With {
                    .Date = FormatLedgerDate(entry.JournalDate),
                    .JournalNumber = entry.JournalNumber,
                    .Description = entry.EntryDescription,
                    .LineDescription = entry.LineDescription,
                    .Debit = ToDecimal(entry.Debit),
                    .Credit = ToDecimal(entry.Credit),
                    .RunningBalance = ToDecimal(entry.RunningBalance)
    }).ToList()

        entriesGrid.DataSource = Nothing
        entriesGrid.DataSource = entryRows

        SetHeader(entriesGrid, "JournalNumber", "Journal Number")
        SetHeader(entriesGrid, "LineDescription", "Line Description")
        FormatMoneyColumn(entriesGrid, "Debit", "Debit")
        FormatMoneyColumn(entriesGrid, "Credit", "Credit")
        FormatMoneyColumn(entriesGrid, "RunningBalance", "Running Balance")

        statusLabel.Text =
            $"{selectedAccount.AccountCode} - " &
            $"{selectedAccount.AccountName} | " &
            $"Opening: {ToDecimal(selectedAccount.OpeningBalance):N2} | " &
            $"Debits: {ToDecimal(selectedAccount.TotalDebits):N2} | " &
            $"Credits: {ToDecimal(selectedAccount.TotalCredits):N2} | " &
            $"Closing: {ToDecimal(selectedAccount.ClosingBalance):N2}"
    End Sub

Private Function FormatLedgerDate(value As String) As String
    Dim parsedDate As DateTime

    If DateTime.TryParse(
        value,
        Globalization.CultureInfo.InvariantCulture,
        Globalization.DateTimeStyles.AssumeUniversal Or
        Globalization.DateTimeStyles.AdjustToUniversal,
        parsedDate) Then

        Return parsedDate.ToString(
            "MMM dd, yyyy",
            Globalization.CultureInfo.InvariantCulture)
    End If

    Return value
End Function

    Private Function ContainsText(
        value As String,
        query As String) As Boolean

        Return Not String.IsNullOrEmpty(value) AndAlso
            value.IndexOf(
            query,
            StringComparison.OrdinalIgnoreCase) >= 0
    End Function

    Private Sub SetHeader(
        grid As DataGridView,
        columnName As String,
        header As String)

        If grid.Columns.Contains(columnName) Then
            grid.Columns(columnName).HeaderText = header
        End If
    End Sub

    Private Sub FormatMoneyColumn(
        grid As DataGridView,
        columnName As String,
        header As String)

        If grid.Columns.Contains(columnName) Then
            grid.Columns(columnName).HeaderText = header
            grid.Columns(columnName).DefaultCellStyle.Format = "N2"
            grid.Columns(columnName).DefaultCellStyle.Alignment =
                DataGridViewContentAlignment.MiddleRight
        End If
    End Sub

    Private Function ToDecimal(value As String) As Decimal
        Dim amount As Decimal

        Decimal.TryParse(
            value,
            Globalization.NumberStyles.Any,
            Globalization.CultureInfo.InvariantCulture,
            amount)

        Return amount
    End Function

    Private Class ApiResponse(Of T)
        Public Property Data As T
    End Class

    Private Class AccountChoice
        Public Property Id As Integer
        Public Property DisplayName As String
    End Class

    Private Class LedgerAccountItem
        Public Property AccountId As Integer
        Public Property AccountCode As String
        Public Property AccountName As String
        Public Property AccountType As String
        Public Property OpeningBalance As String
        Public Property TotalDebits As String
        Public Property TotalCredits As String
        Public Property ClosingBalance As String
        Public Property Entries As List(Of LedgerEntryItem) =
            New List(Of LedgerEntryItem)
    End Class

    Private Class LedgerEntryItem
        Public Property JournalEntryId As Integer
        Public Property JournalNumber As String
        Public Property JournalDate As String
        Public Property EntryDescription As String
        Public Property LineDescription As String
        Public Property Debit As String
        Public Property Credit As String
        Public Property RunningBalance As String
    End Class

End Class