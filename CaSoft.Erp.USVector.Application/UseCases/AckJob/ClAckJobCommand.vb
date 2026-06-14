Public Class ClAckJobCommand
    Private _jobId As Guid

    Public Sub New(jobId As Guid)
        _jobId = jobId
    End Sub

    Public ReadOnly Property JobId As Guid
        Get
            Return _jobId
        End Get
    End Property

End Class
