
Public Class ClGetJobUseCase
    Inherits ClUseCaseBase
    Implements IUseCase

    Private _query As Guid
    Private _cache As IJobCache

    Public Sub New(Query As Guid, cache As IJobCache)
        _query = Query
        _cache = cache
        '   _repository = repository

    End Sub
    Public Overrides Sub execute(presenter As IResponseHandler) Implements IUseCase.Execute
        Try
            Dim job = _cache.GetJob(_query)
            Dim jobDetail As ClJobDetailModel = New ClJobDetailAdapter(job)
            Response.SetResult(jobDetail)
        Catch ex As Exception
            Response.AddError(ex.Message)
        Finally
            presenter.Handle(Response)
        End Try

    End Sub

    Public Overrides Sub Before()

    End Sub
End Class
