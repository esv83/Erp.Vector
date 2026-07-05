
' Mise à jour des jalons opérationnels (En route / Sur place / Terminé) — Result pattern.
' Cumulatif : ne pose QUE les jalons fournis (non-null) ; les autres conservent leur valeur.
Public Class ClUpdateTimeUseCase
    Implements IResultUseCase(Of Boolean)

    Private ReadOnly _repository As IJobRepository
    Private ReadOnly _command As ClJobTimeCommand

    Public Sub New(command As ClJobTimeCommand, repository As IJobRepository)
        _command = command
        _repository = repository
    End Sub

    Public Function Handle() As ClResult(Of Boolean) Implements IResultUseCase(Of Boolean).Handle

        Try
            Dim jobTime As ClJobTimeData = _repository.GetJobTime(_command.JobId)

            ' Cumulatif : on ne pose QUE les jalons réellement fournis (non-null) ; les autres
            ' conservent leur valeur existante. Le client peut ainsi n'envoyer que le jalon franchi
            ' sans effacer les précédents (aligné sur la projection ERP cumulative).
            Dim goTime = New ClTimeFormatAdapter(_command.JobTime.GoTime).ToDateTime
            If goTime.HasValue Then jobTime.GoTime = goTime

            Dim onSiteTime = New ClTimeFormatAdapter(_command.JobTime.OnSiteTime).ToDateTime
            If onSiteTime.HasValue Then jobTime.OnSiteTime = onSiteTime

            Dim terminateTime = New ClTimeFormatAdapter(_command.JobTime.TerminatedTime).ToDateTime
            If terminateTime.HasValue Then jobTime.TerminateTime = terminateTime

            _repository.SaveJobTime(jobTime)
            Return ClResult(Of Boolean).Ok(True)

        Catch ex As Exception
            Return ClResult(Of Boolean).Fail(ClError.Application(ex.Message, ex))
        End Try

    End Function

End Class
