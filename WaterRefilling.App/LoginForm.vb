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
        Text = "Aqua ARC | Sign In"
        StartPosition = FormStartPosition.CenterScreen
        ClientSize = New Size(1080, 720)
        MinimumSize = New Size(1000, 680)
        MaximizeBox = False
        FormBorderStyle = FormBorderStyle.FixedSingle
        BackColor = Color.FromArgb(241, 246, 243)
        Font = New Font("Segoe UI", 10.0F, FontStyle.Regular)

        ' Use a real two-column layout so the brand panel can never cover the form.
        Dim layout As New TableLayoutPanel With {
            .Name = "loginLayout",
            .Dock = DockStyle.Fill,
            .ColumnCount = 2,
            .RowCount = 1,
            .Margin = New Padding(0),
            .Padding = New Padding(0),
            .BackColor = Color.FromArgb(241, 246, 243)
        }
        layout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 35.0F))
        layout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 65.0F))
        layout.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))

        Dim branding As New Panel With {
            .Name = "brandingPanel",
            .Dock = DockStyle.Fill,
            .BackColor = Color.FromArgb(13, 61, 46),
            .Padding = New Padding(32)
        }

        Dim contentPanel As New Panel With {
            .Name = "contentPanel",
            .Dock = DockStyle.Fill,
            .BackColor = Color.FromArgb(241, 246, 243),
            .Padding = New Padding(24)
        }

        layout.Controls.Add(branding, 0, 0)
        layout.Controls.Add(contentPanel, 1, 0)
        Controls.Add(layout)

        Dim accentEdge As New Panel With {
            .Dock = DockStyle.Right,
            .Width = 5,
            .BackColor = Color.FromArgb(53, 178, 122)
        }
        branding.Controls.Add(accentEdge)

        ' Brand name is the primary mark; no separate initial badge.
        Dim brandTitle As New Label With {
            .Text = "AQUA ARC",
            .Font = New Font("Segoe UI", 21.0F, FontStyle.Bold),
            .ForeColor = Color.White,
            .Location = New Point(38, 76),
            .Size = New Size(290, 46),
            .AutoSize = False,
            .TextAlign = ContentAlignment.MiddleLeft,
            .UseMnemonic = False,
            .Padding = New Padding(0)
        }
        branding.Controls.Add(brandTitle)

        Dim brandKicker As New Label With {
            .Text = "WATER STATION BOOKKEEPING",
            .Font = New Font("Segoe UI", 8.5F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(135, 224, 181),
            .Location = New Point(40, 126),
            .Size = New Size(285, 24),
            .AutoSize = False,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Padding = New Padding(0)
        }
        branding.Controls.Add(brandKicker)

        Dim brandRule As New Panel With {
            .Location = New Point(40, 166),
            .Size = New Size(54, 4),
            .BackColor = Color.FromArgb(65, 194, 133)
        }
        branding.Controls.Add(brandRule)

        ' Keep the brand panel informative and balanced without a clipped slogan.
        Dim brandDescription As New Label With {
            .Text = "A focused workspace for recording daily transactions, monitoring cash flow, and keeping financial reports organized.",
            .Font = New Font("Segoe UI", 10.5F, FontStyle.Regular),
            .ForeColor = Color.FromArgb(207, 226, 216),
            .Location = New Point(40, 266),
            .Size = New Size(276, 104)
        }
        branding.Controls.Add(brandDescription)

        Dim featurePanel As New Panel With {
            .Location = New Point(40, 382),
            .Size = New Size(276, 118),
            .BackColor = Color.FromArgb(20, 76, 59)
        }
        branding.Controls.Add(featurePanel)

        Dim featureHeading As New Label With {
            .Text = "YOUR WORKSPACE",
            .Font = New Font("Segoe UI", 8.0F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(135, 224, 181),
            .Location = New Point(16, 14),
            .Size = New Size(240, 18)
        }
        featurePanel.Controls.Add(featureHeading)

        Dim featureTransactions As New Label With {
            .Text = "01   Daily transactions",
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Regular),
            .ForeColor = Color.FromArgb(242, 248, 244),
            .Location = New Point(16, 39),
            .Size = New Size(240, 20)
        }
        featurePanel.Controls.Add(featureTransactions)

        Dim featureCashFlow As New Label With {
            .Text = "02   Cash flow tracking",
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Regular),
            .ForeColor = Color.FromArgb(242, 248, 244),
            .Location = New Point(16, 63),
            .Size = New Size(240, 20)
        }
        featurePanel.Controls.Add(featureCashFlow)

        Dim featureReports As New Label With {
            .Text = "03   Financial reports",
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Regular),
            .ForeColor = Color.FromArgb(242, 248, 244),
            .Location = New Point(16, 87),
            .Size = New Size(240, 20)
        }
        featurePanel.Controls.Add(featureReports)

        Dim brandFooter As New Label With {
            .Text = "BUILT FOR CLEARER DAILY BOOKKEEPING",
            .Font = New Font("Segoe UI", 7.5F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(151, 190, 170),
            .Location = New Point(40, 628),
            .Size = New Size(276, 18)
        }
        branding.Controls.Add(brandFooter)

        ' Elevated, generously spaced form card.
        Dim loginCard As New Panel With {
            .Name = "loginCard",
            .BackColor = Color.White,
            .Size = New Size(480, 590),
            .Location = New Point(0, 0),
            .Padding = New Padding(40)
        }
        contentPanel.Controls.Add(loginCard)
        Dim centerLoginCard As Action =
            Sub()
                loginCard.Left = Math.Max(16, (contentPanel.ClientSize.Width - loginCard.Width) \ 2)
                loginCard.Top = Math.Max(16, (contentPanel.ClientSize.Height - loginCard.Height) \ 2)
            End Sub
        AddHandler contentPanel.Resize, Sub(sender As Object, e As EventArgs) centerLoginCard()
        AddHandler Shown, Sub(sender As Object, e As EventArgs) centerLoginCard()

        ' Keep the accent in the card's top edge; docking would respect the card padding
        ' and place it over the ACCOUNT ACCESS heading.
        Dim cardAccent As New Panel With {
            .Name = "cardAccent",
            .Location = New Point(0, 0),
            .Size = New Size(loginCard.Width, 5),
            .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right,
            .BackColor = Color.FromArgb(39, 153, 101)
        }
        loginCard.Controls.Add(cardAccent)
        cardAccent.SendToBack()

        Dim sectionEyebrow As New Label With {
            .Text = "ACCOUNT ACCESS",
            .Font = New Font("Segoe UI", 8.0F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(39, 139, 94),
            .Location = New Point(40, 28),
            .Size = New Size(390, 18)
        }
        loginCard.Controls.Add(sectionEyebrow)

        formTitle.Text = "Welcome back"
        formTitle.Font = New Font("Segoe UI", 23.0F, FontStyle.Bold)
        formTitle.ForeColor = Color.FromArgb(20, 52, 39)
        formTitle.Location = New Point(40, 50)
        formTitle.Size = New Size(400, 44)
        formTitle.AutoEllipsis = True
        loginCard.Controls.Add(formTitle)

        formDescription.Text = "Sign in to continue to your workspace."
        formDescription.Font = New Font("Segoe UI", 9.5F, FontStyle.Regular)
        formDescription.ForeColor = Color.FromArgb(105, 122, 113)
        formDescription.Location = New Point(42, 96)
        formDescription.Size = New Size(395, 30)
        loginCard.Controls.Add(formDescription)

        ConfigureLabel(nameLabel, "Full name")
        ConfigureLabel(emailLabel, "Email address")
        ConfigureLabel(passwordLabel, "Password")
        ConfigureLabel(confirmPasswordLabel, "Confirm password")

        ConfigureTextBox(nameInput, False)
        ConfigureTextBox(emailInput, False)
        ConfigureTextBox(passwordInput, True)
        ConfigureTextBox(confirmPasswordInput, True)

        For Each input As TextBox In New TextBox() {nameInput, emailInput, passwordInput, confirmPasswordInput}
            input.Size = New Size(400, 38)
            input.BackColor = Color.FromArgb(249, 251, 250)
            input.ForeColor = Color.FromArgb(30, 48, 39)
            input.Margin = New Padding(0)
        Next

        loginCard.Controls.Add(nameLabel)
        loginCard.Controls.Add(nameInput)
        loginCard.Controls.Add(emailLabel)
        loginCard.Controls.Add(emailInput)
        loginCard.Controls.Add(passwordLabel)
        loginCard.Controls.Add(passwordInput)
        loginCard.Controls.Add(confirmPasswordLabel)
        loginCard.Controls.Add(confirmPasswordInput)

        loginButton.Size = New Size(400, 48)
        loginButton.BackColor = Color.FromArgb(24, 125, 79)
        loginButton.ForeColor = Color.White
        loginButton.Font = New Font("Segoe UI", 10.5F, FontStyle.Bold)
        loginButton.FlatStyle = FlatStyle.Flat
        loginButton.FlatAppearance.BorderSize = 0
        loginButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(17, 101, 64)
        loginButton.Cursor = Cursors.Hand
        loginButton.TabIndex = 4
        loginCard.Controls.Add(loginButton)

        statusLabel.Size = New Size(400, 42)
        statusLabel.ForeColor = Color.Firebrick
        statusLabel.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular)
        statusLabel.TextAlign = ContentAlignment.MiddleLeft
        statusLabel.AutoEllipsis = True
        loginCard.Controls.Add(statusLabel)

        modeToggleButton.Size = New Size(400, 34)
        modeToggleButton.FlatStyle = FlatStyle.Flat
        modeToggleButton.FlatAppearance.BorderSize = 0
        modeToggleButton.BackColor = Color.White
        modeToggleButton.ForeColor = Color.FromArgb(24, 125, 79)
        modeToggleButton.Font = New Font("Segoe UI", 9.5F, FontStyle.Bold)
        modeToggleButton.Cursor = Cursors.Hand
        modeToggleButton.TextAlign = ContentAlignment.MiddleCenter
        modeToggleButton.TabIndex = 5
        loginCard.Controls.Add(modeToggleButton)

        AddHandler loginButton.Click, AddressOf SubmitButton_Click
        AddHandler modeToggleButton.Click, AddressOf ModeToggleButton_Click
    End Sub

    Private Sub ConfigureLabel(label As Label, caption As String)
        label.Text = caption
        label.AutoSize = False
        label.Size = New Size(400, 22)
        label.Font = New Font("Segoe UI", 9.5F, FontStyle.Bold)
        label.ForeColor = Color.FromArgb(42, 61, 50)
    End Sub

    Private Sub ConfigureTextBox(textBox As TextBox, isPassword As Boolean)
        textBox.Size = New Size(400, 38)
        textBox.Font = New Font("Segoe UI", 11.0F, FontStyle.Regular)
        textBox.BorderStyle = BorderStyle.FixedSingle
        textBox.UseSystemPasswordChar = isPassword
    End Sub

    Private Sub ApplyMode()
        If registrationMode Then
            Text = "Aqua ARC | Create Account"
            formTitle.Text = "Create your account"
            formDescription.Text = "Request access to the Aqua ARC workspace."

            nameLabel.Location = New Point(40, 132)
            nameInput.Location = New Point(40, 155)
            emailLabel.Location = New Point(40, 202)
            emailInput.Location = New Point(40, 225)
            passwordLabel.Location = New Point(40, 272)
            passwordInput.Location = New Point(40, 295)
            confirmPasswordLabel.Location = New Point(40, 342)
            confirmPasswordInput.Location = New Point(40, 365)

            nameLabel.Visible = True
            nameInput.Visible = True
            confirmPasswordLabel.Visible = True
            confirmPasswordInput.Visible = True

            loginButton.Text = "Create Account"
            loginButton.Location = New Point(40, 422)
            statusLabel.Location = New Point(40, 472)
            modeToggleButton.Location = New Point(40, 526)
            modeToggleButton.Text = "Already registered?  Sign in"
            passwordInput.TabIndex = 2
            confirmPasswordInput.TabIndex = 3
            loginButton.TabIndex = 4
            AcceptButton = loginButton
        Else
            Text = "Aqua ARC | Sign In"
            formTitle.Text = "Welcome back"
            formDescription.Text = "Sign in to continue to your workspace."

            emailLabel.Location = New Point(40, 166)
            emailInput.Location = New Point(40, 189)
            passwordLabel.Location = New Point(40, 254)
            passwordInput.Location = New Point(40, 277)

            nameLabel.Visible = False
            nameInput.Visible = False
            confirmPasswordLabel.Visible = False
            confirmPasswordInput.Visible = False

            loginButton.Text = "Sign In"
            loginButton.Location = New Point(40, 352)
            statusLabel.Location = New Point(40, 406)
            modeToggleButton.Location = New Point(40, 466)
            modeToggleButton.Text = "New to Aqua ARC?  Create an account"
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
                        statusLabel.ForeColor = Color.FromArgb(22, 91, 62)
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

            statusLabel.ForeColor = Color.FromArgb(22, 91, 62)
            statusLabel.Text = "Login successful."

            Hide()

            Using main As New MainForm()
                main.WindowState = FormWindowState.Maximized
                main.ShowDialog()
            End Using

            If String.IsNullOrWhiteSpace(ApiSession.BearerToken) Then
                ApiSession.Clear()
                passwordInput.Clear()
                statusLabel.ForeColor = Color.FromArgb(22, 91, 62)
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
