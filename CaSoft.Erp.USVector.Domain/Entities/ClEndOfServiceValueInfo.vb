Imports CaSoft.Framework

Public Class ClEndOfServiceValueInfo
    Inherits ClIdValue
    Public Sub New(value As Date, source As String)
        MyBase.New(value, source)

        _Source = source
    End Sub

    Public Property Source As ValueInfoSource
End Class
