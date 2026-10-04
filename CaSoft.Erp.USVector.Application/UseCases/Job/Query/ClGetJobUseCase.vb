Imports System.Threading

' Détail d'une mission, assemblé depuis Orders et la base Vector — Result pattern.
Public Class ClGetJobUseCase
    Implements IResultUseCaseAsync(Of ClJobDetailModel)

    Private ReadOnly _query As Guid
    Private ReadOnly _repository As IJobRepository

    Public Sub New(Query As Guid, repository As IJobRepository)
        _query = Query
        _repository = repository
    End Sub

    Public Async Function HandleAsync(ct As CancellationToken) As Task(Of ClResult(Of ClJobDetailModel)) Implements IResultUseCaseAsync(Of ClJobDetailModel).HandleAsync

        ' Le garde-fou d'accès a déjà vérifié la mission ; ce refus ne sert que si elle disparaît entre-temps.
        Dim job = Await _repository.GetJobAsync(_query, ct)
        If job Is Nothing Then
            Return ClResult(Of ClJobDetailModel).Fail(ClError.Application($"Mission {_query} introuvable côté ERP."))
        End If

        Return ClResult(Of ClJobDetailModel).Ok(New ClJobDetailAdapter(job))

    End Function

End Class
