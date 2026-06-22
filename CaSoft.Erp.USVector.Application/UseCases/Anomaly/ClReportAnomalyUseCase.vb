''' <summary>
''' TRF-8 — Signale une anomalie terrain sur une mission. Anomalie non bloquante : simple
''' enregistrement historisé, transféré ensuite dans le paquet field-data.
''' </summary>
Public Class ClReportAnomalyUseCase
    Inherits ClUseCaseBase
    Implements IUseCase

    Private _command As ClReportAnomalyCommand
    Private _repository As IAnomalyRepository

    Public Sub New(command As ClReportAnomalyCommand, repository As IAnomalyRepository)
        _command = command
        _repository = repository
    End Sub

    Public Overrides Sub execute(presenter As IResponseHandler) Implements IUseCase.Execute
        Try
            If _command.MissionId = Guid.Empty Then
                Response.AddError("Mission obligatoire.")
                Return
            End If
            If Not [Enum].IsDefined(GetType(EnAnomalyType), _command.Input.Type) Then
                Response.AddError("Type d'anomalie invalide.")
                Return
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
            Response.SetResult(anomaly.ToDtoOut())
        Catch ex As Exception
            Response.AddError(ex.Message)
        Finally
            presenter.Handle(Response)
        End Try
    End Sub

    Public Overrides Sub Before()
    End Sub

End Class
