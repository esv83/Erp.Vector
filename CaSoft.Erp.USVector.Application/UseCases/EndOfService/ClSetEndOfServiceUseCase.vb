' Affecte une fin de service à un équipage — Result pattern.
Public Class ClSetEndOfServiceUseCase
    Implements IResultUseCase(Of Boolean)

    Private ReadOnly _repository As ICrewRepository
    Private ReadOnly _cache As ICrewCache
    Private ReadOnly _query As ClSetEndOfServiceCommand

    Public Sub New(query As ClSetEndOfServiceCommand, cache As ICrewCache, repository As ICrewRepository)
        _query = query
        _repository = repository
        _cache = cache
    End Sub

    Public Function Handle() As ClResult(Of Boolean) Implements IResultUseCase(Of Boolean).Handle
        Try
            Dim crew = _cache.GetCrew(_query.CrewId)
            If crew Is Nothing Then
                Return ClResult(Of Boolean).Fail(ClError.Application("Le Crew est null (nothing)"))
            End If

            Dim valuInfo As New ClValueInfo(Of DateTime)(_query.EndOfServiceDate, _query.Source)
            crew.ServiceEndDateR.AddValueInfo(valuInfo)
            'TODO Generer une exception quand on affecte une fin de service a un crew qui n'a pas pris son service
            _repository.Update(crew)

            Return ClResult(Of Boolean).Ok(True)
        Catch ex As Exception
            Return ClResult(Of Boolean).Fail(ClError.Application(ex.Message, ex))
        End Try
    End Function

End Class
