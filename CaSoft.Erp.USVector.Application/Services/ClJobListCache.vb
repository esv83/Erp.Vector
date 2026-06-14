Public Class ClJobListCache
    Inherits List(Of ClJob)
    Implements IJobCache

    Private _repository As IJobRepository
    Public Sub New(repository As IJobRepository)
        _repository = repository
    End Sub
    Public Function GetJob(gJobId As Guid) As ClJob Implements IJobCache.GetJob
        Dim result As ClJob = Nothing

        result = Me.SingleOrDefault(Function(f) f.Id = gJobId)
        If result Is Nothing Then
            result = _repository.GetJob(gJobId)
            Me.Add(result)
        End If

        Return result

    End Function

    ''' <summary>
    ''' A tester, jamais utilisée
    ''' </summary>
    Public Sub CleanCache()
        If Me.Count > 0 Then
            For i = Me.Count - 1 To 0 Step -1
                Dim delai = DateTime.Now.Subtract(Me(i).Schedule)
                If delai.TotalHours > 24 Then
                    Me.RemoveAt(i)
                End If
            Next
        End If

    End Sub

End Class

