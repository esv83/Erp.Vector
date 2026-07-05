
Imports CaSoft.Erp.USVector.Application.Dto

' Conducteur d'équipage : conducteur actif + membres sélectionnables + véhicule — Result pattern.
Public Class ClGetDriverUseCase
    Implements IResultUseCase(Of ClLogDriverModel)

    Private ReadOnly _repository As ICrewRepository
    Private ReadOnly _query As Guid

    Public Sub New(Query As Guid, Repository As ICrewRepository)
        _query = Query
        _repository = Repository
    End Sub

    Public Function Handle() As ClResult(Of ClLogDriverModel) Implements IResultUseCase(Of ClLogDriverModel).Handle

        Try
            Dim crew = _repository.GetCrew(_query)
            Dim lastDriver = crew.LastDriver

            Dim logDriverModel As New ClLogDriverModel
            With logDriverModel
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

            Return ClResult(Of ClLogDriverModel).Ok(logDriverModel)

        Catch ex As Exception
            Return ClResult(Of ClLogDriverModel).Fail(ClError.Application(ex.Message, ex))
        End Try

    End Function

End Class
