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

    Private ReadOnly RegisterApiUrl As String =
        New Uri(ApiSession.Client.BaseAddress, "register").ToString()

    Private ReadOnly nameInput As New TextBox()
    Private ReadOnly emailInput As New TextBox()
    Private ReadOnly passwordInput As New TextBox()
    Private ReadOnly confirmPasswordInput As New TextBox()

    Private ReadOnly nameLabel As New Label()
    Private ReadOnly emailLabel As New Label()
    Private ReadOnly passwordLabel As New Label()
    Private ReadOnly confirmPasswordLabel As New Label()

    Private ReadOnly loginButton As New Button()
    Private ReadOnly modeToggleButton As New Button()
    Private ReadOnly statusLabel As New Label()
    Private ReadOnly formTitle As New Label()
    Private ReadOnly formDescription As New Label()

    Private registrationMode As Boolean = False

    Public Sub New()
        InitializeComponent()
        BuildLoginUI()
        ApplyMode()
    End Sub

    Private Sub BuildLoginUI()
        Text = "Aqua ARC - Login"
        StartPosition = FormStartPosition.CenterScreen
        Size = New Size(900, 700)
        MinimumSize = New Size(850, 650)
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

        formTitle.Text = "Welcome back"
        formTitle.Font = New Font("Segoe UI", 23, FontStyle.Bold)
        formTitle.ForeColor = Color.FromArgb(23, 107, 58)
        formTitle.Location = New Point(440, 65)
        formTitle.AutoSize = True
        Controls.Add(formTitle)

        formDescription.Text = "Sign in to continue to your account."
        formDescription.ForeColor = Color.Gray
        formDescription.Location = New Point(443, 110)
        formDescription.AutoSize = True
        Controls.Add(formDescription)

        ConfigureLabel(nameLabel, "Full name")
        ConfigureLabel(emailLabel, "Email address")
        ConfigureLabel(passwordLabel, "Password")
        ConfigureLabel(confirmPasswordLabel, "Confirm password")

        ConfigureTextBox(nameInput, False)
        ConfigureTextBox(emailInput, False)
        ConfigureTextBox(passwordInput, True)
        ConfigureTextBox(confirmPasswordInput, True)

        Controls.Add(nameLabel)
        Controls.Add(nameInput)
        Controls.Add(emailLabel)
        Controls.Add(emailInput)
        Controls.Add(passwordLabel)
        Controls.Add(passwordInput)
        Controls.Add(confirmPasswordLabel)
        Controls.Add(confirmPasswordInput)

        loginButton.Size = New Size(340, 45)
        loginButton.BackColor = Color.FromArgb(46, 157, 88)
        loginButton.ForeColor = Color.White
        loginButton.FlatStyle = FlatStyle.Flat
        loginButton.FlatAppearance.BorderSize = 0
        loginButton.Cursor = Cursors.Hand
        loginButton.TabIndex = 4
        Controls.Add(loginButton)

        statusLabel.Size = New Size(340, 48)
        statusLabel.ForeColor = Color.Firebrick
        statusLabel.TextAlign = ContentAlignment.MiddleLeft
        statusLabel.AutoEllipsis = True
        Controls.Add(statusLabel)

        modeToggleButton.Size = New Size(340, 30)
        modeToggleButton.FlatStyle = FlatStyle.Flat
        modeToggleButton.FlatAppearance.BorderSize = 0
        modeToggleButton.BackColor = BackColor
        modeToggleButton.ForeColor = Color.FromArgb(23, 107, 58)
        modeToggleButton.Cursor = Cursors.Hand
        modeToggleButton.TextAlign = ContentAlignment.MiddleCenter
        modeToggleButton.TabIndex = 5
        Controls.Add(modeToggleButton)

        AddHandler loginButton.Click, AddressOf SubmitButton_Click
        AddHandler modeToggleButton.Click, AddressOf ModeToggleButton_Click
    End Sub

    Private Sub ConfigureLabel(label As Label, caption As String)
        label.Text = caption
        label.AutoSize = True
        label.ForeColor = Color.FromArgb(45, 55, 45)
    End Sub

    Private Sub ConfigureTextBox(textBox As TextBox, isPassword As Boolean)
        textBox.Size = New Size(340, 35)
        textBox.Font = New Font("Segoe UI", 11)
        textBox.BorderStyle = BorderStyle.FixedSingle
        textBox.UseSystemPasswordChar = isPassword
    End Sub

    Private Sub ApplyMode()
        If registrationMode Then
            Text = "Aqua ARC - Create Account"
            formTitle.Text = "Create account"
            formDescription.Text = "Register to request access to Aqua ARC."
            formTitle.Location = New Point(440, 35)
            formDescription.Location = New Point(443, 80)

            nameLabel.Location = New Point(445, 125)
            nameInput.Location = New Point(445, 148)
            emailLabel.Location = New Point(445, 195)
            emailInput.Location = New Point(445, 218)
            passwordLabel.Location = New Point(445, 265)
            passwordInput.Location = New Point(445, 288)
            confirmPasswordLabel.Location = New Point(445, 335)
            confirmPasswordInput.Location = New Point(445, 358)

            nameLabel.Visible = True
            nameInput.Visible = True
            confirmPasswordLabel.Visible = True
            confirmPasswordInput.Visible = True

            loginButton.Text = "Create Account"
            loginButton.Location = New Point(445, 415)
            statusLabel.Location = New Point(445, 465)
            modeToggleButton.Location = New Point(445, 520)
            modeToggleButton.Text = "Already have an account? Log In"
            passwordInput.TabIndex = 2
            confirmPasswordInput.TabIndex = 3
            loginButton.TabIndex = 4
            AcceptButton = loginButton
        Else
            Text = "Aqua ARC - Login"
            formTitle.Text = "Welcome back"
            formDescription.Text = "Sign in to continue to your account."
            formTitle.Location = New Point(440, 85)
            formDescription.Location = New Point(443, 130)

            emailLabel.Location = New Point(445, 190)
            emailInput.Location = New Point(445, 218)
            passwordLabel.Location = New Point(445, 275)
            passwordInput.Location = New Point(445, 303)

            nameLabel.Visible = False
            nameInput.Visible = False
            confirmPasswordLabel.Visible = False
            confirmPasswordInput.Visible = False

            loginButton.Text = "Sign In"
            loginButton.Location = New Point(445, 365)
            statusLabel.Location = New Point(445, 425)
            modeToggleButton.Location = New Point(445, 475)
            modeToggleButton.Text = "Don't have an account? Create Account"
            emailInput.TabIndex = 0
            passwordInput.TabIndex = 1
            loginButton.TabIndex = 4
            AcceptButton = loginButton
        End If

        statusLabel.Text = String.Empty
        statusLabel.ForeColor = Color.Firebrick
    End Sub

    Private Sub ModeToggleButton_Click(sender As Object, e As EventArgs)
        registrationMode = Not registrationMode
        ApplyMode()
    End Sub

    Private Async Sub SubmitButton_Click(sender As Object, e As EventArgs)
        If registrationMode Then
            Await RegisterAccountAsync()
        Else
            Await LoginAsync()
        End If
    End Sub

    Private Async Function RegisterAccountAsync() As Task
        If String.IsNullOrWhiteSpace(nameInput.Text) OrElse
           String.IsNullOrWhiteSpace(emailInput.Text) OrElse
           String.IsNullOrWhiteSpace(passwordInput.Text) OrElse
           String.IsNullOrWhiteSpace(confirmPasswordInput.Text) Then

            ShowError("Complete all fields before registering.")
            Return
        End If

        If Not String.Equals(passwordInput.Text, confirmPasswordInput.Text,
                             StringComparison.Ordinal) Then
            ShowError("The passwords do not match.")
            Return
        End If

        loginButton.Enabled = False
        modeToggleButton.Enabled = False
        statusLabel.ForeColor = Color.Gray
        statusLabel.Text = "Creating your account..."

        Try
            Dim payload = New With {
                .name = nameInput.Text.Trim(),
                .email = emailInput.Text.Trim(),
                .password = passwordInput.Text,
                .password_confirmation = confirmPasswordInput.Text
            }

            Dim json As String = JsonSerializer.Serialize(payload)

            Using client As New HttpClient()
                client.Timeout = TimeSpan.FromSeconds(15)

                Using content As New StringContent(json, Encoding.UTF8, "application/json")
                    Using response As HttpResponseMessage =
                        Await client.PostAsync(RegisterApiUrl, content)

                        Dim responseBody As String =
                            Await response.Content.ReadAsStringAsync()

                        If Not response.IsSuccessStatusCode Then
                            ShowApiError(response.StatusCode, responseBody, True)
                            Return
                        End If

                        Dim serverMessage As String =
                            "Account created successfully. Please wait for Owner approval before logging in."

                        Try
                            Using document As JsonDocument = JsonDocument.Parse(responseBody)
                                Dim messageElement As JsonElement
                                If document.RootElement.TryGetProperty("message", messageElement) AndAlso
                                   messageElement.ValueKind = JsonValueKind.String AndAlso
                                   Not String.IsNullOrWhiteSpace(messageElement.GetString()) Then
                                    serverMessage = messageElement.GetString()
                                End If
                            End Using
                        Catch ex As JsonException
                            ' Keep the safe default message if the response has no valid JSON.
                        End Try

                        registrationMode = False
                        ApplyMode()
                        passwordInput.Clear()
                        confirmPasswordInput.Clear()
                        nameInput.Clear()
                        statusLabel.ForeColor = Color.FromArgb(23, 107, 58)
                        statusLabel.Text = serverMessage
                    End Using
                End Using
            End Using

        Catch ex As TaskCanceledException
            ShowError("The registration request timed out.")

        Catch ex As HttpRequestException
            ShowError("Cannot connect. Make sure the Laravel server is running.")

        Catch ex As JsonException
            ShowError("The server returned an invalid response.")

        Catch ex As Exception
            ShowError("An unexpected registration error occurred.")

        Finally
            loginButton.Enabled = True
            modeToggleButton.Enabled = True
        End Try
    End Function

    Private Async Function LoginAsync() As Task
        If String.IsNullOrWhiteSpace(emailInput.Text) OrElse
           String.IsNullOrWhiteSpace(passwordInput.Text) Then

            ShowError("Enter your email and password.")
            Return
        End If

        loginButton.Enabled = False
        modeToggleButton.Enabled = False
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

                Using content As New StringContent(json, Encoding.UTF8, "application/json")
                    Using response As HttpResponseMessage =
                        Await client.PostAsync(ApiUrl, content)

                        Dim responseBody As String =
                            Await response.Content.ReadAsStringAsync()

                        If Not response.IsSuccessStatusCode Then
                            ShowApiError(response.StatusCode, responseBody, False)
                            Return
                        End If

                        Using document As JsonDocument = JsonDocument.Parse(responseBody)
                            Dim root As JsonElement = document.RootElement
                            Dim successElement As JsonElement

                            If Not root.TryGetProperty("success", successElement) OrElse
                               successElement.ValueKind <> JsonValueKind.True Then
                                ShowError("Login was not confirmed by the server.")
                                Return
                            End If

                            Dim tokenElement As JsonElement
                            If Not root.TryGetProperty("token", tokenElement) OrElse
                               tokenElement.ValueKind <> JsonValueKind.String Then
                                ShowError("Login succeeded, but no API token was returned.")
                                Return
                            End If

                            receivedToken = tokenElement.GetString()
                            If String.IsNullOrWhiteSpace(receivedToken) Then
                                ShowError("The server returned an empty API token.")
                                Return
                            End If

                            Dim tokenTypeElement As JsonElement
                            If root.TryGetProperty("token_type", tokenTypeElement) AndAlso
                               tokenTypeElement.ValueKind = JsonValueKind.String Then
                                receivedTokenType = tokenTypeElement.GetString()
                            End If

                            If Not String.Equals(receivedTokenType, "Bearer",
                                                 StringComparison.OrdinalIgnoreCase) Then
                                ShowError("The server returned an unsupported token type.")
                                Return
                            End If

                            Dim userElement As JsonElement
                            If Not root.TryGetProperty("user", userElement) OrElse
                               userElement.ValueKind <> JsonValueKind.Object Then
                                ShowError("Login response is missing user information.")
                                Return
                            End If

                            Dim idElement As JsonElement
                            Dim nameElement As JsonElement
                            Dim emailElement As JsonElement
                            Dim rolesElement As JsonElement

                            If userElement.TryGetProperty("id", idElement) Then
                                If Not Integer.TryParse(idElement.ToString(), parsedUserId) OrElse
                                   parsedUserId <= 0 Then
                                    ShowError("The server returned an invalid user ID.")
                                    Return
                                End If
                            Else
                                ShowError("The server did not return a user ID.")
                                Return
                            End If

                            If userElement.TryGetProperty("name", nameElement) AndAlso
                               nameElement.ValueKind = JsonValueKind.String Then
                                parsedUserName = nameElement.GetString()
                            End If

                            If userElement.TryGetProperty("email", emailElement) AndAlso
                               emailElement.ValueKind = JsonValueKind.String Then
                                parsedUserEmail = emailElement.GetString()
                            End If

                            If userElement.TryGetProperty("roles", rolesElement) AndAlso
                               rolesElement.ValueKind = JsonValueKind.Array Then
                                For Each roleElement As JsonElement In rolesElement.EnumerateArray()
                                    If roleElement.ValueKind = JsonValueKind.String Then
                                        Dim roleName As String = roleElement.GetString()
                                        If Not String.IsNullOrWhiteSpace(roleName) Then
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
                                ShowError("The server returned incomplete user information.")
                                Return
                            End If
                        End Using
                    End Using
                End Using
            End Using

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
            ShowError("The server request timed out.")

        Catch ex As HttpRequestException
            ShowError("Cannot connect. Make sure the Laravel server is running.")

        Catch ex As JsonException
            ShowError("The server returned an invalid response.")

        Catch ex As Exception
            ShowError("An unexpected login error occurred.")

        Finally
            loginButton.Enabled = True
            modeToggleButton.Enabled = True
        End Try
    End Function

    Private Sub ShowError(message As String)
        statusLabel.ForeColor = Color.Firebrick
        statusLabel.Text = message
    End Sub

    Private Sub ShowApiError(statusCode As System.Net.HttpStatusCode,
                             responseBody As String,
                             isRegistration As Boolean)
        If Not isRegistration AndAlso CInt(statusCode) = 403 Then
            Dim errorCode As String = String.Empty

            Try
                Using document As JsonDocument = JsonDocument.Parse(responseBody)
                    Dim codeElement As JsonElement
                    If document.RootElement.TryGetProperty("code", codeElement) AndAlso
                       codeElement.ValueKind = JsonValueKind.String Then
                        errorCode = codeElement.GetString()
                    End If
                End Using
            Catch ex As JsonException
                ' Use the generic forbidden message when the body is not valid JSON.
            End Try

            If String.Equals(errorCode, "ACCOUNT_PENDING_APPROVAL",
                             StringComparison.OrdinalIgnoreCase) Then
                ShowError("Your account is awaiting Owner approval. Please try again after approval.")
            Else
                ShowError("Access denied. Your account may not be approved for access.")
            End If

        ElseIf CInt(statusCode) = 401 Then
            ShowError("Invalid email or password.")

        ElseIf CInt(statusCode) = 422 Then
            Dim validationMessage As String = String.Empty

            Try
                Using document As JsonDocument = JsonDocument.Parse(responseBody)
                    Dim errorsElement As JsonElement
                    If document.RootElement.TryGetProperty("errors", errorsElement) AndAlso
                       errorsElement.ValueKind = JsonValueKind.Object Then

                        For Each item As JsonProperty In errorsElement.EnumerateObject()
                            If item.Value.ValueKind = JsonValueKind.Array AndAlso
                               item.Value.GetArrayLength() > 0 AndAlso
                               item.Value(0).ValueKind = JsonValueKind.String Then
                                validationMessage = item.Value(0).GetString()
                                Exit For
                            End If
                        Next
                    End If

                    If String.IsNullOrWhiteSpace(validationMessage) Then
                        Dim messageElement As JsonElement
                        If document.RootElement.TryGetProperty("message", messageElement) AndAlso
                           messageElement.ValueKind = JsonValueKind.String Then
                            validationMessage = messageElement.GetString()
                        End If
                    End If
                End Using
            Catch ex As JsonException
                validationMessage = String.Empty
            End Try

            If Not String.IsNullOrWhiteSpace(validationMessage) Then
                ShowError(validationMessage)
            ElseIf isRegistration Then
                ShowError("Registration failed. Please check the submitted details.")
            Else
                ShowError("Please check your email and password format.")
            End If

        ElseIf CInt(statusCode) = 429 Then
            ShowError("Too many attempts. Please wait a moment and try again.")

        Else
            ShowError(If(isRegistration,
                         "Registration request failed. HTTP " & CInt(statusCode).ToString(),
                         "Login request failed. HTTP " & CInt(statusCode).ToString()))
        End If
    End Sub

End Class
