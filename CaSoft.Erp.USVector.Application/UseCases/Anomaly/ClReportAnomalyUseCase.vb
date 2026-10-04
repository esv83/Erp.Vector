Imports System.Threading

''' <summary>
''' TRF-8 — Signale une anomalie terrain sur une mission. Anomalie non bloquante : simple
''' enregistrement historisé, transféré ensuite dans le paquet field-data. Result pattern.
''' </summary>
Public Class ClReportAnomalyUseCase
    Implements IResultUseCaseAsync(Of ClAnomalyDtoOut)

    Private ReadOnly _command As ClReportAnomalyCommand
    Private ReadOnly _repository As IAnomalyRepository

    Public Sub New(command As ClReportAnomalyCommand, repository As IAnomalyRepository)
        _command = command
        _repository = repository
    End Sub

    Public Async Function HandleAsync(ct As CancellationToken) As Task(Of ClResult(Of ClAnomalyDtoOut)) Implements IResultUseCaseAsync(Of ClAnomalyDtoOut).HandleAsync
        If _command.MissionId = Guid.Empty Then
            Return ClResult(Of ClAnomalyDtoOut).Fail(ClError.Application("Mission obligatoire."))
        End If
        If Not [Enum].IsDefined(GetType(EnAnomalyType), _command.Input.Type) Then
            Return ClResult(Of ClAnomalyDtoOut).Fail(ClError.Application("Type d'anomalie invalide."))
        End If

        Dim anomaly As New ClAnomaly With {
            .Id = Guid.NewGuid(),
            .MissionId = _command.MissionId,
            .Type = CType(_command.Input.Type, EnAnomalyType),
            .Text = _command.Input.Text,
            .ReportedAt = DateTime.UtcNow,
            .ReportedCrewId = _command.Input.CrewId
        }

        Await _repository.SaveAsync(anomaly, ct)
        Return ClResult(Of ClAnomalyDtoOut).Ok(anomaly.ToDtoOut())
    End Function

End Class
