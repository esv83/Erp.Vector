
Imports CaSoft.Erp.USVector.Application.Dto

Public Class ClGetDriverUseCase
    Inherits ClUseCaseBase

    Private _repository As ICrewRepository
    Private _query As Guid
    Public Sub New(Query As Guid, Repository As ICrewRepository)
        _query = Query
        _repository = Repository
    End Sub

    Public Overrides sub execute(presenter as IResponseHandler)
        If CanExecute() Then
            Try

                Dim crew = _repository.GetCrew(_query)
                Dim lastDriver = crew.LastDriver

                Dim LogDriverModel As New ClLogDriverModel
                With LogDriverModel
                    .DriversCollection = New ClDriverListModel(crew.EmployeeList)
                    .VehicleModel = New ClVehicleModel(crew.Vehicle)
                    If lastDriver IsNot Nothing Then
                        .ChangeDate = lastDriver.From
                        .SelectedDriver = New ClDriverModel(lastDriver.Employee)
                    Else
                        ' Aucun conducteur désigné : le contrat garantit un SelectedDriver non-null
                        ' (le client legacy lit SelectedDriver.DriverName sans garde). Conducteur « vide »
                        ' → Guid vide, non présent dans DriversCollection = rien de pré-sélectionné.
                        .SelectedDriver = New ClDriverModel(Guid.Empty, String.Empty)
                    End If
                End With

                Response.SetResult(LogDriverModel)

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
