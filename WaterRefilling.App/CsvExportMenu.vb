Imports System.IO
Imports System.Text
Imports System.Windows.Forms

''' <summary>Reusable CSV export menu for read-only grids and reports.</summary>
Public Module CsvExportMenu
    Public Sub Attach(form As Form, ParamArray exports As Tuple(Of String, DataGridView)())
        If form Is Nothing OrElse exports Is Nothing OrElse exports.Length = 0 Then Return

        Dim menu As New MenuStrip With {.Dock = DockStyle.Top, .Name = "csvExportMenu"}
        Dim csvRoot As New ToolStripMenuItem("CSV Export")
        For Each item In exports
            If item Is Nothing OrElse item.Item2 Is Nothing Then Continue For
            Dim grid = item.Item2
            Dim label = item.Item1
            Dim menuItem As New ToolStripMenuItem(label)
            AddHandler menuItem.Click,
                Sub(sender, e)
                    ExportGrid(form, grid, label)
                End Sub
            csvRoot.DropDownItems.Add(menuItem)
        Next
        If csvRoot.DropDownItems.Count = 0 Then Return
        menu.Items.Add(csvRoot)
        ' Keep the export menu above the form's full-size content panel.
        ' WinForms docking follows z-order, so explicitly place the menu first.
        form.Controls.Add(menu)
        form.Controls.SetChildIndex(menu, 0)
        menu.Visible = True
        menu.BringToFront()
    End Sub

    Public Sub ExportGrid(owner As Form, grid As DataGridView, suggestedName As String)
        If grid Is Nothing Then Return
        Using dialog As New SaveFileDialog With {
            .Title = "Export CSV",
            .Filter = "CSV files (*.csv)|*.csv",
            .DefaultExt = "csv",
            .AddExtension = True,
            .FileName = MakeFileName(suggestedName) & ".csv"
        }
            If dialog.ShowDialog(owner) <> DialogResult.OK Then Return
            Try
                Dim visibleColumns = grid.Columns.Cast(Of DataGridViewColumn)().
                    Where(Function(c) c.Visible).OrderBy(Function(c) c.DisplayIndex).ToList()
                Using writer As New StreamWriter(dialog.FileName, False, New UTF8Encoding(True))
                    writer.WriteLine(String.Join(",", visibleColumns.Select(Function(c) EscapeCsv(c.HeaderText))))
                    For Each row As DataGridViewRow In grid.Rows
                        If row.IsNewRow OrElse Not row.Visible Then Continue For
                        writer.WriteLine(String.Join(",", visibleColumns.Select(
                            Function(c) EscapeCsv(If(row.Cells(c.Index).FormattedValue, String.Empty).ToString()))))
                    Next
                End Using
                MessageBox.Show(owner, "CSV exported successfully.", "Aqua ARC", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                MessageBox.Show(owner, "Unable to export CSV." & Environment.NewLine & ex.Message,
                                "Aqua ARC", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Using
    End Sub

    Private Function EscapeCsv(value As String) As String
        Dim text = If(value, String.Empty)
        If text.Contains(",") OrElse text.Contains("""") OrElse text.Contains(vbCr) OrElse text.Contains(vbLf) Then
            Return """" & text.Replace("""", """""") & """"
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
