
Public Class ClJobQueryWithCache


    Public Sub New(gJobID As Guid, cache As Object)
        _JobID = gJobID
        _JobListCache = cache
    End Sub

    Public ReadOnly Property JobID As Guid
    Public ReadOnly Property JobListCache As ClJobListCache

End Class
