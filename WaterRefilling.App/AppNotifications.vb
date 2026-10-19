Imports System
Imports System.Collections.Generic
Imports System.Linq

Public NotInheritable Class AppNotifications

    Private Sub New()
    End Sub

    Public Class NotificationItem
        Public Property Title As String
        Public Property Message As String
        Public Property CreatedAt As DateTime
        Public Property IsRead As Boolean
    End Class

    Private Shared ReadOnly notificationList As New List(Of NotificationItem)()
    Private Shared ReadOnly notificationLock As New Object()

    Public Shared Event NotificationAdded(
        notification As NotificationItem)

    Public Shared Function GetNotifications() As List(Of NotificationItem)
        SyncLock notificationLock
            Return notificationList.
                OrderByDescending(Function(item) item.CreatedAt).
                ToList()
        End SyncLock
    End Function

    Public Shared Function GetUnreadCount() As Integer
        SyncLock notificationLock
            Return notificationList.Where(
                Function(item) Not item.IsRead).Count()
        End SyncLock
    End Function

    Public Shared Sub AddNotification(
        title As String,
        message As String)

        If String.IsNullOrWhiteSpace(title) Then Return

        Dim notification As New NotificationItem With {
            .Title = title.Trim(),
            .Message = If(message, String.Empty).Trim(),
            .CreatedAt = DateTime.Now,
            .IsRead = False
        }

        SyncLock notificationLock
            notificationList.Insert(0, notification)

            If notificationList.Count > 100 Then
                notificationList.RemoveAt(notificationList.Count - 1)
            End If
        End SyncLock

        RaiseEvent NotificationAdded(notification)
    End Sub

    Public Shared Sub MarkAllAsRead()
        SyncLock notificationLock
            For Each notification As NotificationItem In notificationList
                notification.IsRead = True
            Next
        End SyncLock
    End Sub

    Public Shared Sub MarkAsRead(
        notification As NotificationItem)

        If notification Is Nothing Then Return

        SyncLock notificationLock
            notification.IsRead = True
        End SyncLock
    End Sub

    Public Shared Sub ClearNotifications()
        SyncLock notificationLock
            notificationList.Clear()
        End SyncLock
    End Sub

End Class