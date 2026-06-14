
Public Class ClIdValueList


    Private Shared _instance As ClIdValueList = Nothing
    Private Shared _lock As New Object()

    Private ReadOnly _items As List(Of ClIdValue)

    ' Constructeur privé avec injection
    Private Sub New(data As List(Of ClIdValue))
        _items = data
    End Sub

    ' Accès unique à l'instance avec injection obligatoire au premier appel
    Public Shared Function GetInstance(Optional data As List(Of ClIdValue) = Nothing) As ClIdValueList
        If _instance Is Nothing Then
            SyncLock _lock
                If _instance Is Nothing Then
                    If data Is Nothing Then
                        Throw New InvalidOperationException("Le repository doit être initialisé avec des données au premier appel.")
                    End If
                    _instance = New ClIdValueList(data)
                End If
            End SyncLock
        End If
        Return _instance
    End Function

    Public Function GetAll() As List(Of ClIdValue)
        Return _items
    End Function

    Public Function GetById(id As Integer) As ClIdValue
        Dim item = _items.Single(Function(x) x.Id = id)
        Return item
    End Function
End Class


