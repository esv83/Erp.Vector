
' Détail d'une mission (assemblé depuis le cache) — Result pattern.
Public Class ClGetJobUseCase
    Implements IResultUseCase(Of ClJobDetailModel)

    Private ReadOnly _query As Guid
    Private ReadOnly _cache As IJobCache

    Public Sub New(Query As Guid, cache As IJobCache)
        _query = Query
        _cache = cache
    End Sub

    Public Function Handle() As ClResult(Of ClJobDetailModel) Implements IResultUseCase(Of ClJobDetailModel).Handle

        Try
            Dim job = _cache.GetJob(_query)
            Dim jobDetail As ClJobDetailModel = New ClJobDetailAdapter(job)
            Return ClResult(Of ClJobDetailModel).Ok(jobDetail)
        Catch ex As Exception
            Return ClResult(Of ClJobDetailModel).Fail(ClError.Application(ex.Message, ex))
        End Try

    End Function

End Class
