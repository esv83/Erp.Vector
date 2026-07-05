' Insertion d'un log mécanique (constat) — Result pattern. Validation : crew + constat requis.
Public Class ClInsertMechanicLogUseCase
    Implements IResultUseCase(Of Boolean)

    Private ReadOnly _repository As ILogRepository
    Private ReadOnly _command As ClInsertLogModel

    Public Sub New(cmd As ClInsertLogModel, repository As ILogRepository)
        _repository = repository
        _command = cmd
    End Sub

    Public Function Handle() As ClResult(Of Boolean) Implements IResultUseCase(Of Boolean).Handle

        Try
            If IsNothing(_command.CrewId) Then
                Return ClResult(Of Boolean).Fail(ClError.Application("L'id de l'equipage est null"))
            End If
            If String.IsNullOrWhiteSpace(_command.Constat) Then
                Return ClResult(Of Boolean).Fail(ClError.Application("Le constat ne peut etre vide"))
            End If

            _repository.InsertLog(_command.CrewId, _command.Constat, DateTime.Now)
            Return ClResult(Of Boolean).Ok(True)

        Catch ex As Exception
            Return ClResult(Of Boolean).Fail(ClError.Application(ex.Message, ex))
        End Try

    End Function

End Class
