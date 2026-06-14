Public Class ClGetSignatureQuery

    Public Sub New(gJobId As Guid)
        _JobId = gJobId
    End Sub
    Public ReadOnly Property JobId As Guid

End Class
