Option Strict On
Option Explicit On

Imports System.Net
Imports System.Net.Http
Imports System.Text.Json
Imports System.Threading.Tasks

Public Class AuditTrailForm
    Inherits Form

    Private ReadOnly grid As New DataGridView()
    Private ReadOnly refreshButton As New Button()
    Private ReadOnly previousButton As New Button()
    Private ReadOnly nextButton As New Button()
    Private ReadOnly statusLabel As New Label()
    Private ReadOnly fromPicker As New DateTimePicker()
    Private ReadOnly toPicker As New DateTimePicker()
    Private ReadOnly filterBox As New TextBox()

    Private currentPage As Integer = 1
    Private lastPage As Integer = 1

    Public Sub New()
        Text = "Audit Trail"
        StartPosition = FormStartPosition.CenterParent
        MinimumSize = New Size(900, 560)
        Size = New Size(1100, 680)

        Dim top As New FlowLayoutPanel With {
            .Dock = DockStyle.Top,
            .Height = 78,
            .Padding = New Padding(10),
            .WrapContents = True
        }

        fromPicker.Format = DateTimePickerFormat.Short
        fromPicker.Value = Date.Today.AddDays(-30)

        toPicker.Format = DateTimePickerFormat.Short
        toPicker.Value = Date.Today

        filterBox.Width = 180

        refreshButton.Text = "Refresh"
        refreshButton.AutoSize = True

        previousButton.Text = "Previous"
        previousButton.AutoSize = True
        previousButton.Enabled = False

        nextButton.Text = "Next"
        nextButton.AutoSize = True
        nextButton.Enabled = False

        AddHandler refreshButton.Click,
            Async Sub(sender, e)
                Await LoadEventsAsync(1)
            End Sub

        AddHandler previousButton.Click,
            Async Sub(sender, e)
                If currentPage > 1 Then
                    Await LoadEventsAsync(currentPage - 1)
                End If
            End Sub

        AddHandler nextButton.Click,
            Async Sub(sender, e)
                If currentPage < lastPage Then
                    Await LoadEventsAsync(currentPage + 1)
                End If
            End Sub

        top.Controls.AddRange({
            New Label With {
                .Text = "From",
                .AutoSize = True,
                .Margin = New Padding(3, 8, 3, 3)
            },
            fromPicker,
            New Label With {
                .Text = "To",
                .AutoSize = True,
                .Margin = New Padding(3, 8, 3, 3)
            },
            toPicker,
            New Label With {
                .Text = "Event/user filter",
                .AutoSize = True,
                .Margin = New Padding(3, 8, 3, 3)
            },
            filterBox,
            refreshButton,
            previousButton,
            nextButton
        })

        grid.Dock = DockStyle.Fill
        grid.ReadOnly = True
        grid.AllowUserToAddRows = False
        grid.AllowUserToDeleteRows = False
        grid.AutoGenerateColumns = True
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        grid.MultiSelect = False
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        grid.RowHeadersVisible = False

        statusLabel.Dock = DockStyle.Bottom
        statusLabel.Height = 46
        statusLabel.Padding = New Padding(10, 7, 0, 0)
        statusLabel.Text = "Loading audit records..."

        Controls.Add(grid)
        CsvExportMenu.Attach(Me, Tuple.Create("Current Audit Trail Page", grid))
        Controls.Add(statusLabel)
        Controls.Add(top)

        AddHandler Shown,
            Async Sub(sender, e)
                Await LoadEventsAsync(1)
            End Sub
    End Sub

    Private Async Function LoadEventsAsync(
        Optional requestedPage As Integer = 1
    ) As Task

        If String.IsNullOrWhiteSpace(ApiSession.BearerToken) Then
            statusLabel.Text = "Please sign in again."
            Return
        End If

        If fromPicker.Value.Date > toPicker.Value.Date Then
            statusLabel.Text =
                "The From date must not be after the To date."
            Return
        End If

        refreshButton.Enabled = False
        previousButton.Enabled = False
        nextButton.Enabled = False
        statusLabel.Text = "Loading audit records..."

        Try
            Dim url As String =
                "audit-logs?from=" &
                Uri.EscapeDataString(
                    fromPicker.Value.ToString("yyyy-MM-dd")) &
                "&to=" &
                Uri.EscapeDataString(
                    toPicker.Value.ToString("yyyy-MM-dd")) &
                "&filter=" &
                Uri.EscapeDataString(filterBox.Text.Trim()) &
                "&page=" & requestedPage.ToString()

            ApiSession.ApplyAuthorization()

            Using response As HttpResponseMessage =
                Await ApiSession.Client.GetAsync(url)

                Dim body As String =
                    Await response.Content.ReadAsStringAsync()

                If response.StatusCode = HttpStatusCode.Unauthorized Then
                    statusLabel.Text =
                        "Your session has expired. Please sign in again."
                    Return
                End If

                If response.StatusCode = HttpStatusCode.Forbidden Then
                    statusLabel.Text =
                        "Access denied. Audit-log viewing permission is required."
                    Return
                End If

                If Not response.IsSuccessStatusCode Then
                    statusLabel.Text =
                        "Could not load audit records. HTTP " &
                        CInt(response.StatusCode).ToString() & "."
                    Return
                End If

                Using doc As JsonDocument = JsonDocument.Parse(body)
                    Dim root As JsonElement = doc.RootElement
                    Dim data As JsonElement

                    If root.ValueKind <> JsonValueKind.Object OrElse
                        Not root.TryGetProperty("data", data) OrElse
                        data.ValueKind <> JsonValueKind.Array Then

                        statusLabel.Text =
                            "Unexpected audit API response format."
                        Return
                    End If

                    Dim rows As New List(Of AuditEventRow)()

                    For Each item As JsonElement In data.EnumerateArray()
                        Dim user As JsonElement
                        Dim actor As String = ""

                        If item.ValueKind = JsonValueKind.Object AndAlso
                            item.TryGetProperty("user", user) AndAlso
                            user.ValueKind = JsonValueKind.Object Then

                            actor = ReadField(user, "name")

                            If String.IsNullOrWhiteSpace(actor) Then
                                actor = ReadField(user, "email")
                            End If
                        End If

                        If String.IsNullOrWhiteSpace(actor) Then
                            actor = "System / Unknown"
                        End If

                        Dim eventCode As String =
                            ReadField(item, "event")

                        rows.Add(New AuditEventRow With {
                            .OccurredAt = FormatLocalDateTime(ReadField(item, "created_at")),
                            .Actor = actor,
                            .Action = FormatEventName(eventCode),
                            .Description = ReadField(item, "description")
                        })
                    Next

                    grid.DataSource = Nothing
                    grid.DataSource = rows

                    If grid.Columns.Contains("OccurredAt") Then
                        grid.Columns("OccurredAt").HeaderText = "Date / Time"
                        grid.Columns("OccurredAt").FillWeight = 140
                    End If

                    If grid.Columns.Contains("Actor") Then
                        grid.Columns("Actor").HeaderText = "User"
                        grid.Columns("Actor").FillWeight = 100
                    End If

                    If grid.Columns.Contains("Action") Then
                        grid.Columns("Action").HeaderText = "Event"
                        grid.Columns("Action").FillWeight = 120
                    End If

                    If grid.Columns.Contains("Description") Then
                        grid.Columns("Description").HeaderText = "Description"
                        grid.Columns("Description").FillWeight = 180
                    End If

                    Dim total As Integer = rows.Count
                    Dim totalElement As JsonElement

                    If root.TryGetProperty("total", totalElement) AndAlso
                        totalElement.ValueKind = JsonValueKind.Number Then

                        total = totalElement.GetInt32()
                    End If

                    Dim currentPageElement As JsonElement
                    Dim lastPageElement As JsonElement

                    If root.TryGetProperty(
                        "current_page", currentPageElement
                    ) AndAlso
                        currentPageElement.ValueKind = JsonValueKind.Number Then

                        currentPage = currentPageElement.GetInt32()
                    Else
                        currentPage = requestedPage
                    End If

                    If root.TryGetProperty(
                        "last_page", lastPageElement
                    ) AndAlso
                        lastPageElement.ValueKind = JsonValueKind.Number Then

                        lastPage = lastPageElement.GetInt32()
                    Else
                        lastPage = 1
                    End If

                    previousButton.Enabled = currentPage > 1
                    nextButton.Enabled = currentPage < lastPage

                    statusLabel.Text =
                        rows.Count.ToString() &
                        " record(s) on page " &
                        currentPage.ToString() &
                        " of " &
                        lastPage.ToString() &
                        ". Total matching records: " &
                        total.ToString() &
                        ". API: " & ApiSession.Client.BaseAddress.ToString()
                End Using
            End Using

        Catch ex As HttpRequestException
            statusLabel.Text =
                "Cannot reach the API. Check that the Laravel server is running."

        Catch ex As TaskCanceledException
            statusLabel.Text = "The audit request timed out."

        Catch ex As JsonException
            statusLabel.Text = "The API returned invalid JSON."

        Catch ex As Exception
            statusLabel.Text =
                "Unable to load audit records. Check the application logs."

        Finally
            refreshButton.Enabled = True
        End Try
    End Function

    Private Shared Function FormatLocalDateTime(rawValue As String) As String
        If String.IsNullOrWhiteSpace(rawValue) Then
            Return ""
        End If

        Dim parsed As DateTimeOffset
        If DateTimeOffset.TryParse(
            rawValue,
            Globalization.CultureInfo.InvariantCulture,
            Globalization.DateTimeStyles.AssumeUniversal Or Globalization.DateTimeStyles.AdjustToUniversal,
            parsed) Then

            Return parsed.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
        End If

        Return rawValue
    End Function

    Private Shared Function FormatEventName(eventCode As String) As String
        If String.IsNullOrWhiteSpace(eventCode) Then
            Return "Unspecified Event"
        End If

        Dim normalized As String =
            eventCode.Trim().ToLowerInvariant()

        Select Case normalized
            Case "login.success"
                Return "Login Successful"

            Case "logout.success"
                Return "Logout Successful"

            Case "account.registered"
                Return "Account Registered"

            Case "account.approved"
                Return "Account Approved"

            Case "cash_receipt.posted"
                Return "Cash Receipt Posted"

            Case "cash_disbursement.posted"
                Return "Cash Disbursement Posted"

            Case "journal_entry.created"
                Return "Journal Entry Created"

            Case "journal_entry.posted"
                Return "Journal Entry Posted"
        End Select

        Dim readable As String =
            normalized.Replace(".", " ").Replace("_", " ").Replace("-", " ")

        Dim words As String() =
            readable.Split(
                New Char() {" "c},
                StringSplitOptions.RemoveEmptyEntries)

        For index As Integer = 0 To words.Length - 1
            words(index) =
                Char.ToUpperInvariant(words(index)(0)) &
                words(index).Substring(1)
        Next

        Return String.Join(" ", words)
    End Function

    Private Shared Function ReadField(
        item As JsonElement,
        ParamArray names As String()
    ) As String

        For Each name As String In names
            Dim value As JsonElement

            If item.ValueKind = JsonValueKind.Object AndAlso
                item.TryGetProperty(name, value) Then

                If value.ValueKind = JsonValueKind.String Then
                    Return If(value.GetString(), "")
                End If

                If value.ValueKind <> JsonValueKind.Null AndAlso
                    value.ValueKind <> JsonValueKind.Undefined Then

                    Return value.ToString()
                End If
            End If
        Next

        Return String.Empty
    End Function

    Private Class AuditEventRow
        Public Property OccurredAt As String = ""
        Public Property Actor As String = ""
        Public Property Action As String = ""
        Public Property Description As String = ""
    End Class

End Class