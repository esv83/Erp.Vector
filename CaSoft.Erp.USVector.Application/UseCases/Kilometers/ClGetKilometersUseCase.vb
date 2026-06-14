Public Class ClGetKilometersUseCase
    Inherits ClUseCaseBase
    Implements IUseCase

    Private _query As Guid
    Private _repository As ICrewRepository
    Private _cache As ICrewCache
    Public Sub New(query As Guid, cache As ICrewCache, Repository As ICrewRepository)
        _query = query
        _repository = Repository
        _cache = cache
    End Sub

    Public Overrides Sub execute(presenter As IResponseHandler) Implements IUseCase.Execute

        If CanExecute() Then
            Try
                Dim KmModel As New ClKmModel
                Dim crew = _cache.GetCrew(_query)
                If crew.Vehicle.HasLastKilometers Then
                    Response.SetResult(New ClKmModel With {.Km = crew.Vehicle.LastKilometers.Kilometers})
                Else
                    Response.SetResult(New ClKmModel With {.Km = 0})
                End If
            Catch ex As Exception
                Response.AddError(ex.Message)
            Finally
                presenter.Handle(Response)
            End Try
        End If


    End Sub

    Public Overrides Sub Before()
    End Sub

End Class
