' Liste des équipages d'une date (ids) — Result pattern.
Public Class ClGetCrewIdListUseCase
    Implements IResultUseCase(Of List(Of Guid))

    Private ReadOnly _query As DateOnly
    Private ReadOnly _repository As ICrewRepository

    Public Sub New(query As DateOnly, repository As ICrewRepository)
        _query = query
        _repository = repository
    End Sub

    Public Function Handle() As ClResult(Of List(Of Guid)) Implements IResultUseCase(Of List(Of Guid)).Handle

        Try
            Dim crewIdList As List(Of Guid) = _repository.GetCrewIdList(_query)
            Return ClResult(Of List(Of Guid)).Ok(crewIdList)
        Catch ex As Exception
            Return ClResult(Of List(Of Guid)).Fail(ClError.Application(ex.Message, ex))
        End Try

    End Function

End Class
