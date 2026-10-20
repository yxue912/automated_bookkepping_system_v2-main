Imports System.Drawing
Imports System.Globalization
Imports System.Linq
Imports System.Net.Http
Imports System.Net.Http.Json
Imports System.Text.Json
Imports System.Text.Json.Serialization
Imports System.Windows.Forms
Imports System.Drawing.Printing
Imports System.IO
Imports System.Text

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
    Private ReadOnly deleteButton As New Button()
    Private ReadOnly previewVoucherButton As New Button()
    Private ReadOnly saveVoucherPdfButton As New Button()
    Private ReadOnly voucherPrintDocument As New PrintDocument()
    Private selectedVoucherForPrint As Dictionary(Of String, String)

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

        <JsonPropertyName("status")>
        Public Property Status As String

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

        deleteButton.Text = "Delete Selected"
        deleteButton.Width = 120
        deleteButton.Height = 32
        deleteButton.BackColor = Color.Firebrick
        deleteButton.ForeColor = Color.White
        deleteButton.FlatStyle = FlatStyle.Flat
        deleteButton.FlatAppearance.BorderSize = 0
        deleteButton.Enabled = False
        AddHandler deleteButton.Click, AddressOf DeleteSelectedClicked

        previewVoucherButton.Text = "Preview Voucher"
        previewVoucherButton.Width = 125
        previewVoucherButton.Height = 32
        previewVoucherButton.BackColor = Color.FromArgb(31, 78, 121)
        previewVoucherButton.ForeColor = Color.White
        previewVoucherButton.FlatStyle = FlatStyle.Flat
        previewVoucherButton.FlatAppearance.BorderSize = 0
        AddHandler previewVoucherButton.Click, AddressOf PreviewSelectedVoucherClicked

        saveVoucherPdfButton.Text = "Save Voucher PDF"
        saveVoucherPdfButton.Width = 135
        saveVoucherPdfButton.Height = 32
        saveVoucherPdfButton.BackColor = Color.FromArgb(31, 78, 121)
        saveVoucherPdfButton.ForeColor = Color.White
        saveVoucherPdfButton.FlatStyle = FlatStyle.Flat
        saveVoucherPdfButton.FlatAppearance.BorderSize = 0
        AddHandler saveVoucherPdfButton.Click, AddressOf SaveSelectedVoucherPdfClicked

        AddHandler voucherPrintDocument.PrintPage, AddressOf VoucherPrintDocument_PrintPage
        voucherPrintDocument.DocumentName = "Aqua ARC Cash Disbursement Voucher"
        voucherPrintDocument.DefaultPageSettings.Margins = New Margins(20, 20, 20, 20)
        Try
            voucherPrintDocument.DefaultPageSettings.PaperSize = New PaperSize("Aqua ARC Voucher 80mm", 315, 650)
        Catch
            ' Preview only; direct PDF output below defines its own page size.
        End Try

        Dim buttons As New FlowLayoutPanel With {
            .Dock = DockStyle.Fill,
            .FlowDirection = FlowDirection.RightToLeft,
            .WrapContents = False
        }

        buttons.Controls.Add(CsvExportMenu.CreateExportButton(Me, disbursementsGrid, "Cash Disbursements"))
        buttons.Controls.Add(saveButton)
        buttons.Controls.Add(refreshButton)
        buttons.Controls.Add(deleteButton)
        buttons.Controls.Add(previewVoucherButton)
        buttons.Controls.Add(saveVoucherPdfButton)

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

        AddGridColumn("Id", "Id", "ID")
        disbursementsGrid.Columns("Id").Visible = False
        AddGridColumn("Status", "Status", "Status")
        disbursementsGrid.Columns("Status").Visible = False
        AddGridColumn("Date", "DisbursementDate", "Date")
        AddHandler disbursementsGrid.SelectionChanged, AddressOf DisbursementSelectionChanged
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

    Private Function CaptureSelectedVoucher() As Boolean
        If disbursementsGrid.CurrentRow Is Nothing Then
            MessageBox.Show("Select a saved disbursement from Disbursement History first.",
                            "No Voucher Selected", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return False
        End If

        Dim row = disbursementsGrid.CurrentRow
        selectedVoucherForPrint = New Dictionary(Of String, String) From {
            {"Date", GridCellText(row, "DisbursementDate")},
            {"VoucherNumber", GridCellText(row, "VoucherNumber")},
            {"Payee", GridCellText(row, "Payee")},
            {"ExpenseAccount", GridCellText(row, "ExpenseAccountName")},
            {"PaymentAccount", GridCellText(row, "PaymentAccountName")},
            {"PaymentMethod", GridCellText(row, "PaymentMethod")},
            {"Amount", GridCellText(row, "Amount")},
            {"CheckNumber", GridCellText(row, "CheckNumber")}
        }
        Return True
    End Function

    Private Function GridCellText(row As DataGridViewRow, columnName As String) As String
        If Not disbursementsGrid.Columns.Contains(columnName) Then Return ""
        Dim value = row.Cells(columnName).Value
        If value Is Nothing OrElse value Is DBNull.Value Then Return ""
        Return Convert.ToString(value, CultureInfo.CurrentCulture)
    End Function

    Private Function VoucherValue(key As String) As String
        If selectedVoucherForPrint Is Nothing OrElse Not selectedVoucherForPrint.ContainsKey(key) Then Return ""
        Return If(selectedVoucherForPrint(key), "")
    End Function

    Private Sub PreviewSelectedVoucherClicked(sender As Object, e As EventArgs)
        If Not CaptureSelectedVoucher() Then Return
        Using preview As New PrintPreviewDialog()
            preview.Document = voucherPrintDocument
            preview.Width = 900
            preview.Height = 700
            preview.StartPosition = FormStartPosition.CenterParent
            preview.Text = "Aqua ARC - Disbursement Voucher Preview"
            Try
                preview.ShowDialog(Me)
            Catch ex As Exception
                MessageBox.Show("Could not preview the voucher: " & ex.Message,
                                "Preview Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Using
    End Sub

    Private Sub VoucherPrintDocument_PrintPage(sender As Object, e As PrintPageEventArgs)
        If selectedVoucherForPrint Is Nothing Then
            e.HasMorePages = False
            Return
        End If

        Dim g = e.Graphics
        Dim left As Single = e.MarginBounds.Left
        Dim top As Single = e.MarginBounds.Top
        Dim width As Single = e.MarginBounds.Width
        Using titleFont As New Font("Arial", 13, FontStyle.Bold),
              labelFont As New Font("Arial", 8.5F, FontStyle.Bold),
              bodyFont As New Font("Arial", 8.5F),
              smallFont As New Font("Arial", 7.0F),
              titleBrush As New SolidBrush(Color.FromArgb(23, 54, 93)),
              bodyBrush As New SolidBrush(Color.FromArgb(36, 52, 71)),
              centerFormat As New StringFormat With {.Alignment = StringAlignment.Center}

            g.DrawString("AQUA ARC", titleFont, titleBrush, New RectangleF(left, top, width, 22), centerFormat)
            top += 22
            g.DrawString("CASH DISBURSEMENT VOUCHER", labelFont, titleBrush, New RectangleF(left, top, width, 16), centerFormat)
            top += 22
            g.DrawLine(New Pen(Color.FromArgb(31, 78, 121), 1), left, top, left + width, top)
            top += 10

            Dim items As New List(Of KeyValuePair(Of String, String)) From {
                New KeyValuePair(Of String, String)("Date", VoucherValue("Date")),
                New KeyValuePair(Of String, String)("Voucher No.", VoucherValue("VoucherNumber")),
                New KeyValuePair(Of String, String)("Payee", VoucherValue("Payee")),
                New KeyValuePair(Of String, String)("Expense / Debit Account", VoucherValue("ExpenseAccount")),
                New KeyValuePair(Of String, String)("Payment / Cash Account", VoucherValue("PaymentAccount")),
                New KeyValuePair(Of String, String)("Payment Method", VoucherValue("PaymentMethod")),
                New KeyValuePair(Of String, String)("Check Number", VoucherValue("CheckNumber"))
            }
            For Each item In items
                g.DrawString(item.Key, labelFont, bodyBrush, left, top)
                top += 11
                Dim value = If(String.IsNullOrWhiteSpace(item.Value), "—", item.Value)
                g.DrawString(value, bodyFont, bodyBrush, New RectangleF(left, top, width, 30))
                top += If(value.Length > 30, 24, 15)
            Next
            top += 3
            g.DrawLine(Pens.Gray, left, top, left + width, top)
            top += 10
            g.DrawString("TOTAL PAID", labelFont, titleBrush, left, top)
            top += 14
            Dim amount As Decimal
            Dim amountRaw = VoucherValue("Amount")
            Dim amountText = amountRaw
            If Decimal.TryParse(amountRaw, NumberStyles.Any, CultureInfo.CurrentCulture, amount) Then
                amountText = "PHP " & amount.ToString("#,##0.00", CultureInfo.InvariantCulture)
            End If
            g.DrawString(amountText, New Font("Arial", 12, FontStyle.Bold), titleBrush, left, top)
            top += 28
            g.DrawLine(Pens.Gray, left, top, left + width, top)
            top += 12
            g.DrawString("Prepared / Authorized By", smallFont, bodyBrush, left, top)
            top += 25
            g.DrawLine(Pens.Gray, left, top, left + width, top)
            top += 5
            g.DrawString("Generated from the selected saved disbursement in Aqua ARC.", smallFont, Brushes.Gray, left, top)
        End Using
        e.HasMorePages = False
    End Sub

    Private Sub SaveSelectedVoucherPdfClicked(sender As Object, e As EventArgs)
        If Not CaptureSelectedVoucher() Then Return
        Using dialog As New SaveFileDialog()
            dialog.Title = "Save Disbursement Voucher as PDF"
            dialog.Filter = "PDF files (*.pdf)|*.pdf"
            dialog.FileName = "DisbursementVoucher_" & SafeVoucherFileName(VoucherValue("VoucherNumber")) & ".pdf"
            dialog.DefaultExt = "pdf"
            dialog.AddExtension = True
            If dialog.ShowDialog(Me) <> DialogResult.OK Then Return
            Try
                CreateVoucherPdf(dialog.FileName)
                MessageBox.Show("Voucher PDF saved successfully:" & Environment.NewLine & dialog.FileName,
                                "PDF Saved", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                MessageBox.Show("Could not save the voucher PDF: " & ex.Message,
                                "PDF Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Using
    End Sub

    Private Function SafeVoucherFileName(value As String) As String
        If String.IsNullOrWhiteSpace(value) Then Return DateTime.Now.ToString("yyyyMMdd_HHmmss")
        For Each invalidChar In Path.GetInvalidFileNameChars()
            value = value.Replace(invalidChar, "_"c)
        Next
        Return value.Trim()
    End Function

    Private Function PdfVoucherText(value As String) As String
        If String.IsNullOrWhiteSpace(value) Then Return "-"
        Dim result = value.Replace("₱", "PHP ").Replace("—", "-").Replace("–", "-")
        Dim sb As New StringBuilder()
        For Each ch As Char In result
            If AscW(ch) >= 32 AndAlso AscW(ch) <= 255 Then
                sb.Append(ch)
            ElseIf ch = vbCr OrElse ch = vbLf OrElse ch = vbTab Then
                sb.Append(" ")
            Else
                sb.Append("?")
            End If
        Next
        Return sb.ToString()
    End Function

    Private Function EscapeVoucherPdf(value As String) As String
        Return PdfVoucherText(value).Replace("\", "\\").Replace("(", "\(").Replace(")", "\)")
    End Function

    Private Sub CreateVoucherPdf(filePath As String)
        Dim pageWidth As Double = 226.77 ' 80 mm in PDF points
        Dim margin As Double = 15
        Dim items As New List(Of KeyValuePair(Of String, String)) From {
            New KeyValuePair(Of String, String)("Date", VoucherValue("Date")),
            New KeyValuePair(Of String, String)("Voucher No.", VoucherValue("VoucherNumber")),
            New KeyValuePair(Of String, String)("Payee", VoucherValue("Payee")),
            New KeyValuePair(Of String, String)("Expense / Debit Account", VoucherValue("ExpenseAccount")),
            New KeyValuePair(Of String, String)("Payment / Cash Account", VoucherValue("PaymentAccount")),
            New KeyValuePair(Of String, String)("Payment Method", VoucherValue("PaymentMethod")),
            New KeyValuePair(Of String, String)("Check Number", VoucherValue("CheckNumber"))
        }
        Dim pageHeight As Double = 155 + items.Count * 30
        For Each item In items
            If PdfVoucherText(item.Value).Length > 28 Then pageHeight += 12
        Next

        Dim commands As New StringBuilder()
        Dim y As Double = pageHeight - 25
        commands.AppendLine("BT /F2 13 Tf")
        commands.AppendLine(String.Format(CultureInfo.InvariantCulture, "1 0 0 1 73 {0:0.##} Tm ({1}) Tj ET", y, EscapeVoucherPdf("AQUA ARC")))
        y -= 16
        commands.AppendLine("BT /F2 8 Tf")
        commands.AppendLine(String.Format(CultureInfo.InvariantCulture, "1 0 0 1 42 {0:0.##} Tm ({1}) Tj ET", y, EscapeVoucherPdf("CASH DISBURSEMENT VOUCHER")))
        y -= 10
        commands.AppendLine(String.Format(CultureInfo.InvariantCulture, "0.12 0.31 0.47 RG 0.7 w {0} {1:0.##} m {2:0.##} {1:0.##} l S", margin, y, pageWidth - margin))
        y -= 14

        For Each item In items
            commands.AppendLine("BT /F2 8 Tf")
            commands.AppendLine(String.Format(CultureInfo.InvariantCulture, "1 0 0 1 {0} {1:0.##} Tm ({2}) Tj ET", margin, y, EscapeVoucherPdf(item.Key)))
            y -= 10
            Dim value = PdfVoucherText(item.Value)
            For i As Integer = 0 To value.Length - 1 Step 28
                Dim chunk = value.Substring(i, Math.Min(28, value.Length - i))
                commands.AppendLine("BT /F1 8 Tf")
                commands.AppendLine(String.Format(CultureInfo.InvariantCulture, "1 0 0 1 {0} {1:0.##} Tm ({2}) Tj ET", margin, y, EscapeVoucherPdf(chunk)))
                y -= 11
            Next
            y -= 2
        Next
        commands.AppendLine(String.Format(CultureInfo.InvariantCulture, "0.12 0.31 0.47 RG 0.7 w {0} {1:0.##} m {2:0.##} {1:0.##} l S", margin, y, pageWidth - margin))
        y -= 14
        commands.AppendLine("BT /F2 8 Tf")
        commands.AppendLine(String.Format(CultureInfo.InvariantCulture, "1 0 0 1 {0} {1:0.##} Tm ({2}) Tj ET", margin, y, EscapeVoucherPdf("TOTAL PAID")))
        y -= 16
        Dim amount As Decimal
        Dim rawAmount = VoucherValue("Amount")
        Dim amountText = rawAmount
        If Decimal.TryParse(rawAmount, NumberStyles.Any, CultureInfo.CurrentCulture, amount) Then amountText = "PHP " & amount.ToString("#,##0.00", CultureInfo.InvariantCulture)
        commands.AppendLine("BT /F2 11 Tf")
        commands.AppendLine(String.Format(CultureInfo.InvariantCulture, "1 0 0 1 {0} {1:0.##} Tm ({2}) Tj ET", margin, y, EscapeVoucherPdf(amountText)))
        y -= 20
        commands.AppendLine("BT /F1 7 Tf")
        commands.AppendLine(String.Format(CultureInfo.InvariantCulture, "1 0 0 1 {0} {1:0.##} Tm ({2}) Tj ET", margin, y, EscapeVoucherPdf("Prepared / Authorized By")))
        y -= 25
        commands.AppendLine(String.Format(CultureInfo.InvariantCulture, "0.5 G 0.5 w {0} {1:0.##} m {2:0.##} {1:0.##} l S", margin, y, pageWidth - margin))
        y -= 12
        commands.AppendLine("BT /F1 6 Tf")
        commands.AppendLine(String.Format(CultureInfo.InvariantCulture, "1 0 0 1 {0} {1:0.##} Tm ({2}) Tj ET", margin, y, EscapeVoucherPdf("Generated from Aqua ARC saved disbursement.")))

        Dim contentBytes = Encoding.Latin1.GetBytes(commands.ToString())
        Dim objects As New List(Of Byte()) From {
            Encoding.ASCII.GetBytes("<< /Type /Catalog /Pages 2 0 R >>"),
            Encoding.ASCII.GetBytes("<< /Type /Pages /Kids [3 0 R] /Count 1 >>"),
            Encoding.ASCII.GetBytes(String.Format(CultureInfo.InvariantCulture, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {0:0.##} {1:0.##}] /Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >>", pageWidth, pageHeight)),
            Encoding.ASCII.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"),
            Encoding.ASCII.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>")
        }
        Using stream As New MemoryStream()
            Dim header = Encoding.ASCII.GetBytes("%PDF-1.4" & vbLf)
            stream.Write(header, 0, header.Length)
            Dim offsets As New List(Of Long) From {0}
            For i As Integer = 0 To objects.Count - 1
                offsets.Add(stream.Position)
                Dim head = Encoding.ASCII.GetBytes((i + 1).ToString() & " 0 obj" & vbLf)
                stream.Write(head, 0, head.Length)
                stream.Write(objects(i), 0, objects(i).Length)
                Dim tail = Encoding.ASCII.GetBytes(vbLf & "endobj" & vbLf)
                stream.Write(tail, 0, tail.Length)
            Next
            offsets.Add(stream.Position)
            Dim cHead = Encoding.ASCII.GetBytes("6 0 obj" & vbLf & "<< /Length " & contentBytes.Length.ToString(CultureInfo.InvariantCulture) & " >>" & vbLf & "stream" & vbLf)
            stream.Write(cHead, 0, cHead.Length)
            stream.Write(contentBytes, 0, contentBytes.Length)
            Dim cTail = Encoding.ASCII.GetBytes(vbLf & "endstream" & vbLf & "endobj" & vbLf)
            stream.Write(cTail, 0, cTail.Length)
            Dim xrefPos = stream.Position
            Dim xref As New StringBuilder()
            xref.AppendLine("xref")
            xref.AppendLine("0 7")
            xref.AppendLine("0000000000 65535 f ")
            For i As Integer = 1 To 6
                xref.AppendLine(offsets(i).ToString("0000000000", CultureInfo.InvariantCulture) & " 00000 n ")
            Next
            xref.AppendLine("trailer")
            xref.AppendLine("<< /Size 7 /Root 1 0 R >>")
            xref.AppendLine("startxref")
            xref.AppendLine(xrefPos.ToString(CultureInfo.InvariantCulture))
            xref.Append("%%EOF")
            Dim tailBytes = Encoding.ASCII.GetBytes(xref.ToString())
            stream.Write(tailBytes, 0, tailBytes.Length)
            File.WriteAllBytes(filePath, stream.ToArray())
        End Using
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
                        .Id = d.Id,
                        .Status = If(String.IsNullOrWhiteSpace(d.Status), "Unknown", d.Status),
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

    Private Sub DisbursementSelectionChanged(sender As Object, e As EventArgs)
        deleteButton.Enabled = False
        If disbursementsGrid.CurrentRow Is Nothing Then Return
        Dim statusValue = disbursementsGrid.CurrentRow.Cells("Status").Value
        deleteButton.Enabled = statusValue IsNot Nothing AndAlso
            String.Equals(Convert.ToString(statusValue), "Draft", StringComparison.OrdinalIgnoreCase)
    End Sub

    Private Async Sub DeleteSelectedClicked(sender As Object, e As EventArgs)
        If disbursementsGrid.CurrentRow Is Nothing Then
            MessageBox.Show("Select a disbursement from Disbursement History first.",
                "No Entry Selected", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim row = disbursementsGrid.CurrentRow
        Dim idValue = row.Cells("Id").Value
        Dim statusValue = Convert.ToString(row.Cells("Status").Value)
        If idValue Is Nothing OrElse Not String.Equals(statusValue, "Draft", StringComparison.OrdinalIgnoreCase) Then
            MessageBox.Show("Only Draft disbursements can be deleted.", "Cannot Delete Entry",
                MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim id As Integer
        If Not Integer.TryParse(Convert.ToString(idValue), id) Then
            SetStatus("Could not identify the selected disbursement.", True)
            Return
        End If

        Dim voucher = GridCellText(row, "Voucher")
        If MessageBox.Show("Delete Draft disbursement " & voucher & "?" & Environment.NewLine & Environment.NewLine &
                           "Its linked Draft journal entry will also be deleted. This cannot be undone.",
                           "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) <> DialogResult.Yes Then Return

        Try
            deleteButton.Enabled = False
            saveButton.Enabled = False
            ApiSession.ApplyAuthorization()
            Using response = Await ApiSession.Client.DeleteAsync("cash-disbursements/" & id.ToString(CultureInfo.InvariantCulture))
                Dim body = Await response.Content.ReadAsStringAsync()
                If Not response.IsSuccessStatusCode Then Throw New Exception(body)
                Dim result = JsonSerializer.Deserialize(Of ApiResponse(Of Object))(body, jsonOptions)
                MessageBox.Show(If(String.IsNullOrWhiteSpace(result?.Message), "Draft disbursement deleted successfully.", result.Message),
                    "Entry Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End Using
            Await LoadDisbursementsAsync()
        Catch ex As Exception
            SetStatus("Could not delete disbursement: " & ex.Message, True)
            MessageBox.Show(ex.Message, "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            saveButton.Enabled = True
            DisbursementSelectionChanged(Nothing, EventArgs.Empty)
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
