
' Lecture du kilométrage véhicule — Result pattern. (Sans consommateur actif.)
Public Class ClGetKilometersUseCase
    Implements IResultUseCase(Of ClKmModel)

    Private ReadOnly _query As Guid
    Private ReadOnly _repository As ICrewRepository
    Private ReadOnly _cache As ICrewCache

    Public Sub New(query As Guid, cache As ICrewCache, Repository As ICrewRepository)
        _query = query
        _repository = Repository
        _cache = cache
    End Sub

    Public Function Handle() As ClResult(Of ClKmModel) Implements IResultUseCase(Of ClKmModel).Handle

        Try
            Dim crew = _cache.GetCrew(_query)
            If crew.Vehicle.HasLastKilometers Then
                Return ClResult(Of ClKmModel).Ok(New ClKmModel With {.Km = crew.Vehicle.LastKilometers.Kilometers})
            Else
                Return ClResult(Of ClKmModel).Ok(New ClKmModel With {.Km = 0})
            End If
        Catch ex As Exception
            Return ClResult(Of ClKmModel).Fail(ClError.Application(ex.Message, ex))
        End Try

    End Function

End Class
