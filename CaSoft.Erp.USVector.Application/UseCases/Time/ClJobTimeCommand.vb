Public Class ClJobTimeCommand


    Public Sub New(gJobId As Guid, jobTime As ClJobTimeModel)

        _JobTime = jobTime
        _JobId = gJobId

    End Sub
    Public ReadOnly Property JobId As Guid

    Public ReadOnly Property JobTime As ClJobTimeModel

End Class
