Imports System.Drawing
Imports System.Globalization
Imports System.Linq
Imports System.Net.Http
Imports System.Net.Http.Json
Imports System.Text.Json
Imports System.Text.Json.Serialization
Imports System.Windows.Forms

Public Class CashDisbursementsForm
    Inherits Form

    Private ReadOnly disbursementsGrid As New DataGridView()
    Private ReadOnly dateInput As New DateTimePicker()
    Private ReadOnly voucherInput As New TextBox()
    Private ReadOnly payeeInput As New TextBox()
    Private ReadOnly expenseAccountCombo As New ComboBox()
    Private ReadOnly paymentAccountCombo As New ComboBox()
    Private ReadOnly amountInput As New TextBox()
    Private ReadOnly methodCombo As New ComboBox()
    Private ReadOnly checkNumberInput As New TextBox()
    Private ReadOnly remarksInput As New TextBox()
    Private ReadOnly statusLabel As New Label()
    Private ReadOnly saveButton As New Button()
    Private ReadOnly refreshButton As New Button()

    Private ReadOnly jsonOptions As New JsonSerializerOptions With {
        .PropertyNameCaseInsensitive = True,
        .NumberHandling = JsonNumberHandling.AllowReadingFromString
    }

    Private Class ApiResponse(Of T)
        <JsonPropertyName("success")>
        Public Property Success As Boolean

        <JsonPropertyName("message")>
        Public Property Message As String

        <JsonPropertyName("data")>
        Public Property Data As T
    End Class

    Private Class AccountItem
        <JsonPropertyName("id")>
        Public Property Id As Integer

        <JsonPropertyName("account_code")>
        Public Property Code As String

        <JsonPropertyName("account_name")>
        Public Property Name As String

        <JsonPropertyName("account_type")>
        Public Property AccountType As String

        <JsonPropertyName("is_active")>
        Public Property IsActive As Boolean

        Public Overrides Function ToString() As String
            Return $"{Code} - {Name} ({AccountType})"
        End Function
    End Class

    Private Class DisbursementItem
        <JsonPropertyName("id")>
        Public Property Id As Integer

        <JsonPropertyName("disbursement_date")>
        Public Property DisbursementDate As String

        <JsonPropertyName("voucher_number")>
        Public Property VoucherNumber As String

        <JsonPropertyName("payee")>
        Public Property Payee As String

        <JsonPropertyName("account_id")>
        Public Property AccountId As Integer

        <JsonPropertyName("payment_account_id")>
        Public Property PaymentAccountId As Integer?

        <JsonPropertyName("amount")>
        Public Property Amount As Decimal

        <JsonPropertyName("payment_method")>
        Public Property PaymentMethod As String

        <JsonPropertyName("check_number")>
        Public Property CheckNumber As String

        <JsonPropertyName("remarks")>
        Public Property Remarks As String

        <JsonPropertyName("account")>
        Public Property ExpenseAccount As AccountItem

        <JsonPropertyName("payment_account")>
        Public Property PaymentAccount As AccountItem
    End Class

    Public Sub New()
        Text = "Cash Disbursements Book"
        StartPosition = FormStartPosition.CenterScreen
        MinimumSize = New Size(900, 600)
        WindowState = FormWindowState.Maximized
        BackColor = Color.FromArgb(245, 250, 246)
        Font = New Font("Segoe UI", 9.0F)

        BuildInterface()
        AddHandler Shown, AddressOf FormShown
    End Sub

    Private Sub BuildInterface()
        Dim root As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 3,
            .Padding = New Padding(18),
            .BackColor = BackColor
        }

        root.RowStyles.Add(New RowStyle(SizeType.Absolute, 70))
        root.RowStyles.Add(New RowStyle(SizeType.Absolute, 310))
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        Controls.Add(root)

        Dim header As New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = BackColor
        }

        header.Controls.Add(New Label With {
            .Text = "Cash Disbursements Book",
            .Font = New Font("Segoe UI", 20, FontStyle.Bold),
            .ForeColor = Color.FromArgb(38, 95, 65),
            .AutoSize = True,
            .Location = New Point(0, 0)
        })

        header.Controls.Add(New Label With {
            .Text = "Record outgoing payments and automatically post their accounting entries.",
            .ForeColor = Color.FromArgb(90, 110, 98),
            .AutoSize = True,
            .Location = New Point(2, 40)
        })

        root.Controls.Add(header, 0, 0)

        Dim entryPanel As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 4,
            .RowCount = 6,
            .Padding = New Padding(14),
            .BackColor = Color.White
        }

        entryPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 17))
        entryPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33))
        entryPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 17))
        entryPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33))

        For i As Integer = 0 To 4
            entryPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 18))
        Next
        entryPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 40))

        ConfigureInput(dateInput)
        dateInput.Format = DateTimePickerFormat.Short

        ConfigureInput(voucherInput)
        voucherInput.ReadOnly = True
        voucherInput.PlaceholderText = "Generated automatically when saved"
        ConfigureInput(payeeInput)
        ConfigureInput(expenseAccountCombo)
        ConfigureInput(paymentAccountCombo)
        ConfigureInput(amountInput)
        ConfigureInput(methodCombo)
        ConfigureInput(checkNumberInput)
        ConfigureInput(remarksInput)

        expenseAccountCombo.DropDownStyle = ComboBoxStyle.DropDownList
        paymentAccountCombo.DropDownStyle = ComboBoxStyle.DropDownList

        methodCombo.DropDownStyle = ComboBoxStyle.DropDownList
        methodCombo.Items.AddRange(New Object() {"Cash", "Check", "Bank Transfer"})
        methodCombo.SelectedIndex = 0

        remarksInput.Multiline = True
        remarksInput.ScrollBars = ScrollBars.Vertical

        AddField(entryPanel, "Disbursement Date", dateInput, 0, 0)
        AddField(entryPanel, "Voucher Number", voucherInput, 1, 0)
        AddField(entryPanel, "Payee", payeeInput, 0, 1)
        AddField(entryPanel, "Expense / Debit Account", expenseAccountCombo, 1, 1)
        AddField(entryPanel, "Payment / Cash Account", paymentAccountCombo, 0, 2)
        AddField(entryPanel, "Amount", amountInput, 1, 2)
        AddField(entryPanel, "Payment Method", methodCombo, 0, 3)
        AddField(entryPanel, "Check Number (if applicable)", checkNumberInput, 1, 3)
        AddField(entryPanel, "Remarks", remarksInput, 0, 4)

        statusLabel.Dock = DockStyle.Fill
        statusLabel.TextAlign = ContentAlignment.MiddleLeft
        statusLabel.ForeColor = Color.FromArgb(65, 85, 72)

        saveButton.Text = "Save Disbursement"
        saveButton.Width = 150
        saveButton.Height = 32
        saveButton.BackColor = Color.FromArgb(42, 125, 82)
        saveButton.ForeColor = Color.White
        saveButton.FlatStyle = FlatStyle.Flat
        saveButton.FlatAppearance.BorderSize = 0
        AddHandler saveButton.Click, AddressOf SaveClicked

        refreshButton.Text = "Refresh List"
        refreshButton.Width = 110
        refreshButton.Height = 32
        AddHandler refreshButton.Click, AddressOf RefreshClicked

        Dim buttons As New FlowLayoutPanel With {
            .Dock = DockStyle.Fill,
            .FlowDirection = FlowDirection.RightToLeft,
            .WrapContents = False
        }

        buttons.Controls.Add(saveButton)
        buttons.Controls.Add(refreshButton)

        entryPanel.Controls.Add(statusLabel, 0, 5)
        entryPanel.SetColumnSpan(statusLabel, 2)
        entryPanel.Controls.Add(buttons, 2, 5)
        entryPanel.SetColumnSpan(buttons, 2)

        root.Controls.Add(entryPanel, 0, 1)

        disbursementsGrid.Dock = DockStyle.Fill
        disbursementsGrid.ReadOnly = True
        disbursementsGrid.AllowUserToAddRows = False
        disbursementsGrid.AllowUserToDeleteRows = False
        disbursementsGrid.AutoGenerateColumns = False
        disbursementsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        disbursementsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        disbursementsGrid.MultiSelect = False
        disbursementsGrid.BackgroundColor = Color.White
        disbursementsGrid.BorderStyle = BorderStyle.None
        disbursementsGrid.RowHeadersVisible = False

        AddGridColumn("Date", "DisbursementDate", "Date")
        AddGridColumn("Voucher", "VoucherNumber", "Voucher Number")
        AddGridColumn("Payee", "Payee", "Payee")
        AddGridColumn("Expense Account", "ExpenseAccountName", "Expense Account")
        AddGridColumn("Payment Account", "PaymentAccountName", "Payment Account")
        AddGridColumn("Method", "PaymentMethod", "Method")
        AddGridColumn("Amount", "Amount", "Amount", "N2")
        AddGridColumn("Check No.", "CheckNumber", "Check Number")

        Dim listPanel As New Panel With {
            .Dock = DockStyle.Fill,
            .Padding = New Padding(0, 12, 0, 0),
            .BackColor = BackColor
        }

        listPanel.Controls.Add(disbursementsGrid)
        listPanel.Controls.Add(New Label With {
            .Text = "Disbursement History",
            .Font = New Font("Segoe UI", 13, FontStyle.Bold),
            .ForeColor = Color.FromArgb(38, 95, 65),
            .Dock = DockStyle.Top,
            .Height = 35
        })

        root.Controls.Add(listPanel, 0, 2)
    End Sub

    Private Sub AddGridColumn(
        name As String,
        propertyName As String,
        heading As String,
        Optional numberFormat As String = ""
    )
        Dim column As New DataGridViewTextBoxColumn With {
            .Name = name,
            .HeaderText = heading,
            .DataPropertyName = propertyName
        }

        If numberFormat <> "" Then
            column.DefaultCellStyle.Format = numberFormat
        End If

        disbursementsGrid.Columns.Add(column)
    End Sub

    Private Sub ConfigureInput(control As Control)
        control.Dock = DockStyle.Fill
        control.Margin = New Padding(5)
    End Sub

    Private Sub AddField(
        panel As TableLayoutPanel,
        caption As String,
        input As Control,
        pairIndex As Integer,
        rowIndex As Integer
    )
        Dim labelColumn As Integer = pairIndex * 2
        Dim inputColumn As Integer = labelColumn + 1

        panel.Controls.Add(New Label With {
            .Text = caption,
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft,
            .ForeColor = Color.FromArgb(50, 75, 60),
            .Margin = New Padding(4)
        }, labelColumn, rowIndex)

        panel.Controls.Add(input, inputColumn, rowIndex)
    End Sub

    Private Async Sub FormShown(sender As Object, e As EventArgs)
        Await LoadAccountsAsync()
        Await LoadDisbursementsAsync()
    End Sub

    Private Async Function LoadAccountsAsync() As Task
        Try
            ApiSession.ApplyAuthorization()

            Using response = Await ApiSession.Client.GetAsync("accounts?is_active=1")
                Dim body = Await response.Content.ReadAsStringAsync()

                If Not response.IsSuccessStatusCode Then
                    Throw New Exception(body)
                End If

                Dim result = JsonSerializer.Deserialize(
                    Of ApiResponse(Of List(Of AccountItem)))(body, jsonOptions)

                Dim accounts = If(result?.Data, New List(Of AccountItem)())

                expenseAccountCombo.Items.Clear()
                paymentAccountCombo.Items.Clear()

                For Each account In accounts
                    If account.AccountType = "Expense" OrElse
                       account.AccountType = "Liability" Then
                        expenseAccountCombo.Items.Add(account)
                    End If

                    If account.AccountType = "Asset" Then
                        paymentAccountCombo.Items.Add(account)
                    End If
                Next

                If expenseAccountCombo.Items.Count > 0 Then
                    expenseAccountCombo.SelectedIndex = 0
                End If

                If paymentAccountCombo.Items.Count > 0 Then
                    paymentAccountCombo.SelectedIndex = 0
                End If

                SetStatus("Active accounts loaded.")
            End Using

        Catch ex As Exception
            SetStatus("Could not load accounts: " & ex.Message, True)
        End Try
    End Function

    Private Async Function LoadDisbursementsAsync() As Task
        Try
            ApiSession.ApplyAuthorization()

            Using response = Await ApiSession.Client.GetAsync("cash-disbursements")
                Dim body = Await response.Content.ReadAsStringAsync()

                If Not response.IsSuccessStatusCode Then
                    Throw New Exception(body)
                End If

                Dim result = JsonSerializer.Deserialize(
                    Of ApiResponse(Of List(Of DisbursementItem)))(body, jsonOptions)

                Dim items = If(result?.Data, New List(Of DisbursementItem)())

                Dim rows = items.Select(
                    Function(d) New With {
                        .DisbursementDate = FormatDisbursementDate(d.DisbursementDate),
                        .VoucherNumber = d.VoucherNumber,
                        .Payee = d.Payee,
                        .ExpenseAccountName = If(d.ExpenseAccount IsNot Nothing,
                            d.ExpenseAccount.Code & " - " & d.ExpenseAccount.Name,
                            d.AccountId.ToString()),
                        .PaymentAccountName = If(d.PaymentAccount IsNot Nothing,
                            d.PaymentAccount.Code & " - " & d.PaymentAccount.Name,
                            If(d.PaymentAccountId.HasValue,
                               d.PaymentAccountId.Value.ToString(), "")),
                        .PaymentMethod = d.PaymentMethod,
                        .Amount = d.Amount,
                        .CheckNumber = d.CheckNumber
                    }).ToList()

                disbursementsGrid.DataSource = Nothing
                disbursementsGrid.DataSource = rows

                SetStatus($"{items.Count} disbursement(s) loaded.")
            End Using

        Catch ex As Exception
            SetStatus("Could not load disbursements: " & ex.Message, True)
        End Try
    End Function

    Private Function FormatDisbursementDate(value As String) As String
        If String.IsNullOrWhiteSpace(value) Then Return ""

        Dim parsed As DateTime
        If DateTime.TryParse(value, CultureInfo.InvariantCulture,
                             DateTimeStyles.AssumeLocal, parsed) Then
            Return parsed.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture)
        End If

        Return value
    End Function

    Private Async Sub SaveClicked(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(payeeInput.Text) Then
            ShowValidation("Enter the payee.", payeeInput)
            Return
        End If

        If expenseAccountCombo.SelectedItem Is Nothing OrElse
           paymentAccountCombo.SelectedItem Is Nothing Then
            MessageBox.Show("Select both the expense/debit account and payment account.",
                "Required Fields", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim amount As Decimal
        If Not Decimal.TryParse(amountInput.Text, NumberStyles.Number,
                                CultureInfo.CurrentCulture, amount) OrElse amount <= 0D Then
            ShowValidation("Enter a valid amount greater than zero.", amountInput)
            Return
        End If

        Dim expenseAccount = DirectCast(expenseAccountCombo.SelectedItem, AccountItem)
        Dim paymentAccount = DirectCast(paymentAccountCombo.SelectedItem, AccountItem)

        If expenseAccount.Id = paymentAccount.Id Then
            MessageBox.Show("The two selected accounts must be different.",
                "Invalid Accounts", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If methodCombo.SelectedItem Is Nothing Then
            MessageBox.Show("Select a payment method.",
                "Required Field", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim payload = New Dictionary(Of String, Object) From {
            {"disbursement_date", dateInput.Value.ToString("yyyy-MM-dd")},
            {"payee", payeeInput.Text.Trim()},
            {"account_id", expenseAccount.Id},
            {"payment_account_id", paymentAccount.Id},
            {"amount", amount},
            {"payment_method", methodCombo.SelectedItem.ToString()},
            {"check_number", If(String.IsNullOrWhiteSpace(checkNumberInput.Text),
                                Nothing, checkNumberInput.Text.Trim())},
            {"remarks", If(String.IsNullOrWhiteSpace(remarksInput.Text),
                           Nothing, remarksInput.Text.Trim())}
        }

        payload("post_now") = ApiSession.HasServerPermissions AndAlso
            ApiSession.CurrentUserPermissions.Contains("cash_disbursements.approve") AndAlso
            ApiSession.CurrentUserPermissions.Contains("cash_disbursements.post")

        Try
            saveButton.Enabled = False
            SetStatus("Saving disbursement...")

            ApiSession.ApplyAuthorization()

            Using response = Await ApiSession.Client.PostAsJsonAsync(
                "cash-disbursements", payload)

                Dim body = Await response.Content.ReadAsStringAsync()

                If Not response.IsSuccessStatusCode Then
                    Throw New Exception(body)
                End If

                Dim result = JsonSerializer.Deserialize(
                    Of ApiResponse(Of DisbursementItem))(body, jsonOptions)

                Dim generatedVoucher As String = If(
                    result?.Data IsNot Nothing,
                    result.Data.VoucherNumber,
                    "")
                Dim successMessage As String = If(
                    String.IsNullOrWhiteSpace(result?.Message),
                    "Disbursement saved successfully.",
                    result.Message)
                If Not String.IsNullOrWhiteSpace(generatedVoucher) Then
                    successMessage &= Environment.NewLine & Environment.NewLine &
                        "Generated voucher number: " & generatedVoucher
                End If

                MessageBox.Show(
                    successMessage,
                    "Disbursement Saved",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information)

                ClearInputs()
                Await LoadDisbursementsAsync()
            End Using

        Catch ex As Exception
            SetStatus("Could not save disbursement: " & ex.Message, True)
            MessageBox.Show(ex.Message, "Save Failed",
                MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            saveButton.Enabled = True
        End Try
    End Sub

    Private Sub ShowValidation(message As String, control As Control)
        MessageBox.Show(message, "Required Field",
            MessageBoxButtons.OK, MessageBoxIcon.Warning)
        control.Focus()
    End Sub

    Private Async Sub RefreshClicked(sender As Object, e As EventArgs)
        Await LoadAccountsAsync()
        Await LoadDisbursementsAsync()
    End Sub

    Private Sub ClearInputs()
        dateInput.Value = Date.Today
        voucherInput.Clear()
        payeeInput.Clear()
        amountInput.Clear()
        checkNumberInput.Clear()
        remarksInput.Clear()
        methodCombo.SelectedIndex = 0

        If expenseAccountCombo.Items.Count > 0 Then
            expenseAccountCombo.SelectedIndex = 0
        End If

        If paymentAccountCombo.Items.Count > 0 Then
            paymentAccountCombo.SelectedIndex = 0
        End If
    End Sub

    Private Sub SetStatus(message As String, Optional isError As Boolean = False)
        statusLabel.Text = message
        statusLabel.ForeColor = If(
            isError, Color.Firebrick, Color.FromArgb(65, 85, 72))
    End Sub
End Class
