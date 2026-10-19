Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Net
Imports System.Net.Http
Imports System.Text.Json
Imports System.Text.Json.Serialization
Imports System.Threading.Tasks
Imports System.Windows.Forms

Public Class PendingAccountApprovalsForm
    Inherits Form

    Private ReadOnly pendingGrid As New DataGridView()
    Private ReadOnly refreshButton As New Button()
    Private ReadOnly approveButton As New Button()
    Private ReadOnly statusLabel As New Label()

    Private ReadOnly jsonOptions As New JsonSerializerOptions With {
        .PropertyNameCaseInsensitive = True
    }

    Public Sub New()
        Text = "Pending Account Approvals"
        StartPosition = FormStartPosition.CenterParent
        Size = New Size(900, 560)
        MinimumSize = New Size(720, 420)
        BackColor = Color.White
        Font = New Font("Segoe UI", 9.0F)
        BuildLayout()
        AddHandler Shown, Async Sub(sender, e) Await LoadPendingUsersAsync()
    End Sub

    Private Sub BuildLayout()
        Dim header As New Label With {
            .Text = "Pending Account Approvals",
            .Font = New Font("Segoe UI", 16.0F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(28, 88, 52),
            .AutoSize = True,
            .Location = New Point(20, 18)
        }

        Dim description As New Label With {
            .Text = "Review new registrations and approve eligible users as Staff.",
            .ForeColor = Color.DimGray,
            .AutoSize = True,
            .Location = New Point(22, 53)
        }

        refreshButton.Text = "Refresh"
        refreshButton.Size = New Size(100, 34)
        refreshButton.Location = New Point(20, 88)
        refreshButton.FlatStyle = FlatStyle.Flat
        refreshButton.BackColor = Color.FromArgb(238, 248, 240)
        refreshButton.ForeColor = Color.FromArgb(28, 88, 52)
        AddHandler refreshButton.Click, Async Sub(sender, e) Await LoadPendingUsersAsync()

        approveButton.Text = "Approve Selected"
        approveButton.Size = New Size(145, 34)
        approveButton.Location = New Point(130, 88)
        approveButton.FlatStyle = FlatStyle.Flat
        approveButton.BackColor = Color.FromArgb(28, 88, 52)
        approveButton.ForeColor = Color.White
        approveButton.Enabled = False
        AddHandler approveButton.Click, Async Sub(sender, e) Await ApproveSelectedAsync()

        statusLabel.Text = "Loading pending accounts..."
        statusLabel.AutoSize = False
        statusLabel.Location = New Point(290, 88)
        statusLabel.Size = New Size(560, 34)
        statusLabel.TextAlign = ContentAlignment.MiddleLeft
        statusLabel.ForeColor = Color.DimGray

        pendingGrid.Location = New Point(20, 136)
        pendingGrid.Size = New Size(840, 350)
        pendingGrid.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        pendingGrid.ReadOnly = True
        pendingGrid.AllowUserToAddRows = False
        pendingGrid.AllowUserToDeleteRows = False
        pendingGrid.MultiSelect = False
        pendingGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        pendingGrid.AutoGenerateColumns = False
        pendingGrid.RowHeadersVisible = False
        pendingGrid.BackgroundColor = Color.White
        pendingGrid.BorderStyle = BorderStyle.FixedSingle
        pendingGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill

        pendingGrid.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "UserId",
            .HeaderText = "ID",
            .DataPropertyName = "Id",
            .FillWeight = 10
        })
        pendingGrid.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "UserName",
            .HeaderText = "Name",
            .DataPropertyName = "Name",
            .FillWeight = 25
        })
        pendingGrid.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "UserEmail",
            .HeaderText = "Email",
            .DataPropertyName = "Email",
            .FillWeight = 35
        })
        pendingGrid.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "RegisteredAt",
            .HeaderText = "Registered",
            .DataPropertyName = "CreatedAt",
            .FillWeight = 25
        })

        AddHandler pendingGrid.SelectionChanged,
            Sub(sender, e)
                approveButton.Enabled =
                    pendingGrid.SelectedRows.Count > 0 AndAlso
                    Not refreshButton.Enabled = False
            End Sub

        Controls.Add(header)
        Controls.Add(description)
        Controls.Add(refreshButton)
        Controls.Add(approveButton)
        Controls.Add(statusLabel)
        Controls.Add(pendingGrid)

        AddHandler Resize,
            Sub(sender, e)
                pendingGrid.Width = Math.Max(400, ClientSize.Width - 40)
                pendingGrid.Height = Math.Max(180, ClientSize.Height - 156)
                statusLabel.Width = Math.Max(200, ClientSize.Width - 330)
            End Sub
    End Sub

    Private Async Function LoadPendingUsersAsync() As Task
        If Not IsOwnerAllowed() Then
            statusLabel.Text = "Access denied. Owner role and users.manage permission are required."
            pendingGrid.DataSource = Nothing
            approveButton.Enabled = False
            Return
        End If

        SetBusy(True, "Loading pending accounts...")
        Try
            Using response As HttpResponseMessage =
                Await ApiSession.Client.GetAsync("pending-users")
                Dim body As String = Await response.Content.ReadAsStringAsync()

                If response.StatusCode = HttpStatusCode.Unauthorized Then
                    statusLabel.Text = "Your session expired. Please sign in again."
                    Return
                End If

                If response.StatusCode = HttpStatusCode.Forbidden Then
                    statusLabel.Text = "Access denied by the server. Check Owner permissions."
                    Return
                End If

                If Not response.IsSuccessStatusCode Then
                    statusLabel.Text = "Could not load pending accounts (HTTP " &
                        CInt(response.StatusCode).ToString() & ")."
                    Return
                End If

                Dim result As PendingUsersResponse =
                    JsonSerializer.Deserialize(Of PendingUsersResponse)(body, jsonOptions)

                Dim users As New List(Of PendingUserRow)()
                If result IsNot Nothing AndAlso result.Users IsNot Nothing Then
                    For Each user As PendingUserDto In result.Users
                        users.Add(New PendingUserRow With {
                            .Id = user.Id,
                            .Name = If(user.Name, ""),
                            .Email = If(user.Email, ""),
                            .CreatedAt = FormatDate(user.CreatedAt)
                        })
                    Next
                End If

                pendingGrid.DataSource = Nothing
                pendingGrid.DataSource = users
                If users.Count = 0 Then
                    statusLabel.Text = "No pending registrations."
                Else
                    statusLabel.Text = users.Count.ToString() & " pending account(s)."
                End If
            End Using
        Catch ex As TaskCanceledException
            statusLabel.Text = "The request timed out. Check that the API is running."
        Catch ex As HttpRequestException
            statusLabel.Text = "Could not connect to the API. Check the API URL and server."
        Catch ex As JsonException
            statusLabel.Text = "The API returned an unexpected response format."
        Catch ex As Exception
            statusLabel.Text = "Unable to load pending accounts: " & ex.Message
        Finally
            SetBusy(False, statusLabel.Text)
        End Try
    End Function

    Private Async Function ApproveSelectedAsync() As Task
        If Not IsOwnerAllowed() Then
            MessageBox.Show(
                "Access denied. The Owner role and users.manage permission are required.",
                "Account Approvals",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning)
            Return
        End If

        If pendingGrid.SelectedRows.Count = 0 Then Return

        Dim selected As PendingUserRow =
            TryCast(pendingGrid.SelectedRows(0).DataBoundItem, PendingUserRow)
        If selected Is Nothing Then Return

        Dim answer As DialogResult = MessageBox.Show(
            "Approve " & selected.Name & " (" & selected.Email &
            ") and assign the Staff role?",
            "Confirm Account Approval",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question)

        If answer <> DialogResult.Yes Then Return

        SetBusy(True, "Approving account...")
        Try
            Using response As HttpResponseMessage =
                Await ApiSession.Client.PostAsync(
                    "pending-users/" & selected.Id.ToString() & "/approve",
                    Nothing)
                Dim body As String = Await response.Content.ReadAsStringAsync()

                If response.StatusCode = HttpStatusCode.Unauthorized Then
                    statusLabel.Text = "Your session expired. Please sign in again."
                    Return
                End If

                If response.StatusCode = HttpStatusCode.Forbidden Then
                    statusLabel.Text = "The server denied approval. Check Owner permissions."
                    Return
                End If

                If response.StatusCode = HttpStatusCode.Conflict Then
                    MessageBox.Show(
                        "This account was already approved. The list will refresh.",
                        "Account Already Approved",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information)
                    Await LoadPendingUsersAsync()
                    Return
                End If

                If Not response.IsSuccessStatusCode Then
                    Dim message As String = TryReadMessage(body)
                    statusLabel.Text = "Approval failed (HTTP " &
                        CInt(response.StatusCode).ToString() & ")."
                    MessageBox.Show(
                        If(String.IsNullOrWhiteSpace(message), statusLabel.Text, message),
                        "Approval Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error)
                    Return
                End If

                MessageBox.Show(
                    "Account approved and assigned the Staff role.",
                    "Approval Successful",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information)

                Await LoadPendingUsersAsync()
            End Using
        Catch ex As TaskCanceledException
            statusLabel.Text = "The approval request timed out. Refresh before retrying."
        Catch ex As HttpRequestException
            statusLabel.Text = "Could not connect to the API. Check the API server."
        Catch ex As Exception
            statusLabel.Text = "Unable to approve account: " & ex.Message
        Finally
            SetBusy(False, statusLabel.Text)
        End Try
    End Function

    Private Function IsOwnerAllowed() As Boolean
        Dim hasOwner As Boolean =
            ApiSession.CurrentUserRoles IsNot Nothing AndAlso
            ApiSession.CurrentUserRoles.Exists(
                Function(role) String.Equals(
                    If(role, "").Trim(), "Owner",
                    StringComparison.OrdinalIgnoreCase))

        Dim hasPermission As Boolean =
            ApiSession.HasServerPermissions AndAlso
            ApiSession.CurrentUserPermissions IsNot Nothing AndAlso
            ApiSession.CurrentUserPermissions.Contains("users.manage")

        Return hasOwner AndAlso hasPermission
    End Function

    Private Sub SetBusy(isBusy As Boolean, message As String)
        refreshButton.Enabled = Not isBusy
        approveButton.Enabled =
            Not isBusy AndAlso pendingGrid.SelectedRows.Count > 0
        statusLabel.Text = message
        UseWaitCursor = isBusy
    End Sub

    Private Function FormatDate(value As String) As String
        If String.IsNullOrWhiteSpace(value) Then Return ""
        Dim parsed As DateTimeOffset
        If DateTimeOffset.TryParse(value, parsed) Then
            Return parsed.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
        End If
        Return value
    End Function

    Private Function TryReadMessage(body As String) As String
        If String.IsNullOrWhiteSpace(body) Then Return ""
        Try
            Using document As JsonDocument = JsonDocument.Parse(body)
                Dim propertyValue As JsonElement
                If document.RootElement.ValueKind = JsonValueKind.Object AndAlso
                    document.RootElement.TryGetProperty("message", propertyValue) AndAlso
                    propertyValue.ValueKind = JsonValueKind.String Then
                    Return propertyValue.GetString()
                End If
            End Using
        Catch
            ' Keep the UI message generic when the body is not valid JSON.
        End Try
        Return ""
    End Function

    Private NotInheritable Class PendingUsersResponse
        <JsonPropertyName("users")>
        Public Property Users As List(Of PendingUserDto)
    End Class

    Private NotInheritable Class PendingUserDto
        <JsonPropertyName("id")>
        Public Property Id As Integer

        <JsonPropertyName("name")>
        Public Property Name As String

        <JsonPropertyName("email")>
        Public Property Email As String

        <JsonPropertyName("created_at")>
        Public Property CreatedAt As String
    End Class

    Private NotInheritable Class PendingUserRow
        Public Property Id As Integer
        Public Property Name As String
        Public Property Email As String
        Public Property CreatedAt As String
    End Class
End Class
