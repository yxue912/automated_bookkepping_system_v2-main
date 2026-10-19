Imports System.IO
Imports System.IO.Compression
Imports System.Text
Imports System.Xml
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Linq

''' <summary>Visible export toolbar and formatted Excel workbook export for read-only grids and reports.</summary>
Public Module CsvExportMenu
    Public Sub Attach(form As Form, ParamArray exports As Tuple(Of String, DataGridView)())
        ' Kept for compatibility with older forms. Export controls are now created
        ' inside each form's own action layout via CreateExportButton.
    End Sub

    Public Function CreateExportButton(owner As Form, grid As DataGridView, suggestedName As String) As Button
        Dim button As New Button With {
            .Text = "Export " & suggestedName,
            .AutoSize = True,
            .Height = 32,
            .MinimumSize = New Size(125, 32),
            .Padding = New Padding(10, 0, 10, 0),
            .Margin = New Padding(6, 2, 3, 2),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.FromArgb(39, 112, 74),
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 9.0F, FontStyle.Bold),
            .Cursor = Cursors.Hand,
            .UseVisualStyleBackColor = False
        }
        button.FlatAppearance.BorderSize = 0
        AddHandler button.Click, Sub(sender, e) ExportGrid(owner, grid, suggestedName)
        Return button
    End Function

    Public Sub ExportGrid(owner As Form, grid As DataGridView, suggestedName As String)
        If grid Is Nothing Then Return
        Using dialog As New SaveFileDialog With {
            .Title = "Export professional Excel workbook",
            .Filter = "Excel Workbook (*.xlsx)|*.xlsx|CSV file (*.csv)|*.csv",
            .DefaultExt = "xlsx", .AddExtension = True,
            .FileName = MakeFileName(suggestedName) & ".xlsx"
        }
            If dialog.ShowDialog(owner) <> DialogResult.OK Then Return
            Try
                If String.Equals(Path.GetExtension(dialog.FileName), ".csv", StringComparison.OrdinalIgnoreCase) Then
                    ExportCsv(dialog.FileName, grid)
                Else
                    ExportXlsx(dialog.FileName, grid, suggestedName)
                End If
                MessageBox.Show(owner, "The export was completed successfully.",
                                "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                MessageBox.Show(owner, "Unable to export data." & Environment.NewLine & ex.Message,
                                "Aqua ARC", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Using
    End Sub

    Private Sub ExportCsv(fileName As String, grid As DataGridView)
        Dim columns = VisibleColumns(grid)
        Using writer As New StreamWriter(fileName, False, New UTF8Encoding(True))
            writer.WriteLine(String.Join(",", columns.Select(Function(c) EscapeCsv(c.HeaderText))))
            For Each row As DataGridViewRow In grid.Rows
                If row.IsNewRow OrElse Not row.Visible Then Continue For
                writer.WriteLine(String.Join(",", columns.Select(Function(c) EscapeCsv(If(row.Cells(c.Index).FormattedValue, String.Empty).ToString()))))
            Next
        End Using
    End Sub

    ' Creates a standards-compliant .xlsx package using the Open XML parts, without third-party dependencies.
    Private Sub ExportXlsx(fileName As String, grid As DataGridView, reportTitle As String)
        Dim columns = VisibleColumns(grid)
        Dim rows As New List(Of DataGridViewRow)
        For Each row As DataGridViewRow In grid.Rows
            If Not row.IsNewRow AndAlso row.Visible Then rows.Add(row)
        Next
        If File.Exists(fileName) Then File.Delete(fileName)
        Using archive = ZipFile.Open(fileName, ZipArchiveMode.Create)
            AddXml(archive, "[Content_Types].xml", "<?xml version=""1.0"" encoding=""UTF-8""?><Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types""><Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/><Default Extension=""xml"" ContentType=""application/xml""/><Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/><Override PartName=""/xl/worksheets/sheet1.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/><Override PartName=""/xl/styles.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml""/></Types>")
            AddXml(archive, "_rels/.rels", "<?xml version=""1.0"" encoding=""UTF-8""?><Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships""><Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/></Relationships>")
            AddXml(archive, "xl/workbook.xml", "<?xml version=""1.0"" encoding=""UTF-8""?><workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships""><sheets><sheet name=""" & XmlEscape(Left(MakeSheetName(reportTitle), 31)) & """ sheetId=""1"" r:id=""rId1""/></sheets></workbook>")
            AddXml(archive, "xl/_rels/workbook.xml.rels", "<?xml version=""1.0"" encoding=""UTF-8""?><Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships""><Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet1.xml""/><Relationship Id=""rId2"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"" Target=""styles.xml""/></Relationships>")
            AddXml(archive, "xl/styles.xml", "<?xml version=""1.0"" encoding=""UTF-8""?><styleSheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main""><fonts count=""3""><font><sz val=""11""/><name val=""Calibri""/></font><font><b/><color rgb=""FFFFFFFF""/><sz val=""11""/><name val=""Calibri""/></font><font><b/><color rgb=""FF255B40""/><sz val=""16""/><name val=""Calibri""/></font></fonts><fills count=""3""><fill><patternFill patternType=""none""/></fill><fill><patternFill patternType=""gray125""/></fill><fill><patternFill patternType=""solid""><fgColor rgb=""FF255B40""/><bgColor indexed=""64""/></patternFill></fill></fills><borders count=""2""><border/><border><bottom style=""thin""><color rgb=""FFD5E3D9""/></bottom></border></borders><cellStyleXfs count=""1""><xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0""/></cellStyleXfs><cellXfs count=""4""><xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0"" xfId=""0""/><xf numFmtId=""0"" fontId=""1"" fillId=""2"" borderId=""1"" xfId=""0"" applyFont=""1"" applyFill=""1""/><xf numFmtId=""4"" fontId=""0"" fillId=""0"" borderId=""1"" xfId=""0"" applyNumberFormat=""1""/><xf numFmtId=""14"" fontId=""0"" fillId=""0"" borderId=""1"" xfId=""0"" applyNumberFormat=""1""/></cellXfs><cellStyles count=""1""><cellStyle name=""Normal"" xfId=""0"" builtinId=""0""/></cellStyles></styleSheet>")

            Dim sb As New StringBuilder()
            sb.Append("<?xml version=""1.0"" encoding=""UTF-8""?><worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main""><sheetViews><sheetView workbookViewId=""0""><pane ySplit=""1"" topLeftCell=""A2"" activePane=""bottomLeft"" state=""frozen""/></sheetView></sheetViews><cols>")
            For i = 0 To columns.Count - 1
                Dim maxLen = Math.Max(12, columns(i).HeaderText.Length + 2)
                For Each row In rows
                    Dim value = If(row.Cells(columns(i).Index).FormattedValue, String.Empty).ToString()
                    maxLen = Math.Max(maxLen, Math.Min(42, value.Length + 2))
                Next
                sb.Append("<col min=""").Append(i + 1).Append(""" max=""").Append(i + 1).Append(""" width=""").Append(Math.Min(42, maxLen)).Append(""" customWidth=""1""/>")
            Next
            sb.Append("</cols><sheetData><row r=""1"" ht=""24"" customHeight=""1"">")
            For i = 0 To columns.Count - 1
                sb.Append(CellXml(i, 1, columns(i).HeaderText, 1))
            Next
            sb.Append("</row>")
            For r = 0 To rows.Count - 1
                sb.Append("<row r=""").Append(r + 2).Append(""">")
                For c = 0 To columns.Count - 1
                    Dim raw = If(rows(r).Cells(columns(c).Index).FormattedValue, String.Empty).ToString()
                    Dim header = columns(c).HeaderText.ToLowerInvariant()
                    Dim style = If(header.Contains("date"), 3, If(header.Contains("amount") OrElse header.Contains("debit") OrElse header.Contains("credit") OrElse header.Contains("balance") OrElse header.Contains("total") OrElse header.Contains("cost") OrElse header.Contains("price"), 2, 0))
                    sb.Append(CellXml(c, r + 2, raw, style))
                Next
                sb.Append("</row>")
            Next
            sb.Append("</sheetData><autoFilter ref=""A1:").Append(ColumnName(Math.Max(1, columns.Count))).Append(Math.Max(1, rows.Count + 1)).Append("""/></worksheet>")
            AddXml(archive, "xl/worksheets/sheet1.xml", sb.ToString())
        End Using
    End Sub

    Private Function CellXml(columnIndex As Integer, rowIndex As Integer, value As String, style As Integer) As String
        Dim reference = ColumnName(columnIndex + 1) & rowIndex.ToString()
        Return "<c r=""" & reference & """ s=""" & style.ToString() & """ t=""inlineStr""><is><t xml:space=""preserve"">" & XmlEscape(If(value, String.Empty)) & "</t></is></c>"
    End Function

    Private Sub AddXml(archive As ZipArchive, path As String, content As String)
        Dim entry = archive.CreateEntry(path, CompressionLevel.Optimal)
        Using stream = entry.Open(), writer As New StreamWriter(stream, New UTF8Encoding(False))
            writer.Write(content)
        End Using
    End Sub

    Private Function VisibleColumns(grid As DataGridView) As List(Of DataGridViewColumn)
        Return grid.Columns.Cast(Of DataGridViewColumn)().Where(Function(c) c.Visible).OrderBy(Function(c) c.DisplayIndex).ToList()
    End Function

    Private Function ColumnName(number As Integer) As String
        Dim result As String = ""
        While number > 0
            number -= 1
            result = ChrW(65 + (number Mod 26)) & result
            number \= 26
        End While
        Return result
    End Function

    Private Function XmlEscape(value As String) As String
        Return If(value, String.Empty).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("""", "&quot;").Replace("'", "&apos;")
    End Function

    Private Function MakeSheetName(value As String) As String
        Dim result = If(value, "Report")
        For Each ch In New Char() {"["c, "]"c, ":"c, "*"c, "?"c, "/"c, "\"c}
            result = result.Replace(ch, " "c)
        Next
        Return If(String.IsNullOrWhiteSpace(result.Trim()), "Report", result.Trim())
    End Function

    Private Function EscapeCsv(value As String) As String
        Dim text = If(value, String.Empty)
        Dim quote As String = ChrW(34).ToString()
        If text.Contains(",") OrElse text.Contains(quote) OrElse text.Contains(vbCr) OrElse text.Contains(vbLf) Then
            Return quote & text.Replace(quote, quote & quote) & quote
        End If
        Return text
    End Function

    Private Function MakeFileName(value As String) As String
        Dim result = If(value, "Export").Trim().Replace(" ", "_")
        For Each ch In Path.GetInvalidFileNameChars()
            result = result.Replace(ch, "_"c)
        Next
        Return If(String.IsNullOrWhiteSpace(result), "Export", result)
    End Function
End Module
