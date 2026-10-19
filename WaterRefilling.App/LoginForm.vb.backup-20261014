Imports System.Drawing
Imports System.Net.Http
Imports System.Text
Imports System.Text.Json
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports System.Collections.Generic

Public Class LoginForm
    Inherits Form

    Private ReadOnly ApiUrl As String =
        New Uri(ApiSession.Client.BaseAddress, "login").ToString()

    Private ReadOnly emailInput As New TextBox()
    Private ReadOnly passwordInput As New TextBox()
    Private ReadOnly loginButton As New Button()
    Private ReadOnly statusLabel As New Label()

    Public Sub New()
        InitializeComponent()
        BuildLoginUI()
    End Sub

    Private Sub BuildLoginUI()
        Text = "Aqua ARC - Login"
        StartPosition = FormStartPosition.CenterScreen
        Size = New Size(900, 580)
        MinimumSize = New Size(800, 520)
        BackColor = Color.FromArgb(245, 250, 245)
        Font = New Font("Segoe UI", 10)

        Dim branding As New Panel With {
            .Dock = DockStyle.Left,
            .Width = 390,
            .BackColor = Color.FromArgb(23, 107, 58),
            .Padding = New Padding(35)
        }
        Controls.Add(branding)

        Dim brandTitle As New Label With {
            .Text = "Aqua ARC",
            .Font = New Font("Segoe UI", 30, FontStyle.Bold),
            .ForeColor = Color.White,
            .Location = New Point(35, 110),
            .AutoSize = True
        }
        branding.Controls.Add(brandTitle)

        Dim brandSubtitle As New Label With {
            .Text = "Bookkeeping" & vbCrLf & "Made Simple.",
            .Font = New Font("Segoe UI", 21, FontStyle.Bold),
            .ForeColor = Color.FromArgb(214, 244, 221),
            .Location = New Point(38, 170),
            .Size = New Size(300, 100)
        }
        branding.Controls.Add(brandSubtitle)

        Dim brandDescription As New Label With {
            .Text = "A centralized system for managing financial records, accounting transactions, and reports.",
            .Font = New Font("Segoe UI", 10),
            .ForeColor = Color.FromArgb(230, 245, 233),
            .Location = New Point(38, 295),
            .Size = New Size(300, 75)
        }
        branding.Controls.Add(brandDescription)

        Dim formTitle As New Label With {
            .Text = "Welcome back",
            .Font = New Font("Segoe UI", 23, FontStyle.Bold),
            .ForeColor = Color.FromArgb(23, 107, 58),
            .Location = New Point(440, 85),
            .AutoSize = True
        }
        Controls.Add(formTitle)

        Dim formDescription As New Label With {
            .Text = "Sign in to continue to your account.",
            .ForeColor = Color.Gray,
            .Location = New Point(443, 130),
            .AutoSize = True
        }
        Controls.Add(formDescription)

        Dim emailLabel As New Label With {
            .Text = "Email address",
            .Location = New Point(445, 190),
            .AutoSize = True
        }
        Controls.Add(emailLabel)

        emailInput.Location = New Point(445, 218)
        emailInput.Size = New Size(340, 35)
        emailInput.Font = New Font("Segoe UI", 11)
        emailInput.BorderStyle = BorderStyle.FixedSingle
        emailInput.TabIndex = 0
        Controls.Add(emailInput)

        Dim passwordLabel As New Label With {
            .Text = "Password",
            .Location = New Point(445, 275),
            .AutoSize = True
        }
        Controls.Add(passwordLabel)

        passwordInput.Location = New Point(445, 303)
        passwordInput.Size = New Size(340, 35)
        passwordInput.Font = New Font("Segoe UI", 11)
        passwordInput.BorderStyle = BorderStyle.FixedSingle
        passwordInput.UseSystemPasswordChar = True
        passwordInput.TabIndex = 1
        Controls.Add(passwordInput)

        loginButton.Text = "Sign In"
        loginButton.Location = New Point(445, 365)
        loginButton.Size = New Size(340, 45)
        loginButton.BackColor = Color.FromArgb(46, 157, 88)
        loginButton.ForeColor = Color.White
        loginButton.FlatStyle = FlatStyle.Flat
        loginButton.FlatAppearance.BorderSize = 0
        loginButton.Cursor = Cursors.Hand
        loginButton.TabIndex = 2
        Controls.Add(loginButton)

        statusLabel.Location = New Point(445, 425)
        statusLabel.Size = New Size(340, 45)
        statusLabel.ForeColor = Color.Firebrick
        statusLabel.TextAlign = ContentAlignment.MiddleLeft
        Controls.Add(statusLabel)

        AcceptButton = loginButton

        AddHandler loginButton.Click, AddressOf LoginButton_Click
    End Sub

    Private Async Sub LoginButton_Click(
        sender As Object,
        e As EventArgs)

        If String.IsNullOrWhiteSpace(emailInput.Text) OrElse
           String.IsNullOrWhiteSpace(passwordInput.Text) Then

            statusLabel.ForeColor = Color.Firebrick
            statusLabel.Text = "Enter your email and password."
            Return
        End If

        loginButton.Enabled = False
        statusLabel.ForeColor = Color.Gray
        statusLabel.Text = "Signing in..."

        Try
            Dim payload = New With {
                .email = emailInput.Text.Trim(),
                .password = passwordInput.Text
            }

            Dim json As String = JsonSerializer.Serialize(payload)
            Dim receivedToken As String = String.Empty
            Dim receivedTokenType As String = String.Empty

            Dim parsedUserId As Integer = 0
            Dim parsedUserName As String = String.Empty
            Dim parsedUserEmail As String = String.Empty
            Dim parsedRoles As New List(Of String)()
            Dim parsedPermissions As New List(Of String)()
            Dim receivedPermissions As Boolean = False

            Using client As New HttpClient()
                client.Timeout = TimeSpan.FromSeconds(15)

                Using content As New StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json")

                    Using response As HttpResponseMessage =
                        Await client.PostAsync(ApiUrl, content)

                        Dim responseBody As String =
                            Await response.Content.ReadAsStringAsync()

                        If Not response.IsSuccessStatusCode Then
                            statusLabel.ForeColor = Color.Firebrick

                            If CInt(response.StatusCode) = 401 Then
                                statusLabel.Text =
                                    "Invalid email or password."
                            ElseIf CInt(response.StatusCode) = 422 Then
                                statusLabel.Text =
                                    "Please check your email and password format."
                            Else
                                statusLabel.Text =
                                    "Login request failed. HTTP " &
                                    CInt(response.StatusCode).ToString()
                            End If

                            Return
                        End If

                        Using document As JsonDocument =
                            JsonDocument.Parse(responseBody)

                            Dim root As JsonElement =
                                document.RootElement

                            Dim successElement As JsonElement

                            If Not root.TryGetProperty(
                                "success", successElement) OrElse
                                successElement.ValueKind <>
                                JsonValueKind.True Then

                                statusLabel.ForeColor = Color.Firebrick
                                statusLabel.Text =
                                    "Login was not confirmed by the server."
                                Return
                            End If

                            Dim tokenElement As JsonElement

                            If Not root.TryGetProperty(
                                "token", tokenElement) OrElse
                                tokenElement.ValueKind <>
                                JsonValueKind.String Then

                                statusLabel.ForeColor = Color.Firebrick
                                statusLabel.Text =
                                    "Login succeeded, but no API token was returned."
                                Return
                            End If

                            receivedToken = tokenElement.GetString()

                            If String.IsNullOrWhiteSpace(receivedToken) Then
                                statusLabel.ForeColor = Color.Firebrick
                                statusLabel.Text =
                                    "The server returned an empty API token."
                                Return
                            End If

                            Dim tokenTypeElement As JsonElement

                            If root.TryGetProperty(
                                "token_type", tokenTypeElement) AndAlso
                                tokenTypeElement.ValueKind =
                                JsonValueKind.String Then

                                receivedTokenType =
                                    tokenTypeElement.GetString()
                            End If

                            If Not String.Equals(
                                receivedTokenType,
                                "Bearer",
                                StringComparison.OrdinalIgnoreCase) Then

                                statusLabel.ForeColor = Color.Firebrick
                                statusLabel.Text =
                                    "The server returned an unsupported token type."
                                Return
                            End If

                            ' Read the authenticated user information.
                            Dim userElement As JsonElement

                            If Not root.TryGetProperty(
                                "user", userElement) OrElse
                                userElement.ValueKind <>
                                JsonValueKind.Object Then

                                statusLabel.ForeColor = Color.Firebrick
                                statusLabel.Text =
                                    "Login response is missing user information."
                                Return
                            End If

                            Dim idElement As JsonElement
                            Dim nameElement As JsonElement
                            Dim emailElement As JsonElement
                            Dim rolesElement As JsonElement

                            If userElement.TryGetProperty(
                                "id", idElement) Then

                                If Not Integer.TryParse(
                                    idElement.ToString(),
                                    parsedUserId) OrElse
                                    parsedUserId <= 0 Then

                                    statusLabel.ForeColor = Color.Firebrick
                                    statusLabel.Text =
                                        "The server returned an invalid user ID."
                                    Return
                                End If
                            Else
                                statusLabel.ForeColor = Color.Firebrick
                                statusLabel.Text =
                                    "The server did not return a user ID."
                                Return
                            End If

                            If userElement.TryGetProperty(
                                "name", nameElement) AndAlso
                                nameElement.ValueKind =
                                JsonValueKind.String Then

                                parsedUserName = nameElement.GetString()
                            End If

                            If userElement.TryGetProperty(
                                "email", emailElement) AndAlso
                                emailElement.ValueKind =
                                JsonValueKind.String Then

                                parsedUserEmail = emailElement.GetString()
                            End If

                            If userElement.TryGetProperty(
                                "roles", rolesElement) AndAlso
                                rolesElement.ValueKind =
                                JsonValueKind.Array Then

                                For Each roleElement As JsonElement In
                                    rolesElement.EnumerateArray()

                                    If roleElement.ValueKind =
                                        JsonValueKind.String Then

                                        Dim roleName As String =
                                            roleElement.GetString()

                                        If Not String.IsNullOrWhiteSpace(
                                            roleName) Then

                                            parsedRoles.Add(roleName.Trim())
                                        End If
                                    End If
                                Next
                            End If

                            Dim permissionsElement As JsonElement
                            If userElement.TryGetProperty("permissions", permissionsElement) AndAlso
                                permissionsElement.ValueKind = JsonValueKind.Array Then
                                receivedPermissions = True
                                For Each permissionElement As JsonElement In permissionsElement.EnumerateArray()
                                    If permissionElement.ValueKind = JsonValueKind.String Then
                                        parsedPermissions.Add(permissionElement.GetString())
                                    End If
                                Next
                            End If

                            If String.IsNullOrWhiteSpace(parsedUserName) OrElse
                               String.IsNullOrWhiteSpace(parsedUserEmail) Then

                                statusLabel.ForeColor = Color.Firebrick
                                statusLabel.Text =
                                    "The server returned incomplete user information."
                                Return
                            End If
                        End Using
                    End Using
                End Using
            End Using

            ' Store the authenticated session for the other forms.
            ApiSession.BearerToken = receivedToken
            ApiSession.CurrentUserId = parsedUserId
            ApiSession.CurrentUserName = parsedUserName
            ApiSession.CurrentUserEmail = parsedUserEmail
            ApiSession.CurrentUserRoles = parsedRoles
            ApiSession.CurrentUserPermissions = parsedPermissions
            ApiSession.HasServerPermissions = receivedPermissions

            ApiSession.ApplyAuthorization()

            statusLabel.ForeColor = Color.FromArgb(23, 107, 58)
            statusLabel.Text = "Login successful."

        Hide()

Using main As New MainForm()
    main.ShowDialog()
End Using

If String.IsNullOrWhiteSpace(ApiSession.BearerToken) Then
    ApiSession.Clear()

    passwordInput.Clear()
    statusLabel.ForeColor = Color.FromArgb(23, 107, 58)
    statusLabel.Text = "You have been logged out."

    Show()
    Activate()
Else
    Close()
End If

        Catch ex As TaskCanceledException
            statusLabel.ForeColor = Color.Firebrick
            statusLabel.Text = "The server request timed out."

        Catch ex As HttpRequestException
            statusLabel.ForeColor = Color.Firebrick
            statusLabel.Text =
                "Cannot connect. Make sure the Laravel server is running."

        Catch ex As JsonException
            statusLabel.ForeColor = Color.Firebrick
            statusLabel.Text = "The server returned an invalid response."

        Catch ex As Exception
            statusLabel.ForeColor = Color.Firebrick
            statusLabel.Text = "An unexpected login error occurred."

        Finally
            loginButton.Enabled = True
        End Try

    End Sub

End Class
