' Détail d'un équipage (ERP-backed) — Result pattern.
Friend Class ClGetCrewUseCase
    Implements IResultUseCase(Of ClCrew)

    Private ReadOnly _query As Guid
    Private ReadOnly _repository As ICrewRepository

    Public Sub New(query As Guid, repository As ICrewRepository)
        _query = query
        _repository = repository
    End Sub

    Public Function Handle() As ClResult(Of ClCrew) Implements IResultUseCase(Of ClCrew).Handle

        Try
            Dim crew = _repository.GetCrew(_query)
            Return ClResult(Of ClCrew).Ok(crew)
        Catch ex As Exception
            Return ClResult(Of ClCrew).Fail(ClError.Application(ex.Message, ex))
        End Try

    End Function

End Class
