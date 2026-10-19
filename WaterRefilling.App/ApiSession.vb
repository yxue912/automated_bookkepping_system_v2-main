Imports System
Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Net
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Collections.Generic

Public NotInheritable Class ApiSession

    Private Sub New()
    End Sub

    Private Shared Function EffectiveBaseAddress() As Uri
        Dim configured As String = Environment.GetEnvironmentVariable("AQUA_ARC_API_BASE_URL")
        If String.IsNullOrWhiteSpace(configured) Then
            configured = "http://127.0.0.1:8000/api/"
        End If
        Dim parsed As Uri = Nothing
        If Not Uri.TryCreate(configured.TrimEnd("/"c) & "/", UriKind.Absolute, parsed) OrElse
            (parsed.Scheme <> Uri.UriSchemeHttp AndAlso parsed.Scheme <> Uri.UriSchemeHttps) Then
            Throw New InvalidOperationException("AQUA_ARC_API_BASE_URL must be an absolute HTTP or HTTPS API URL.")
        End If
        Return parsed
    End Function

    Private NotInheritable Class SessionHandler
        Inherits DelegatingHandler

        Public Sub New()
            MyBase.New(New HttpClientHandler())
        End Sub

        Protected Overrides Async Function SendAsync(
            request As HttpRequestMessage,
            cancellationToken As CancellationToken
        ) As Task(Of HttpResponseMessage)
            Dim response As HttpResponseMessage =
                Await MyBase.SendAsync(request, cancellationToken)
            If response.StatusCode = HttpStatusCode.Unauthorized Then
                ApiSession.Clear()
            End If
            Return response
        End Function
    End Class

    Public Shared ReadOnly Client As New HttpClient(New SessionHandler()) With {
        .BaseAddress = EffectiveBaseAddress(),
        .Timeout = TimeSpan.FromSeconds(30)
    }

    Public Shared Property BearerToken As String

    Public Shared Property CurrentUserId As Integer

    Public Shared Property CurrentUserName As String

    Public Shared Property CurrentUserEmail As String

    Public Shared Property CurrentUserRoles As New List(Of String)

    Public Shared Property CurrentUserPermissions As New List(Of String)

    Public Shared Property HasServerPermissions As Boolean

    Public Shared Sub ApplyAuthorization()
        Client.DefaultRequestHeaders.Authorization = Nothing

        If Not String.IsNullOrWhiteSpace(BearerToken) Then
            Client.DefaultRequestHeaders.Authorization =
                New AuthenticationHeaderValue("Bearer", BearerToken)
        End If
    End Sub

    Public Shared Sub Clear()
        BearerToken = Nothing
        CurrentUserId = 0
        CurrentUserName = Nothing
        CurrentUserEmail = Nothing
        CurrentUserRoles = New List(Of String)()
        CurrentUserPermissions = New List(Of String)()
        HasServerPermissions = False
        ApplyAuthorization()
    End Sub

End Class
