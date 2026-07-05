''' <summary>
''' TRF-8 — Signale une anomalie terrain sur une mission. Anomalie non bloquante : simple
''' enregistrement historisé, transféré ensuite dans le paquet field-data. Result pattern.
''' </summary>
Public Class ClReportAnomalyUseCase
    Implements IResultUseCase(Of ClAnomalyDtoOut)

    Private ReadOnly _command As ClReportAnomalyCommand
    Private ReadOnly _repository As IAnomalyRepository

    Public Sub New(command As ClReportAnomalyCommand, repository As IAnomalyRepository)
        _command = command
        _repository = repository
    End Sub

    Public Function Handle() As ClResult(Of ClAnomalyDtoOut) Implements IResultUseCase(Of ClAnomalyDtoOut).Handle
        Try
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

            _repository.Save(anomaly)
            Return ClResult(Of ClAnomalyDtoOut).Ok(anomaly.ToDtoOut())
        Catch ex As Exception
            Return ClResult(Of ClAnomalyDtoOut).Fail(ClError.Application(ex.Message, ex))
        End Try
    End Function

End Class
