' Fin de service d'un équipage (date fiabilisée) — Result pattern.
Public Class ClGetEndOfServiceUseCase
    Implements IResultUseCase(Of ClReliableDateModel)

    Private ReadOnly _repository As ICrewRepository
    Private ReadOnly _query As Guid

    Public Sub New(query As Guid, Repository As ICrewRepository)
        _query = query
        _repository = Repository
    End Sub

    Public Function Handle() As ClResult(Of ClReliableDateModel) Implements IResultUseCase(Of ClReliableDateModel).Handle
        Try
            Dim crew As ClCrew = _repository.GetCrew(_query)
            Return ClResult(Of ClReliableDateModel).Ok(
                New ClReliableDateModel With {.Date = crew.ServiceEndDateR, .Fiability = crew.ServiceEndDateR.Level})
        Catch ex As Exception
            Return ClResult(Of ClReliableDateModel).Fail(ClError.Application(ex.Message, ex))
        End Try
    End Function

End Class
