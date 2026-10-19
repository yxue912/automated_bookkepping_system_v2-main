Imports System.Drawing
Imports System.Linq
Imports System.Net.Http
Imports System.Text
Imports System.Text.Json
Imports System.Text.Json.Serialization
Imports System.Threading.Tasks
Imports System.Windows.Forms

Public Class JournalEntriesForm
    Inherits Form

    Private ReadOnly jsonOptions As New JsonSerializerOptions With {
        .PropertyNameCaseInsensitive = True,
        .NumberHandling = JsonNumberHandling.AllowReadingFromString
    }

    Private ReadOnly titleLabel As New Label()
    Private ReadOnly refreshButton As New Button()
    Private ReadOnly postButton As New Button()
    Private ReadOnly statusLabel As New Label()
    Private ReadOnly entriesGrid As New DataGridView()
    Private ReadOnly linesGrid As New DataGridView()
    Private ReadOnly detailsLabel As New Label()

    Private isLoading As Boolean
    Private isPosting As Boolean
    Private selectedEntryId As Integer?

    Public Sub New()
        InitializeComponent()
        ConfigureEntriesGrid()
        ConfigureLinesGrid()

        AddHandler Me.Load, AddressOf JournalEntriesForm_Load
        AddHandler refreshButton.Click, AddressOf RefreshButton_Click
        AddHandler postButton.Click, AddressOf PostButton_Click
        AddHandler entriesGrid.SelectionChanged, AddressOf EntriesGrid_SelectionChanged
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Journal Entries"
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.Size = New Size(1200, 750)
        Me.MinimumSize = New Size(900, 600)
        Me.BackColor = Color.FromArgb(245, 250, 246)
        Me.Font = New Font("Segoe UI", 9.0F)

        titleLabel.Text = "Journal Entries"
        titleLabel.Font = New Font("Segoe UI", 20.0F, FontStyle.Bold)
        titleLabel.ForeColor = Color.FromArgb(35, 90, 60)
        titleLabel.Location = New Point(20, 15)
        titleLabel.Size = New Size(500, 40)

        refreshButton.Text = "Refresh"
        refreshButton.Location = New Point(910, 20)
        refreshButton.Size = New Size(110, 35)
        refreshButton.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        StyleButton(refreshButton, Color.FromArgb(55, 130, 85))

        postButton.Text = "Post Entry"
        postButton.Location = New Point(1030, 20)
        postButton.Size = New Size(130, 35)
        postButton.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        StyleButton(postButton, Color.FromArgb(35, 90, 60))
        postButton.Enabled = False

        statusLabel.Location = New Point(20, 60)
        statusLabel.Size = New Size(1120, 30)
        statusLabel.ForeColor = Color.FromArgb(80, 90, 80)
        statusLabel.Anchor =
            AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right

        Dim entriesLabel As New Label With {
            .Text = "Journal Entry Records",
            .Font = New Font("Segoe UI", 12.0F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(35, 90, 60),
            .Location = New Point(20, 95),
            .Size = New Size(400, 25)
        }

        entriesGrid.Location = New Point(20, 125)
        entriesGrid.Size = New Size(1140, 250)
        entriesGrid.Anchor =
            AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        entriesGrid.BackgroundColor = Color.White

        detailsLabel.Text = "Selected Entry Details"
        detailsLabel.Font = New Font("Segoe UI", 12.0F, FontStyle.Bold)
        detailsLabel.ForeColor = Color.FromArgb(35, 90, 60)
        detailsLabel.Location = New Point(20, 395)
        detailsLabel.Size = New Size(500, 25)

        linesGrid.Location = New Point(20, 425)
        linesGrid.Size = New Size(1140, 250)
        linesGrid.Anchor =
            AnchorStyles.Top Or AnchorStyles.Bottom Or
            AnchorStyles.Left Or AnchorStyles.Right
        linesGrid.BackgroundColor = Color.White

        Me.Controls.Add(titleLabel)
        Me.Controls.Add(refreshButton)
        Me.Controls.Add(postButton)
        Me.Controls.Add(statusLabel)
        Me.Controls.Add(entriesLabel)
        Me.Controls.Add(entriesGrid)
        Me.Controls.Add(detailsLabel)
        Me.Controls.Add(linesGrid)
    End Sub

    Private Sub StyleButton(button As Button, backColor As Color)
        button.BackColor = backColor
        button.ForeColor = Color.White
        button.FlatStyle = FlatStyle.Flat
        button.FlatAppearance.BorderSize = 0
        button.Cursor = Cursors.Hand
    End Sub

    Private Sub ConfigureEntriesGrid()
        With entriesGrid
            .ReadOnly = True
            .AllowUserToAddRows = False
            .AllowUserToDeleteRows = False
            .AllowUserToResizeRows = False
            .MultiSelect = False
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            .RowHeadersVisible = False
            .AutoGenerateColumns = True
            .EnableHeadersVisualStyles = False
            .ColumnHeadersDefaultCellStyle.BackColor =
                Color.FromArgb(55, 110, 75)
            .ColumnHeadersDefaultCellStyle.ForeColor = Color.White
            .ColumnHeadersDefaultCellStyle.Font =
                New Font("Segoe UI", 9.0F, FontStyle.Bold)
            .DefaultCellStyle.SelectionBackColor =
                Color.FromArgb(210, 235, 215)
            .DefaultCellStyle.SelectionForeColor = Color.Black
        End With
    End Sub

    Private Sub ConfigureLinesGrid()
        With linesGrid
            .ReadOnly = True
            .AllowUserToAddRows = False
            .AllowUserToDeleteRows = False
            .AllowUserToResizeRows = False
            .MultiSelect = False
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            .RowHeadersVisible = False
            .AutoGenerateColumns = True
            .EnableHeadersVisualStyles = False
            .ColumnHeadersDefaultCellStyle.BackColor =
                Color.FromArgb(55, 110, 75)
            .ColumnHeadersDefaultCellStyle.ForeColor = Color.White
            .ColumnHeadersDefaultCellStyle.Font =
                New Font("Segoe UI", 9.0F, FontStyle.Bold)
        End With
    End Sub

    Private Async Sub JournalEntriesForm_Load(
        sender As Object,
        e As EventArgs
    )
        Await LoadJournalEntriesAsync()
    End Sub

    Private Async Sub RefreshButton_Click(
        sender As Object,
        e As EventArgs
    )
        Await LoadJournalEntriesAsync()
    End Sub

    Private Async Function LoadJournalEntriesAsync() As Task
        If isLoading OrElse isPosting Then Return

        isLoading = True
        refreshButton.Enabled = False
        postButton.Enabled = False
        statusLabel.Text = "Loading journal entries..."

        Try
            ApiSession.ApplyAuthorization()

            Dim entries As New List(Of JournalEntryItem)()
            Dim pageNumber As Integer = 1
            Dim lastPage As Integer = 1
            Do
                Using response As HttpResponseMessage =
                    Await ApiSession.Client.GetAsync("journal-entries?page=" & pageNumber.ToString())

                Dim body As String =
                    Await response.Content.ReadAsStringAsync()

                If Not response.IsSuccessStatusCode Then
                    statusLabel.Text =
                        $"Unable to load journal entries " &
                        $"({CInt(response.StatusCode)}): {ExtractApiMessage(body)}"

                    entriesGrid.DataSource = Nothing
                    linesGrid.DataSource = Nothing
                    selectedEntryId = Nothing
                    Return
                End If

                Dim page As JournalEntryPage =
                    JsonSerializer.Deserialize(Of JournalEntryPage)(
                        body, jsonOptions)

                If page Is Nothing Then Throw New JsonException("Missing pagination data.")
                entries.AddRange(If(page.Data, New List(Of JournalEntryItem)()))
                lastPage = Math.Max(1, page.LastPage)
                End Using
                pageNumber += 1
            Loop While pageNumber <= lastPage

                Dim rows As New List(Of JournalEntryRow)

                For Each entry As JournalEntryItem In entries
                    If entry Is Nothing Then Continue For

                    Dim entryLines As List(Of JournalLineItem) =
                        If(entry.Lines, New List(Of JournalLineItem)())

                    Dim totalDebit As Decimal =
                        entryLines.Sum(Function(line) line.DebitAmount)

                    Dim totalCredit As Decimal =
                        entryLines.Sum(Function(line) line.CreditAmount)

                    rows.Add(New JournalEntryRow With {
                        .ID = entry.Id,
                        .JournalNumber = entry.JournalNumber,
                        .Date = FormatDate(entry.EntryDate),
                        .Description = entry.Description,
                        .ReferenceType = FormatReferenceType(entry.ReferenceType),
                        .ReferenceID = entry.ReferenceId,
                        .Status = entry.Status,
                        .TotalDebit = totalDebit,
                        .TotalCredit = totalCredit,
                        .Balanced = entryLines.Count >= 2 AndAlso
                            Math.Abs(totalDebit - totalCredit) < 0.005D
                    })
                Next

                entriesGrid.DataSource = Nothing
                entriesGrid.DataSource = rows

                If entriesGrid.Columns.Contains("ID") Then
                    entriesGrid.Columns("ID").Visible = False
                End If

                If entriesGrid.Columns.Contains("ReferenceID") Then
                    entriesGrid.Columns("ReferenceID").Visible = False
                End If

                ' Use readable column headings instead of raw property names.
                SetColumnHeader(entriesGrid, "JournalNumber", "Journal Number", 105)
                SetColumnHeader(entriesGrid, "Date", "Date", 105)
                SetColumnHeader(entriesGrid, "Description", "Description", 170)
                SetColumnHeader(entriesGrid, "ReferenceType", "Reference Type", 110)
                SetColumnHeader(entriesGrid, "Status", "Status", 75)
                SetColumnHeader(entriesGrid, "TotalDebit", "Total Debit", 100)
                SetColumnHeader(entriesGrid, "TotalCredit", "Total Credit", 100)
                SetColumnHeader(entriesGrid, "Balanced", "Balanced", 75)

                If entriesGrid.Columns.Contains("TotalDebit") Then
                    entriesGrid.Columns("TotalDebit").DefaultCellStyle.Format = "N2"
                End If

                If entriesGrid.Columns.Contains("TotalCredit") Then
                    entriesGrid.Columns("TotalCredit").DefaultCellStyle.Format = "N2"
                End If

                If rows.Count = 0 Then
                    statusLabel.Text = "No journal entries were returned by the API."
                    linesGrid.DataSource = Nothing
                    selectedEntryId = Nothing
                Else
                    entriesGrid.ClearSelection()

                    If entriesGrid.Columns.Contains("JournalNumber") Then
                        entriesGrid.CurrentCell =
                            entriesGrid.Rows(0).Cells("JournalNumber")
                    End If

                    entriesGrid.Rows(0).Selected = True

                    Dim firstId As Integer = rows(0).ID
                    selectedEntryId = firstId
                    UpdatePostButtonState()

                    statusLabel.Text =
                        $"Loaded {rows.Count} journal entry record(s)."

                    ' Explicitly load the first row's details.
                    Await LoadSelectedEntryLinesAsync(firstId)
                End If

        Catch ex As JsonException
            statusLabel.Text =
                "The journal entries response could not be read: " & ex.Message

        Catch ex As HttpRequestException
            statusLabel.Text =
                "Could not connect to Laravel. Make sure the API server is running."

        Catch ex As Exception
            statusLabel.Text =
                "Error loading journal entries: " & ex.Message

        Finally
            isLoading = False
            refreshButton.Enabled = True
            UpdatePostButtonState()
        End Try
    End Function

    Private Async Sub EntriesGrid_SelectionChanged(
        sender As Object,
        e As EventArgs
    )
        If isLoading OrElse isPosting OrElse
           entriesGrid.CurrentRow Is Nothing Then Return

        If Not entriesGrid.Columns.Contains("ID") Then Return

        Dim selectedValue As Object =
            entriesGrid.CurrentRow.Cells("ID").Value

        If selectedValue Is Nothing OrElse
           selectedValue Is DBNull.Value Then Return

        Dim entryId As Integer

        If Not Integer.TryParse(selectedValue.ToString(), entryId) Then Return

        If selectedEntryId.HasValue AndAlso selectedEntryId.Value = entryId Then
            UpdatePostButtonState()
            Return
        End If

        selectedEntryId = entryId
        UpdatePostButtonState()

        Await LoadSelectedEntryLinesAsync(entryId)
    End Sub

    Private Sub UpdatePostButtonState()
        If isPosting OrElse isLoading OrElse
           Not selectedEntryId.HasValue Then
            postButton.Enabled = False
            Return
        End If

        If entriesGrid.CurrentRow Is Nothing OrElse
           Not entriesGrid.Columns.Contains("Status") Then
            postButton.Enabled = False
            Return
        End If

        Dim entryStatus As String =
            Convert.ToString(entriesGrid.CurrentRow.Cells("Status").Value)

        postButton.Enabled =
            String.Equals(
                entryStatus,
                "Draft",
                StringComparison.OrdinalIgnoreCase)
    End Sub

    Private Async Sub PostButton_Click(
        sender As Object,
        e As EventArgs
    )
        If Not selectedEntryId.HasValue OrElse isPosting Then Return

        If entriesGrid.CurrentRow Is Nothing OrElse
           Not entriesGrid.Columns.Contains("Status") Then
            MessageBox.Show(
                "Select a journal entry first.",
                "Journal Entries",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information)
            Return
        End If

        Dim selectedStatus As String =
            Convert.ToString(entriesGrid.CurrentRow.Cells("Status").Value)

        If Not String.Equals(
            selectedStatus,
            "Draft",
            StringComparison.OrdinalIgnoreCase) Then

            MessageBox.Show(
                "Only Draft journal entries can be posted.",
                "Cannot Post Entry",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information)

            UpdatePostButtonState()
            Return
        End If

        Dim entryId As Integer = selectedEntryId.Value
        Dim journalNumber As String =
            Convert.ToString(entriesGrid.CurrentRow.Cells("JournalNumber").Value)

        If MessageBox.Show(
            $"Post journal entry {journalNumber}?" &
            Environment.NewLine & Environment.NewLine &
            "Make sure the entry is correct. Posting may affect accounting reports.",
            "Confirm Journal Posting",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2) <> DialogResult.Yes Then
            Return
        End If

        isPosting = True
        postButton.Enabled = False
        refreshButton.Enabled = False
        statusLabel.Text = $"Posting journal entry {journalNumber}..."

        Try
            ApiSession.ApplyAuthorization()

            Using content As New StringContent(
                "{}", Encoding.UTF8, "application/json")

                Using response As HttpResponseMessage =
                    Await ApiSession.Client.PostAsync(
                        $"journal-entries/{entryId}/post", content)

                    Dim body As String =
                        Await response.Content.ReadAsStringAsync()

                    If Not response.IsSuccessStatusCode Then
                        Dim errorMessage As String = ExtractApiMessage(body)

                        statusLabel.Text =
                            $"Posting failed ({CInt(response.StatusCode)}): " &
                            errorMessage

                        MessageBox.Show(
                            errorMessage,
                            "Posting Failed",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning)
                        Return
                    End If
                End Using
            End Using

            MessageBox.Show(
                $"Journal entry {journalNumber} was posted successfully.",
                "Entry Posted",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information)

        Catch ex As HttpRequestException
            statusLabel.Text = "Could not connect to the Laravel API."

            MessageBox.Show(
                statusLabel.Text,
                "Connection Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)

        Catch ex As Exception
            statusLabel.Text =
                "Error posting journal entry: " & ex.Message

            MessageBox.Show(
                statusLabel.Text,
                "Posting Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)

        Finally
            isPosting = False
            refreshButton.Enabled = True
        End Try

        Await LoadJournalEntriesAsync()
    End Sub

    Private Async Function LoadSelectedEntryLinesAsync(
        entryId As Integer
    ) As Task
        statusLabel.Text = "Loading selected journal entry details..."
        linesGrid.DataSource = Nothing

        Try
            ApiSession.ApplyAuthorization()

            Using response As HttpResponseMessage =
                Await ApiSession.Client.GetAsync($"journal-entries/{entryId}")

                Dim body As String =
                    Await response.Content.ReadAsStringAsync()

                If Not response.IsSuccessStatusCode Then
                    statusLabel.Text =
                        $"Unable to load entry details " &
                        $"({CInt(response.StatusCode)}): {ExtractApiMessage(body)}"
                    Return
                End If

                Using document As JsonDocument = JsonDocument.Parse(body)
                    Dim root As JsonElement = document.RootElement
                    Dim entryElement As JsonElement = root
                    Dim dataElement As JsonElement

                    If root.ValueKind = JsonValueKind.Object AndAlso
                       root.TryGetProperty("data", dataElement) AndAlso
                       dataElement.ValueKind = JsonValueKind.Object Then
                        entryElement = dataElement
                    End If

                    Dim entry As JournalEntryItem =
                        JsonSerializer.Deserialize(Of JournalEntryItem)(
                            entryElement.GetRawText(), jsonOptions)

                    If entry Is Nothing Then
                        statusLabel.Text = "The API returned an empty journal entry."
                        Return
                    End If

                    Dim entryLines As List(Of JournalLineItem) =
                        If(entry.Lines, New List(Of JournalLineItem)())

                    Dim detailRows As New List(Of JournalLineRow)

                    For Each line As JournalLineItem In entryLines
                        detailRows.Add(New JournalLineRow With {
                            .AccountID = line.AccountId,
                            .AccountName = If(
                                line.Account Is Nothing,
                                "",
                                $"{line.Account.AccountCode} - {line.Account.Name}"),
                            .Description = If(line.Description, ""),
                            .DebitAmount = line.DebitAmount,
                            .CreditAmount = line.CreditAmount
                        })
                    Next

                    linesGrid.DataSource = Nothing
                    linesGrid.DataSource = detailRows

                    SetColumnHeader(linesGrid, "AccountID", "Account ID", 75)
                    SetColumnHeader(linesGrid, "AccountName", "Account", 180)
                    SetColumnHeader(linesGrid, "Description", "Description", 180)
                    SetColumnHeader(linesGrid, "DebitAmount", "Debit Amount", 110)
                    SetColumnHeader(linesGrid, "CreditAmount", "Credit Amount", 110)

                    If linesGrid.Columns.Contains("DebitAmount") Then
                        linesGrid.Columns("DebitAmount").DefaultCellStyle.Format = "N2"
                    End If

                    If linesGrid.Columns.Contains("CreditAmount") Then
                        linesGrid.Columns("CreditAmount").DefaultCellStyle.Format = "N2"
                    End If

                    Dim debitTotal As Decimal =
                        entryLines.Sum(Function(line) line.DebitAmount)

                    Dim creditTotal As Decimal =
                        entryLines.Sum(Function(line) line.CreditAmount)

                    Dim balanceText As String =
                        If(entryLines.Count >= 2 AndAlso
                           Math.Abs(debitTotal - creditTotal) < 0.005D,
                           "Balanced",
                           "Not balanced")

                    statusLabel.Text =
                        $"Entry {entry.JournalNumber} ({entry.Status}): " &
                        $"Debit {debitTotal:N2} | " &
                        $"Credit {creditTotal:N2} | {balanceText}"

                    If entriesGrid.CurrentRow IsNot Nothing AndAlso
                       entriesGrid.Columns.Contains("Status") Then
                        entriesGrid.CurrentRow.Cells("Status").Value = entry.Status
                    End If
                End Using
            End Using

        Catch ex As JsonException
            statusLabel.Text =
                "The journal entry details could not be read: " & ex.Message

        Catch ex As HttpRequestException
            statusLabel.Text = "Could not connect to the Laravel API."

        Catch ex As Exception
            statusLabel.Text =
                "Error loading entry details: " & ex.Message

        Finally
            UpdatePostButtonState()
        End Try
    End Function

    Private Shared Sub SetColumnHeader(
        grid As DataGridView,
        columnName As String,
        headerText As String,
        fillWeight As Single
    )
        If grid.Columns.Contains(columnName) Then
            grid.Columns(columnName).HeaderText = headerText
            grid.Columns(columnName).FillWeight = fillWeight
        End If
    End Sub

    Private Shared Function FormatReferenceType(value As String) As String
        If String.IsNullOrWhiteSpace(value) Then Return ""

        Select Case value.Trim().ToLowerInvariant()
            Case "cashreceipt"
                Return "Cash Receipt"
            Case "cashdisbursement"
                Return "Cash Disbursement"
            Case "journalentry"
                Return "Journal Entry"
            Case Else
                ' Add spaces when an API value uses PascalCase, without changing stored data.
                Dim result As New System.Text.StringBuilder()

                For index As Integer = 0 To value.Trim().Length - 1
                    Dim currentCharacter As Char = value.Trim()(index)

                    If index > 0 AndAlso
                       Char.IsUpper(currentCharacter) AndAlso
                       (Char.IsLower(value.Trim()(index - 1)) OrElse
                        Char.IsDigit(value.Trim()(index - 1))) Then
                        result.Append(" "c)
                    End If

                    result.Append(currentCharacter)
                Next

                Return result.ToString().Trim()
        End Select
    End Function

    Private Shared Function FormatDate(value As String) As String
        If String.IsNullOrWhiteSpace(value) Then Return ""

        Dim parsed As DateTime
        If DateTime.TryParse(
            value,
            Globalization.CultureInfo.InvariantCulture,
            Globalization.DateTimeStyles.AssumeLocal,
            parsed
        ) Then
            Return parsed.ToString(
                "MMM dd, yyyy",
                Globalization.CultureInfo.InvariantCulture
            )
        End If

        Return value
    End Function

    Private Function ExtractApiMessage(body As String) As String
        If String.IsNullOrWhiteSpace(body) Then
            Return "The API returned no error details."
        End If

        Try
            Using document As JsonDocument = JsonDocument.Parse(body)
                Dim root As JsonElement = document.RootElement
                Dim messageElement As JsonElement

                If root.ValueKind = JsonValueKind.Object AndAlso
                   root.TryGetProperty("message", messageElement) AndAlso
                   messageElement.ValueKind = JsonValueKind.String Then
                    Return messageElement.GetString()
                End If

                Dim errorsElement As JsonElement

                If root.ValueKind = JsonValueKind.Object AndAlso
                   root.TryGetProperty("errors", errorsElement) AndAlso
                   errorsElement.ValueKind = JsonValueKind.Object Then

                    Dim messages As New List(Of String)

                    For Each propertyItem As JsonProperty In errorsElement.EnumerateObject()
                        If propertyItem.Value.ValueKind = JsonValueKind.Array Then
                            For Each item As JsonElement In propertyItem.Value.EnumerateArray()
                                If item.ValueKind = JsonValueKind.String Then
                                    messages.Add(item.GetString())
                                End If
                            Next
                        End If
                    Next

                    If messages.Count > 0 Then
                        Return String.Join(Environment.NewLine, messages)
                    End If
                End If
            End Using
        Catch
            ' Use the response body if it is not JSON.
        End Try

        Return body
    End Function

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        RemoveHandler entriesGrid.SelectionChanged,
            AddressOf EntriesGrid_SelectionChanged

        ' ApiSession.Client is shared with other forms. Do not dispose it here.
        MyBase.OnFormClosed(e)
    End Sub

    Private Class JournalEntryPage
        <JsonPropertyName("data")>
        Public Property Data As List(Of JournalEntryItem)

        <JsonPropertyName("last_page")>
        Public Property LastPage As Integer
    End Class

    Private Class JournalEntryItem
        <JsonPropertyName("id")>
        Public Property Id As Integer

        <JsonPropertyName("journal_number")>
        Public Property JournalNumber As String

        <JsonPropertyName("journal_date")>
        Public Property EntryDate As String

        <JsonPropertyName("description")>
        Public Property Description As String

        <JsonPropertyName("reference_type")>
        Public Property ReferenceType As String

        <JsonPropertyName("reference_id")>
        Public Property ReferenceId As Integer?

        <JsonPropertyName("status")>
        Public Property Status As String

        <JsonPropertyName("lines")>
        Public Property Lines As List(Of JournalLineItem)
    End Class

    Private Class JournalLineItem
        <JsonPropertyName("id")>
        Public Property Id As Integer

        <JsonPropertyName("account_id")>
        Public Property AccountId As Integer

        <JsonPropertyName("description")>
        Public Property Description As String

        <JsonPropertyName("debit_amount")>
        <JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)>
        Public Property DebitAmount As Decimal

        <JsonPropertyName("credit_amount")>
        <JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)>
        Public Property CreditAmount As Decimal

        <JsonPropertyName("account")>
        Public Property Account As AccountItem
    End Class

    Private Class AccountItem
        <JsonPropertyName("id")>
        Public Property Id As Integer

        <JsonPropertyName("account_code")>
        Public Property AccountCode As String

        <JsonPropertyName("account_name")>
        Public Property Name As String
    End Class

    Private Class JournalEntryRow
        Public Property ID As Integer
        Public Property JournalNumber As String
        Public Property [Date] As String
        Public Property Description As String
        Public Property ReferenceType As String
        Public Property ReferenceID As Integer?
        Public Property Status As String
        Public Property TotalDebit As Decimal
        Public Property TotalCredit As Decimal
        Public Property Balanced As Boolean
    End Class

    Private Class JournalLineRow
        Public Property AccountID As Integer
        Public Property AccountName As String
        Public Property Description As String
        Public Property DebitAmount As Decimal
        Public Property CreditAmount As Decimal
    End Class

End Class
