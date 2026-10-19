Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Globalization
Imports System.Net.Http
Imports System.Text
Imports System.Text.Json
Imports System.Threading.Tasks
Imports System.Windows.Forms

Public Class FixedAssetsForm
    Inherits Form

    Private ReadOnly primaryGreen As Color = Color.FromArgb(46, 157, 88)
    Private ReadOnly deepGreen As Color = Color.FromArgb(23, 107, 58)
    Private ReadOnly pageBackground As Color = Color.FromArgb(245, 250, 245)
    Private ReadOnly textColor As Color = Color.FromArgb(32, 51, 40)

    Private ReadOnly assetCodeInput As New TextBox()
    Private ReadOnly nameInput As New TextBox()
    Private ReadOnly categoryInput As New TextBox()
    Private ReadOnly categorySelectButton As New Button()
    Private ReadOnly selectedAssetCategories As New List(Of String)()
    Private ReadOnly acquisitionDateInput As New DateTimePicker()
    Private ReadOnly acquisitionCostInput As New TextBox()
    Private ReadOnly usefulLifeInput As New TextBox()
    Private ReadOnly salvageValueInput As New TextBox()
    Private ReadOnly locationInput As New TextBox()
    Private ReadOnly accountIdInput As New TextBox()
    Private ReadOnly descriptionInput As New TextBox()
    Private ReadOnly activeInput As New CheckBox()

    Private ReadOnly assetsGrid As New DataGridView()
    Private ReadOnly assetSearchInput As New TextBox()
    Private ReadOnly clearSearchButton As New Button()
    Private ReadOnly categoryFilterButton As New Button()
    Private ReadOnly selectedCategoryFilters As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
    Private ReadOnly statusLabel As New Label()

    Private ReadOnly saveButton As New Button()
    Private ReadOnly updateButton As New Button()
    Private ReadOnly deleteButton As New Button()
    Private ReadOnly clearButton As New Button()
    Private ReadOnly refreshButton As New Button()

    Private selectedAssetId As Integer? = Nothing
    Private isLoading As Boolean = False
    Private isSaving As Boolean = False
    Private isDeleting As Boolean = False

    Private ReadOnly assetCategories As String() = {
        "Water Purification Equipment",
        "Water Storage Equipment",
        "Water Pumps",
        "Water Treatment Equipment",
        "Filling Equipment",
        "Plumbing and Piping",
        "Electrical Equipment",
        "Computers and IT Equipment",
        "Office Equipment",
        "Furniture and Fixtures",
        "Vehicles",
        "Buildings and Improvements",
        "Leasehold Improvements",
        "Tools and Maintenance Equipment",
        "Safety and Security Equipment",
        "Laboratory and Testing Equipment",
        "Communication Equipment",
        "Other Fixed Assets"
    }

    Public Sub New()
        Me.Text = "Aqua ARC | Fixed Assets"
        Me.StartPosition = FormStartPosition.CenterParent
        Me.MinimumSize = New Size(900, 600)
        Me.WindowState = FormWindowState.Maximized
        Me.BackColor = pageBackground
        Me.Font = New Font("Segoe UI", 9.0F)
        Me.AutoScaleMode = AutoScaleMode.Dpi

        BuildInterface()

        AddHandler Me.Shown, AddressOf FixedAssetsForm_Shown
    End Sub

    Private Async Sub FixedAssetsForm_Shown(
        sender As Object,
        e As EventArgs)

        Await LoadAssetsAsync()
    End Sub

    Private Sub BuildInterface()
        Dim root As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 3,
            .Padding = New Padding(18),
            .BackColor = pageBackground
        }

        root.RowStyles.Add(New RowStyle(SizeType.Absolute, 72))
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 58))
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 42))

        ' Heading
        Dim heading As New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = pageBackground
        }

        Dim title As New Label With {
            .Text = "Fixed Assets",
            .Font = New Font("Segoe UI", 22, FontStyle.Bold),
            .ForeColor = deepGreen,
            .AutoSize = True,
            .Location = New Point(0, 0)
        }

        Dim subtitle As New Label With {
            .Text = "Manage equipment, property, and other business assets.",
            .Font = New Font("Segoe UI", 9.5F),
            .ForeColor = Color.FromArgb(95, 115, 101),
            .AutoSize = True,
            .Location = New Point(3, 39)
        }

        heading.Controls.Add(title)
        heading.Controls.Add(subtitle)
        root.Controls.Add(heading, 0, 0)

        ' Asset editor
        Dim editorCard As New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.White,
            .Padding = New Padding(16),
            .Margin = New Padding(0, 0, 0, 12),
            .BorderStyle = BorderStyle.FixedSingle
        }

        Dim editorLayout As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 4,
            .RowCount = 6,
            .BackColor = Color.White
        }

        editorLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 16))
        editorLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 34))
        editorLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 16))
        editorLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 34))

        For i As Integer = 0 To 4
            editorLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 53))
        Next

        editorLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

        ConfigureTextBox(assetCodeInput)
        assetCodeInput.ReadOnly = True
        assetCodeInput.BackColor = Color.FromArgb(240, 245, 240)
        assetCodeInput.PlaceholderText = "Generated automatically when saved"
        ConfigureTextBox(nameInput)
        ConfigureTextBox(acquisitionCostInput)
        ConfigureTextBox(usefulLifeInput)
        ConfigureTextBox(salvageValueInput)
        ConfigureTextBox(locationInput)
        ConfigureTextBox(accountIdInput)

        categoryInput.Dock = DockStyle.Fill
        categoryInput.Font = New Font("Segoe UI", 9.5F)
        categoryInput.BackColor = Color.White
        categoryInput.ForeColor = textColor
        categoryInput.ReadOnly = True
        categoryInput.PlaceholderText = "Select one or more categories"
        categoryInput.Margin = New Padding(3, 8, 6, 8)

        categorySelectButton.Text = "Select..."
        categorySelectButton.Dock = DockStyle.Fill
        categorySelectButton.Font = New Font("Segoe UI", 9.0F)
        categorySelectButton.BackColor = primaryGreen
        categorySelectButton.ForeColor = Color.White
        categorySelectButton.FlatStyle = FlatStyle.Flat
        categorySelectButton.FlatAppearance.BorderSize = 0
        categorySelectButton.Margin = New Padding(0, 8, 10, 8)
        AddHandler categorySelectButton.Click, AddressOf CategorySelectButton_Click

        Dim categoryPanel As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 2,
            .RowCount = 1,
            .BackColor = Color.White,
            .Margin = New Padding(0)
        }
        categoryPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        categoryPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 92))
        categoryPanel.Controls.Add(categoryInput, 0, 0)
        categoryPanel.Controls.Add(categorySelectButton, 1, 0)

        acquisitionDateInput.Dock = DockStyle.Fill
        acquisitionDateInput.Font = New Font("Segoe UI", 9.5F)
        acquisitionDateInput.Format = DateTimePickerFormat.Short
        acquisitionDateInput.ShowCheckBox = True
        acquisitionDateInput.Checked = False

        descriptionInput.Dock = DockStyle.Fill
        descriptionInput.Multiline = True
        descriptionInput.ScrollBars = ScrollBars.Vertical
        descriptionInput.Font = New Font("Segoe UI", 9.5F)

        activeInput.Text = "Asset is active"
        activeInput.Checked = True
        activeInput.AutoSize = True
        activeInput.ForeColor = textColor
        activeInput.Anchor = AnchorStyles.Left

        AddField(editorLayout, "Asset Code *", assetCodeInput, 0, 0)
        AddField(editorLayout, "Asset Name *", nameInput, 2, 0)
        AddField(editorLayout, "Category *", categoryPanel, 0, 1)
        AddField(editorLayout, "Acquisition Date", acquisitionDateInput, 2, 1)
        AddField(editorLayout, "Acquisition Cost *", acquisitionCostInput, 0, 2)
        AddField(editorLayout, "Useful Life (Years) *", usefulLifeInput, 2, 2)
        AddField(editorLayout, "Salvage Value", salvageValueInput, 0, 3)
        AddField(editorLayout, "Location", locationInput, 2, 3)
        AddField(editorLayout, "Account ID (Optional)", accountIdInput, 0, 4)

        editorLayout.Controls.Add(activeInput, 2, 4)
        editorLayout.SetColumnSpan(activeInput, 2)

        Dim descriptionLabel As New Label With {
            .Text = "Description",
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft,
            .ForeColor = textColor
        }

        editorLayout.Controls.Add(descriptionLabel, 0, 5)
        editorLayout.Controls.Add(descriptionInput, 1, 5)
        editorLayout.SetColumnSpan(descriptionInput, 3)

        editorCard.Controls.Add(editorLayout)
        root.Controls.Add(editorCard, 0, 1)

        ' Registered assets panel
        Dim listCard As New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.White,
            .Padding = New Padding(12),
            .Margin = New Padding(0),
            .BorderStyle = BorderStyle.FixedSingle
        }

        Dim listLayout As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 4,
            .BackColor = Color.White
        }

        listLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 38))
        listLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 42))
        listLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        listLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 46))

        Dim listTitle As New Label With {
            .Text = "Registered Assets",
            .Font = New Font("Segoe UI", 13, FontStyle.Bold),
            .ForeColor = deepGreen,
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft
        }

        ' Search bar
        Dim searchPanel As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 3,
            .RowCount = 1,
            .BackColor = Color.White,
            .Margin = New Padding(0, 0, 0, 4)
        }

        searchPanel.ColumnStyles.Add(
            New ColumnStyle(SizeType.Percent, 100))
        searchPanel.ColumnStyles.Add(
            New ColumnStyle(SizeType.Absolute, 165))
        searchPanel.ColumnStyles.Add(
            New ColumnStyle(SizeType.Absolute, 115))

        assetSearchInput.Dock = DockStyle.Fill
        assetSearchInput.Font = New Font("Segoe UI", 9.5F)
        assetSearchInput.Margin = New Padding(0, 5, 8, 5)
        assetSearchInput.PlaceholderText =
            "Search asset code, name, or category..."

        categoryFilterButton.Text = "Filter Categories"
        categoryFilterButton.Dock = DockStyle.Fill
        categoryFilterButton.Margin = New Padding(0, 4, 8, 4)
        categoryFilterButton.BackColor = Color.FromArgb(23, 107, 58)
        categoryFilterButton.ForeColor = Color.White
        categoryFilterButton.FlatStyle = FlatStyle.Flat
        categoryFilterButton.FlatAppearance.BorderSize = 0
        categoryFilterButton.Cursor = Cursors.Hand

        clearSearchButton.Text = "Clear Filters"
        clearSearchButton.Dock = DockStyle.Fill
        clearSearchButton.Margin = New Padding(0, 4, 0, 4)
        clearSearchButton.BackColor = Color.FromArgb(100, 116, 106)
        clearSearchButton.ForeColor = Color.White
        clearSearchButton.FlatStyle = FlatStyle.Flat
        clearSearchButton.FlatAppearance.BorderSize = 0
        clearSearchButton.Cursor = Cursors.Hand

        searchPanel.Controls.Add(assetSearchInput, 0, 0)
        searchPanel.Controls.Add(categoryFilterButton, 1, 0)
        searchPanel.Controls.Add(clearSearchButton, 2, 0)

        AddHandler categoryFilterButton.Click, AddressOf CategoryFilterButton_Click

        ConfigureGrid()

        listLayout.Controls.Add(listTitle, 0, 0)
        listLayout.Controls.Add(searchPanel, 0, 1)
        listLayout.Controls.Add(assetsGrid, 0, 2)

        ' Status and action buttons
        Dim bottomPanel As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 2,
            .RowCount = 1,
            .BackColor = Color.White
        }

        bottomPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 35))
        bottomPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 65))

        statusLabel.Text = "Ready."
        statusLabel.Dock = DockStyle.Fill
        statusLabel.TextAlign = ContentAlignment.MiddleLeft
        statusLabel.ForeColor = Color.FromArgb(95, 115, 101)
        statusLabel.AutoEllipsis = True

        Dim buttonFlow As New FlowLayoutPanel With {
            .Dock = DockStyle.Fill,
            .FlowDirection = FlowDirection.RightToLeft,
            .WrapContents = False,
            .BackColor = Color.White,
            .AutoScroll = True
        }

        ConfigureButton(saveButton, "Save New", primaryGreen)
        ConfigureButton(updateButton, "Update Selected", deepGreen)
        ConfigureButton(deleteButton, "Deactivate", Color.FromArgb(190, 65, 65))
        ConfigureButton(clearButton, "Clear Form", Color.FromArgb(100, 116, 106))
        ConfigureButton(refreshButton, "Refresh List", Color.FromArgb(64, 125, 86))

        saveButton.Width = 88
        updateButton.Width = 120
        deleteButton.Width = 100
        clearButton.Width = 95
        refreshButton.Width = 100

        buttonFlow.Controls.Add(CsvExportMenu.CreateExportButton(Me, assetsGrid, "Fixed Assets"))
        buttonFlow.Controls.Add(refreshButton)
        buttonFlow.Controls.Add(clearButton)
        buttonFlow.Controls.Add(deleteButton)
        buttonFlow.Controls.Add(updateButton)
        buttonFlow.Controls.Add(saveButton)

        bottomPanel.Controls.Add(statusLabel, 0, 0)
        bottomPanel.Controls.Add(buttonFlow, 1, 0)

        listLayout.Controls.Add(bottomPanel, 0, 3)
        listCard.Controls.Add(listLayout)
        root.Controls.Add(listCard, 0, 2)

        Me.Controls.Add(root)

        ' Event handlers
        AddHandler saveButton.Click, AddressOf SaveButton_Click
        AddHandler updateButton.Click, AddressOf UpdateButton_Click
        AddHandler deleteButton.Click, AddressOf DeleteButton_Click
        AddHandler clearButton.Click, AddressOf ClearButton_Click
        AddHandler refreshButton.Click, AddressOf RefreshButton_Click
        AddHandler assetsGrid.CellDoubleClick, AddressOf AssetsGrid_CellDoubleClick
        AddHandler assetsGrid.SelectionChanged, AddressOf AssetsGrid_SelectionChanged
        AddHandler assetSearchInput.TextChanged, AddressOf AssetSearchInput_TextChanged
        AddHandler clearSearchButton.Click, AddressOf ClearSearchButton_Click

        updateButton.Enabled = False
        deleteButton.Enabled = False
    End Sub

    Private Sub ConfigureTextBox(box As TextBox)
        box.Dock = DockStyle.Fill
        box.Font = New Font("Segoe UI", 9.5F)
        box.Margin = New Padding(3, 8, 10, 8)
    End Sub

    Private Sub AddField(
        layout As TableLayoutPanel,
        caption As String,
        control As Control,
        labelColumn As Integer,
        row As Integer)

        Dim fieldLabel As New Label With {
            .Text = caption,
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft,
            .ForeColor = textColor,
            .Margin = New Padding(3, 4, 5, 4)
        }

        control.Margin = New Padding(3, 8, 10, 8)

        layout.Controls.Add(fieldLabel, labelColumn, row)
        layout.Controls.Add(control, labelColumn + 1, row)
    End Sub

    Private Sub ConfigureButton(
        button As Button,
        caption As String,
        backgroundColor As Color)

        button.Text = caption
        button.Width = 105
        button.Height = 32
        button.Margin = New Padding(3, 5, 3, 5)
        button.FlatStyle = FlatStyle.Flat
        button.FlatAppearance.BorderSize = 0
        button.BackColor = backgroundColor
        button.ForeColor = Color.White
        button.Font = New Font("Segoe UI", 8.5F, FontStyle.Bold)
        button.Cursor = Cursors.Hand
        button.UseVisualStyleBackColor = False
    End Sub

    Private Sub ConfigureGrid()
        assetsGrid.Dock = DockStyle.Fill
        assetsGrid.BackgroundColor = Color.White
        assetsGrid.BorderStyle = BorderStyle.None
        assetsGrid.AllowUserToAddRows = False
        assetsGrid.AllowUserToDeleteRows = False
        assetsGrid.ReadOnly = True
        assetsGrid.MultiSelect = False
        assetsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        assetsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        assetsGrid.RowHeadersVisible = False
        assetsGrid.EnableHeadersVisualStyles = False
        assetsGrid.ColumnHeadersDefaultCellStyle.BackColor = deepGreen
        assetsGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
        assetsGrid.ColumnHeadersDefaultCellStyle.Font =
            New Font("Segoe UI", 9, FontStyle.Bold)
        assetsGrid.DefaultCellStyle.SelectionBackColor =
            Color.FromArgb(218, 241, 224)
        assetsGrid.DefaultCellStyle.SelectionForeColor = textColor
        assetsGrid.AlternatingRowsDefaultCellStyle.BackColor =
            Color.FromArgb(247, 251, 247)
        assetsGrid.RowTemplate.Height = 30

        assetsGrid.Columns.Clear()

        Dim idColumn As New DataGridViewTextBoxColumn With {
            .Name = "id",
            .HeaderText = "ID",
            .Visible = False
        }

        assetsGrid.Columns.Add(idColumn)
        assetsGrid.Columns.Add("asset_code", "Asset Code")
        assetsGrid.Columns.Add("name", "Asset Name")
        assetsGrid.Columns.Add("category", "Category")
        assetsGrid.Columns.Add("acquisition_cost", "Acquisition Cost")
        assetsGrid.Columns.Add("is_active", "Status")
    End Sub

    Private Async Function LoadAssetsAsync() As Task
        If isLoading OrElse isSaving OrElse isDeleting Then Return

        isLoading = True
        refreshButton.Enabled = False
        statusLabel.Text = "Loading assets..."

        Try
            ApiSession.ApplyAuthorization()

            Using response As HttpResponseMessage =
                Await ApiSession.Client.GetAsync("fixed-assets")

                Dim json As String =
                    Await response.Content.ReadAsStringAsync()

                If Not response.IsSuccessStatusCode Then
                    Throw New Exception(
                        "Could not load assets. Server response: " & json)
                End If

                Using doc As JsonDocument = JsonDocument.Parse(json)
                    Dim root As JsonElement = doc.RootElement
                    Dim data As JsonElement = root

                    If root.ValueKind = JsonValueKind.Object Then
                        Dim nested As JsonElement

                        If root.TryGetProperty("data", nested) Then
                            data = nested
                        End If
                    End If

                    If data.ValueKind <> JsonValueKind.Array Then
                        Throw New Exception(
                            "The API returned an unexpected asset list format.")
                    End If

                    assetsGrid.Rows.Clear()

                    For Each asset As JsonElement In data.EnumerateArray()
                        Dim id As String = JsonText(asset, "id")
                        Dim code As String = JsonText(asset, "asset_code")
                        Dim assetName As String = JsonText(asset, "name")
                        Dim category As String = JsonText(asset, "category")
                        Dim cost As Decimal =
                            JsonDecimal(asset, "acquisition_cost")
                        Dim active As Boolean =
                            JsonBoolean(asset, "is_active")

                        Dim rowIndex As Integer = assetsGrid.Rows.Add(
                            id,
                            code,
                            assetName,
                            category,
                            cost.ToString("N2", CultureInfo.CurrentCulture),
                            If(active, "Active", "Inactive"))

                        If Not active Then
                            assetsGrid.Rows(rowIndex).DefaultCellStyle.ForeColor =
                                Color.Gray
                        End If
                    Next
                End Using
            End Using

            ApplyAssetSearch()

            statusLabel.Text =
                "Showing " & CountVisibleAssets().ToString() &
                " of " & assetsGrid.Rows.Count.ToString() & " asset(s)."

        Catch ex As Exception
            statusLabel.Text = "Unable to load assets."

            MessageBox.Show(
                "Unable to load fixed assets." & Environment.NewLine &
                ex.Message & Environment.NewLine & Environment.NewLine &
                "Check your connection and the Laravel API.",
                "Aqua ARC",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)
        Finally
            isLoading = False
            refreshButton.Enabled = True
            UpdateSelectionButtons()
        End Try
    End Function

    Private Sub AssetSearchInput_TextChanged(
        sender As Object,
        e As EventArgs)

        ApplyAssetSearch()

        If Not isLoading Then
            statusLabel.Text =
                "Showing " & CountVisibleAssets().ToString() &
                " of " & assetsGrid.Rows.Count.ToString() & " asset(s)."
        End If
    End Sub

    Private Sub CategoryFilterButton_Click(sender As Object, e As EventArgs)
        Using dialog As New Form()
            dialog.Text = "Filter Fixed Assets by Category"
            dialog.StartPosition = FormStartPosition.CenterParent
            dialog.FormBorderStyle = FormBorderStyle.FixedDialog
            dialog.MinimizeBox = False
            dialog.MaximizeBox = False
            dialog.ShowInTaskbar = False
            dialog.ClientSize = New Size(430, 500)
            dialog.BackColor = Color.White
            dialog.Font = New Font("Segoe UI", 9.5F)

            Dim heading As New Label With {
                .Text = "Select one or more categories",
                .Font = New Font("Segoe UI", 13.0F, FontStyle.Bold),
                .ForeColor = deepGreen,
                .Location = New Point(18, 16),
                .Size = New Size(390, 28)
            }
            dialog.Controls.Add(heading)

            Dim hint As New Label With {
                .Text = "Assets matching any selected category will be shown. Leave all unchecked to show every category.",
                .ForeColor = Color.FromArgb(95, 115, 101),
                .Location = New Point(18, 48),
                .Size = New Size(390, 38)
            }
            dialog.Controls.Add(hint)

            Dim categoryList As New CheckedListBox With {
                .Location = New Point(18, 92),
                .Size = New Size(394, 320),
                .CheckOnClick = True,
                .BorderStyle = BorderStyle.FixedSingle,
                .Font = New Font("Segoe UI", 9.5F),
                .IntegralHeight = False
            }

            For Each category As String In assetCategories
                Dim index As Integer = categoryList.Items.Add(category)
                If selectedCategoryFilters.Contains(category) Then
                    categoryList.SetItemChecked(index, True)
                End If
            Next
            dialog.Controls.Add(categoryList)

            Dim selectAllButton As New Button With {
                .Text = "Select All",
                .Location = New Point(18, 425),
                .Size = New Size(95, 34),
                .FlatStyle = FlatStyle.Flat
            }
            AddHandler selectAllButton.Click,
                Sub()
                    For i As Integer = 0 To categoryList.Items.Count - 1
                        categoryList.SetItemChecked(i, True)
                    Next
                End Sub
            dialog.Controls.Add(selectAllButton)

            Dim clearButton As New Button With {
                .Text = "Clear",
                .Location = New Point(120, 425),
                .Size = New Size(80, 34),
                .FlatStyle = FlatStyle.Flat
            }
            AddHandler clearButton.Click,
                Sub()
                    For i As Integer = 0 To categoryList.Items.Count - 1
                        categoryList.SetItemChecked(i, False)
                    Next
                End Sub
            dialog.Controls.Add(clearButton)

            Dim cancelButton As New Button With {
                .Text = "Cancel",
                .Location = New Point(225, 425),
                .Size = New Size(85, 34),
                .DialogResult = DialogResult.Cancel,
                .FlatStyle = FlatStyle.Flat
            }
            dialog.Controls.Add(cancelButton)

            Dim applyButton As New Button With {
                .Text = "Apply",
                .Location = New Point(320, 425),
                .Size = New Size(92, 34),
                .BackColor = primaryGreen,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .DialogResult = DialogResult.OK
            }
            applyButton.FlatAppearance.BorderSize = 0
            dialog.Controls.Add(applyButton)
            dialog.AcceptButton = applyButton
            dialog.CancelButton = cancelButton

            If dialog.ShowDialog(Me) = DialogResult.OK Then
                selectedCategoryFilters.Clear()
                For Each item As Object In categoryList.CheckedItems
                    selectedCategoryFilters.Add(Convert.ToString(item))
                Next

                categoryFilterButton.Text =
                    If(selectedCategoryFilters.Count = 0,
                       "Filter Categories",
                       "Categories (" & selectedCategoryFilters.Count.ToString() & ")")

                ApplyAssetSearch()
                statusLabel.Text =
                    "Showing " & CountVisibleAssets().ToString() &
                    " of " & assetsGrid.Rows.Count.ToString() & " asset(s)."
            End If
        End Using
    End Sub

    Private Sub ApplyAssetSearch()
        Dim query As String = assetSearchInput.Text.Trim()

        assetsGrid.SuspendLayout()

        Try
            For Each row As DataGridViewRow In assetsGrid.Rows
                If row.IsNewRow Then Continue For

                Dim assetCode As String =
                    Convert.ToString(row.Cells("asset_code").Value)

                Dim assetName As String =
                    Convert.ToString(row.Cells("name").Value)

                Dim category As String =
                    Convert.ToString(row.Cells("category").Value)

                Dim matchesSearch As Boolean =
                    String.IsNullOrWhiteSpace(query) OrElse
                    assetCode.IndexOf(
                        query, StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                    assetName.IndexOf(
                        query, StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                    category.IndexOf(
                        query, StringComparison.OrdinalIgnoreCase) >= 0

                Dim matchesCategory As Boolean =
                    selectedCategoryFilters.Count = 0 OrElse
                    selectedCategoryFilters.Contains(category)

                row.Visible = matchesSearch AndAlso matchesCategory
            Next
        Finally
            assetsGrid.ResumeLayout()
        End Try
    End Sub

    Private Function CountVisibleAssets() As Integer
        Dim count As Integer = 0

        For Each row As DataGridViewRow In assetsGrid.Rows
            If Not row.IsNewRow AndAlso row.Visible Then
                count += 1
            End If
        Next

        Return count
    End Function

    Private Sub ClearSearchButton_Click(
        sender As Object,
        e As EventArgs)

        assetSearchInput.Clear()
        selectedCategoryFilters.Clear()
        categoryFilterButton.Text = "Filter Categories"
        ApplyAssetSearch()
        statusLabel.Text =
            "Showing " & CountVisibleAssets().ToString() &
            " of " & assetsGrid.Rows.Count.ToString() & " asset(s)."
    End Sub

    Private Sub CategorySelectButton_Click(sender As Object, e As EventArgs)
        Using dialog As New Form()
            dialog.Text = "Select Fixed-Asset Categories"
            dialog.StartPosition = FormStartPosition.CenterParent
            dialog.FormBorderStyle = FormBorderStyle.FixedDialog
            dialog.MinimizeBox = False
            dialog.MaximizeBox = False
            dialog.ShowInTaskbar = False
            dialog.ClientSize = New Size(440, 510)
            dialog.BackColor = Color.White
            dialog.Font = New Font("Segoe UI", 9.5F)

            Dim heading As New Label With {
                .Text = "Select one or more categories",
                .Font = New Font("Segoe UI", 13.0F, FontStyle.Bold),
                .ForeColor = deepGreen,
                .Location = New Point(18, 16),
                .Size = New Size(400, 28)
            }
            dialog.Controls.Add(heading)

            Dim hint As New Label With {
                .Text = "Check every category that applies to this asset.",
                .ForeColor = Color.FromArgb(95, 115, 101),
                .Location = New Point(18, 47),
                .Size = New Size(400, 25)
            }
            dialog.Controls.Add(hint)

            Dim categoryList As New CheckedListBox With {
                .Location = New Point(18, 78),
                .Size = New Size(404, 335),
                .CheckOnClick = True,
                .BorderStyle = BorderStyle.FixedSingle,
                .Font = New Font("Segoe UI", 9.5F),
                .IntegralHeight = False
            }

            For Each category As String In assetCategories
                Dim index As Integer = categoryList.Items.Add(category)
                If selectedAssetCategories.Contains(category) Then
                    categoryList.SetItemChecked(index, True)
                End If
            Next
            dialog.Controls.Add(categoryList)

            Dim selectAllButton As New Button With {
                .Text = "Select All",
                .Location = New Point(18, 438),
                .Size = New Size(95, 34),
                .FlatStyle = FlatStyle.Flat
            }
            AddHandler selectAllButton.Click,
                Sub()
                    For i As Integer = 0 To categoryList.Items.Count - 1
                        categoryList.SetItemChecked(i, True)
                    Next
                End Sub
            dialog.Controls.Add(selectAllButton)

            Dim cancelButton As New Button With {
                .Text = "Cancel",
                .Location = New Point(230, 438),
                .Size = New Size(85, 34),
                .DialogResult = DialogResult.Cancel,
                .FlatStyle = FlatStyle.Flat
            }
            dialog.Controls.Add(cancelButton)

            Dim applyButton As New Button With {
                .Text = "Apply",
                .Location = New Point(327, 438),
                .Size = New Size(95, 34),
                .BackColor = primaryGreen,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .DialogResult = DialogResult.OK
            }
            applyButton.FlatAppearance.BorderSize = 0
            dialog.Controls.Add(applyButton)
            dialog.AcceptButton = applyButton
            dialog.CancelButton = cancelButton

            If dialog.ShowDialog(Me) = DialogResult.OK Then
                selectedAssetCategories.Clear()
                For Each item As Object In categoryList.CheckedItems
                    selectedAssetCategories.Add(Convert.ToString(item))
                Next
                UpdateCategoryDisplay()
            End If
        End Using
    End Sub

    Private Sub UpdateCategoryDisplay()
        categoryInput.Text = String.Join(", ", selectedAssetCategories)
    End Sub

    Private Async Sub SaveButton_Click(sender As Object, e As EventArgs)
        Await SaveAssetAsync(False)
    End Sub

    Private Async Sub UpdateButton_Click(sender As Object, e As EventArgs)
        Await SaveAssetAsync(True)
    End Sub

    Private Async Function SaveAssetAsync(isUpdate As Boolean) As Task
        If isSaving OrElse isDeleting Then Return

        Dim cost As Decimal
        Dim usefulLife As Decimal
        Dim salvage As Decimal
        Dim accountId As Integer

        If String.IsNullOrWhiteSpace(nameInput.Text) Then
            ShowValidation("Enter an asset name.")
            Return
        End If

        If selectedAssetCategories.Count = 0 Then
            ShowValidation("Select at least one fixed-asset category.")
            Return
        End If

        If Not TryReadDecimal(acquisitionCostInput.Text, cost) OrElse cost < 0D Then
            ShowValidation("Enter a valid acquisition cost of zero or greater.")
            Return
        End If

        If Not TryReadDecimal(usefulLifeInput.Text, usefulLife) OrElse usefulLife <= 0D Then
            ShowValidation("Enter a useful life greater than zero, in years.")
            Return
        End If

        If String.IsNullOrWhiteSpace(salvageValueInput.Text) Then
            salvage = 0D
        ElseIf Not TryReadDecimal(salvageValueInput.Text, salvage) OrElse salvage < 0D Then
            ShowValidation("Enter a valid salvage value of zero or greater.")
            Return
        End If

        If salvage > cost Then
            ShowValidation("Salvage value cannot be greater than acquisition cost.")
            Return
        End If

        Dim accountIdValue As Object = Nothing

        If Not String.IsNullOrWhiteSpace(accountIdInput.Text) Then
            If Not Integer.TryParse(accountIdInput.Text.Trim(), accountId) OrElse accountId <= 0 Then
                ShowValidation("Account ID must be a positive whole number.")
                Return
            End If

            accountIdValue = accountId
        End If

        If isUpdate AndAlso Not selectedAssetId.HasValue Then
            ShowValidation("Double-click an asset in the list before updating it.")
            Return
        End If

        Dim payload As New Dictionary(Of String, Object) From {
            {"name", nameInput.Text.Trim()},
            {"category", String.Join(" | ", selectedAssetCategories)},
            {"acquisition_cost", cost},
            {"useful_life_years", usefulLife},
            {"salvage_value", salvage},
            {"location", NullableString(locationInput.Text)},
            {"account_id", accountIdValue},
            {"description", NullableString(descriptionInput.Text)},
            {"is_active", activeInput.Checked}
        }

        If acquisitionDateInput.Checked Then
            payload("acquisition_date") =
                acquisitionDateInput.Value.ToString("yyyy-MM-dd")
        Else
            payload("acquisition_date") = Nothing
        End If

        isSaving = True
        SetEditorEnabled(False)
        refreshButton.Enabled = False
        deleteButton.Enabled = False

        Try
            statusLabel.Text =
                If(isUpdate, "Updating asset...", "Saving asset...")

            Dim json As String = JsonSerializer.Serialize(payload)

            ApiSession.ApplyAuthorization()

            Using content As New StringContent(
                json,
                Encoding.UTF8,
                "application/json")

                Dim response As HttpResponseMessage

                If isUpdate Then
                    response = Await ApiSession.Client.PutAsync(
                        "fixed-assets/" & selectedAssetId.Value.ToString(),
                        content)
                Else
                    response = Await ApiSession.Client.PostAsync(
                        "fixed-assets",
                        content)
                End If

                Using response
                    Dim responseText As String =
                        Await response.Content.ReadAsStringAsync()

                    If Not response.IsSuccessStatusCode Then
                        Throw New Exception(
                            "Server response (" &
                            CInt(response.StatusCode).ToString() & "): " &
                            responseText)
                    End If
                End Using
            End Using

            MessageBox.Show(
                If(isUpdate,
                   "Fixed asset updated successfully.",
                   "Fixed asset saved successfully."),
                "Aqua ARC",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information)

            ClearForm()

        Catch ex As Exception
            statusLabel.Text = "Save failed."

            MessageBox.Show(
                "Unable to save the fixed asset." & Environment.NewLine &
                ex.Message,
                "Aqua ARC",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)
        Finally
            isSaving = False
            SetEditorEnabled(True)
            refreshButton.Enabled = True
        End Try

        Await LoadAssetsAsync()
    End Function

    Private Async Sub DeleteButton_Click(sender As Object, e As EventArgs)
        Await DeactivateSelectedAssetAsync()
    End Sub

    Private Async Function DeactivateSelectedAssetAsync() As Task
        If isDeleting OrElse isSaving OrElse isLoading Then Return

        If Not selectedAssetId.HasValue Then
            ShowValidation("Select an asset from the list first.")
            Return
        End If

        Dim assetId As Integer = selectedAssetId.Value
        Dim assetCode As String = assetCodeInput.Text.Trim()
        Dim assetName As String = nameInput.Text.Trim()

        If String.IsNullOrWhiteSpace(assetCode) Then
            assetCode = "(unknown code)"
        End If

        If String.IsNullOrWhiteSpace(assetName) Then
            assetName = "(unnamed asset)"
        End If

        Dim confirmationMessage As String =
            "Deactivate this fixed asset?" & Environment.NewLine &
            Environment.NewLine &
            "Asset: " & assetName & Environment.NewLine &
            "Code: " & assetCode & Environment.NewLine &
            Environment.NewLine &
            "The record will be retained for accounting history. " &
            "It will not be permanently erased." & Environment.NewLine &
            Environment.NewLine &
            "Continue?"

        Dim choice As DialogResult = MessageBox.Show(
            confirmationMessage,
            "Confirm Asset Deactivation",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2)

        If choice <> DialogResult.Yes Then
            statusLabel.Text = "Deactivation cancelled."
            Return
        End If

        isDeleting = True
        SetEditorEnabled(False)
        refreshButton.Enabled = False
        deleteButton.Enabled = False
        statusLabel.Text = "Deactivating asset..."

        Try
            ApiSession.ApplyAuthorization()

            Using response As HttpResponseMessage =
                Await ApiSession.Client.DeleteAsync(
                    "fixed-assets/" & assetId.ToString())

                Dim responseText As String =
                    Await response.Content.ReadAsStringAsync()

                If Not response.IsSuccessStatusCode Then
                    Throw New Exception(
                        "Server response (" &
                        CInt(response.StatusCode).ToString() & "): " &
                        responseText)
                End If
            End Using

            ClearForm()

            MessageBox.Show(
                "The fixed asset was deactivated successfully." &
                Environment.NewLine & Environment.NewLine &
                "Its record has been retained for accounting history.",
                "Aqua ARC",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information)

            statusLabel.Text = "Asset deactivated."

        Catch ex As Exception
            statusLabel.Text = "Deactivation failed."

            MessageBox.Show(
                "Unable to deactivate the fixed asset." &
                Environment.NewLine & ex.Message &
                Environment.NewLine & Environment.NewLine &
                "Check that the Laravel API is running and that its " &
                "DELETE endpoint deactivates the asset.",
                "Aqua ARC",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)
        Finally
            isDeleting = False
            SetEditorEnabled(True)
            refreshButton.Enabled = True
        End Try

        Await LoadAssetsAsync()
    End Function

    Private Async Sub AssetsGrid_CellDoubleClick(
        sender As Object,
        e As DataGridViewCellEventArgs)

        If e.RowIndex < 0 OrElse isSaving OrElse isDeleting Then Return

        Dim idText As String =
            Convert.ToString(assetsGrid.Rows(e.RowIndex).Cells("id").Value)

        Dim assetId As Integer

        If Not Integer.TryParse(idText, assetId) Then Return

        Await LoadSelectedAssetAsync(assetId)
    End Sub

    Private Async Function LoadSelectedAssetAsync(assetId As Integer) As Task
        If isSaving OrElse isDeleting Then Return

        Try
            statusLabel.Text = "Loading selected asset..."
            SetEditorEnabled(False)

            ApiSession.ApplyAuthorization()

            Using response As HttpResponseMessage =
                Await ApiSession.Client.GetAsync(
                    "fixed-assets/" & assetId.ToString())

                Dim json As String =
                    Await response.Content.ReadAsStringAsync()

                If Not response.IsSuccessStatusCode Then
                    Throw New Exception("Server response: " & json)
                End If

                Using doc As JsonDocument = JsonDocument.Parse(json)
                    Dim asset As JsonElement = doc.RootElement
                    Dim nested As JsonElement

                    If asset.ValueKind = JsonValueKind.Object AndAlso
                       asset.TryGetProperty("data", nested) Then
                        asset = nested
                    End If

                    If asset.ValueKind <> JsonValueKind.Object Then
                        Throw New Exception(
                            "The API returned an unexpected asset format.")
                    End If

                    selectedAssetId = assetId

                    assetCodeInput.Text = JsonText(asset, "asset_code")
                    nameInput.Text = JsonText(asset, "name")

                    Dim category As String = JsonText(asset, "category")
                    selectedAssetCategories.Clear()

                    For Each savedCategory As String In
                        category.Split(New String() {"|"}, StringSplitOptions.RemoveEmptyEntries)
                        Dim normalizedCategory As String = savedCategory.Trim()
                        If normalizedCategory.Length > 0 AndAlso
                           Not selectedAssetCategories.Contains(normalizedCategory) Then
                            selectedAssetCategories.Add(normalizedCategory)
                        End If
                    Next

                    UpdateCategoryDisplay()

                    Dim acquisitionDate As String =
                        JsonText(asset, "acquisition_date").Trim()

                    Dim parsedDate As DateTime
                    Dim dateParsed As Boolean = False

                    If acquisitionDate.Length >= 10 Then
                        dateParsed = DateTime.TryParseExact(
                            acquisitionDate.Substring(0, 10),
                            "yyyy-MM-dd",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            parsedDate)
                    End If

                    If dateParsed Then
                        acquisitionDateInput.Value = parsedDate.Date
                        acquisitionDateInput.Checked = True
                    Else
                        acquisitionDateInput.Value = DateTime.Today
                        acquisitionDateInput.Checked = False
                    End If

                    acquisitionCostInput.Text =
                        JsonDecimal(asset, "acquisition_cost").ToString(
                            CultureInfo.CurrentCulture)

                    usefulLifeInput.Text =
                        JsonDecimal(asset, "useful_life_years").ToString(
                            CultureInfo.CurrentCulture)

                    salvageValueInput.Text =
                        JsonDecimal(asset, "salvage_value").ToString(
                            CultureInfo.CurrentCulture)

                    locationInput.Text = JsonText(asset, "location")
                    accountIdInput.Text = JsonText(asset, "account_id")
                    descriptionInput.Text = JsonText(asset, "description")
                    activeInput.Checked = JsonBoolean(asset, "is_active")

                    statusLabel.Text = "Editing asset: " & assetCodeInput.Text
                End Using
            End Using

        Catch ex As Exception
            MessageBox.Show(
                "Unable to load the selected asset." &
                Environment.NewLine & ex.Message,
                "Aqua ARC",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)
        Finally
            SetEditorEnabled(True)
            UpdateSelectionButtons()
        End Try
    End Function

    Private Sub AssetsGrid_SelectionChanged(sender As Object, e As EventArgs)
        If isLoading OrElse isSaving OrElse isDeleting Then Return

        ' Require double-clicking an asset to load it for editing.
        If assetsGrid.SelectedRows.Count = 0 Then
            selectedAssetId = Nothing
        Else
            Dim row As DataGridViewRow = assetsGrid.SelectedRows(0)
            Dim rowId As Integer
            Dim rowIdText As String =
                Convert.ToString(row.Cells("id").Value)

            If Not Integer.TryParse(rowIdText, rowId) OrElse
            Not selectedAssetId.HasValue OrElse
            selectedAssetId.Value <> rowId Then
                selectedAssetId = Nothing
            End If
        End If

        UpdateSelectionButtons()
    End Sub

    Private Sub UpdateSelectionButtons()
        Dim hasSelectedGridRow As Boolean =
            assetsGrid.SelectedRows.Count > 0

        If Not hasSelectedGridRow Then
            deleteButton.Enabled = selectedAssetId.HasValue AndAlso
                                   Not isDeleting AndAlso
                                   Not isSaving AndAlso
                                   Not isLoading

            updateButton.Enabled = selectedAssetId.HasValue AndAlso
                                   Not isDeleting AndAlso
                                   Not isSaving AndAlso
                                   Not isLoading
            Return
        End If

        Dim row As DataGridViewRow = assetsGrid.SelectedRows(0)
        Dim rowId As Integer
        Dim rowIdText As String =
            Convert.ToString(row.Cells("id").Value)

        Dim validRow As Boolean = Integer.TryParse(rowIdText, rowId)

        deleteButton.Enabled = validRow AndAlso
                               Not isDeleting AndAlso
                               Not isSaving AndAlso
                               Not isLoading

        updateButton.Enabled = selectedAssetId.HasValue AndAlso
                               Not isDeleting AndAlso
                               Not isSaving AndAlso
                               Not isLoading
    End Sub

    Private Sub ClearButton_Click(sender As Object, e As EventArgs)
        ClearForm()
    End Sub

    Private Async Sub RefreshButton_Click(sender As Object, e As EventArgs)
        Await LoadAssetsAsync()
    End Sub

    Private Sub ClearForm()
        selectedAssetId = Nothing

        assetCodeInput.Clear()
        assetCodeInput.PlaceholderText = "Generated automatically when saved"
        nameInput.Clear()
        selectedAssetCategories.Clear()
        UpdateCategoryDisplay()

        acquisitionDateInput.Value = DateTime.Today
        acquisitionDateInput.Checked = False

        acquisitionCostInput.Clear()
        usefulLifeInput.Clear()
        salvageValueInput.Text = "0"
        locationInput.Clear()
        accountIdInput.Clear()
        descriptionInput.Clear()
        activeInput.Checked = True

        assetsGrid.ClearSelection()

        updateButton.Enabled = False
        deleteButton.Enabled = False
        statusLabel.Text = "Ready for a new asset."
    End Sub

    Private Sub SetEditorEnabled(enabled As Boolean)
        assetCodeInput.Enabled = enabled
        nameInput.Enabled = enabled
        categoryInput.Enabled = enabled
        categorySelectButton.Enabled = enabled
        acquisitionDateInput.Enabled = enabled
        acquisitionCostInput.Enabled = enabled
        usefulLifeInput.Enabled = enabled
        salvageValueInput.Enabled = enabled
        locationInput.Enabled = enabled
        accountIdInput.Enabled = enabled
        descriptionInput.Enabled = enabled
        activeInput.Enabled = enabled

        saveButton.Enabled = enabled
        clearButton.Enabled = enabled
        updateButton.Enabled = enabled AndAlso selectedAssetId.HasValue
        deleteButton.Enabled = enabled AndAlso selectedAssetId.HasValue
    End Sub

    Private Sub ShowValidation(message As String)
        MessageBox.Show(
            message,
            "Check Asset Information",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning)
    End Sub

    Private Function NullableString(value As String) As Object
        If String.IsNullOrWhiteSpace(value) Then
            Return Nothing
        End If

        Return value.Trim()
    End Function

    Private Function TryReadDecimal(
        value As String,
        ByRef result As Decimal) As Boolean

        If Decimal.TryParse(
            value,
            NumberStyles.Number,
            CultureInfo.CurrentCulture,
            result) Then
            Return True
        End If

        Return Decimal.TryParse(
            value,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            result)
    End Function

    Private Function JsonText(
        element As JsonElement,
        propertyName As String) As String

        Dim value As JsonElement

        If element.ValueKind <> JsonValueKind.Object OrElse
           Not element.TryGetProperty(propertyName, value) Then
            Return String.Empty
        End If

        If value.ValueKind = JsonValueKind.Null OrElse
           value.ValueKind = JsonValueKind.Undefined Then
            Return String.Empty
        End If

        If value.ValueKind = JsonValueKind.String Then
            Return If(value.GetString(), String.Empty)
        End If

        Return value.ToString()
    End Function

    Private Function JsonDecimal(
        element As JsonElement,
        propertyName As String) As Decimal

        Dim raw As String = JsonText(element, propertyName)
        Dim result As Decimal

        If Decimal.TryParse(
            raw,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            result) Then
            Return result
        End If

        If Decimal.TryParse(
            raw,
            NumberStyles.Any,
            CultureInfo.CurrentCulture,
            result) Then
            Return result
        End If

        Return 0D
    End Function

    Private Function JsonBoolean(
        element As JsonElement,
        propertyName As String) As Boolean

        Dim value As JsonElement

        If element.ValueKind <> JsonValueKind.Object OrElse
        Not element.TryGetProperty(propertyName, value) Then
            Return False
        End If

        If value.ValueKind = JsonValueKind.True Then Return True
        If value.ValueKind = JsonValueKind.False Then Return False

        Dim raw As String = value.ToString()
        Dim result As Boolean

        If Boolean.TryParse(raw, result) Then Return result

        Return raw = "1"
    End Function

End Class