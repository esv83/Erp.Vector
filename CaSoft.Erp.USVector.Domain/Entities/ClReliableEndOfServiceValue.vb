Imports CaSoft.Framework

Public Class ClEndOfServiceInfo
    Inherits ClValueInfo(Of DateTime)

    Public Sub New(time As DateTime, source As String)
        MyBase.New(time, source)

        _Source = source

    End Sub

    Public ReadOnly Property Source As ValueInfoSource


End Class
