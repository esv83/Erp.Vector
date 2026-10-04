Imports System.Threading

Imports CaSoft.Erp.USVector.Application.Dto

' Conducteur d'équipage : conducteur actif + membres sélectionnables + véhicule — Result pattern.
Public Class ClGetDriverUseCase
    Implements IResultUseCaseAsync(Of ClLogDriverModel)

    Private ReadOnly _repository As ICrewRepository
    Private ReadOnly _query As Guid

    Public Sub New(Query As Guid, Repository As ICrewRepository)
        _query = Query
        _repository = Repository
    End Sub

    Public Async Function HandleAsync(ct As CancellationToken) As Task(Of ClResult(Of ClLogDriverModel)) Implements IResultUseCaseAsync(Of ClLogDriverModel).HandleAsync

        Dim crew = Await _repository.GetCrewAsync(_query, ct)
        If crew Is Nothing Then
            Return ClResult(Of ClLogDriverModel).Fail(ClError.Application($"Équipage {_query} introuvable côté ERP."))
        End If

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

    End Function

End Class
