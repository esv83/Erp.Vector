Imports System.Threading

' Mise à jour des jalons opérationnels (En route / Sur place / Terminé) — Result pattern.
' Cumulatif : ne pose QUE les jalons fournis (non-null) ; les autres conservent leur valeur.
Public Class ClUpdateTimeUseCase
    Implements IResultUseCaseAsync(Of Boolean)

    Private ReadOnly _repository As IJobRepository
    Private ReadOnly _command As ClJobTimeCommand

    Public Sub New(command As ClJobTimeCommand, repository As IJobRepository)
        _command = command
        _repository = repository
    End Sub

    Public Async Function HandleAsync(ct As CancellationToken) As Task(Of ClResult(Of Boolean)) Implements IResultUseCaseAsync(Of Boolean).HandleAsync

        Dim jobTime As ClJobTimeData = Await _repository.GetJobTimeAsync(_command.JobId, ct)

        ' Cumulatif : on ne pose QUE les jalons réellement fournis (non-null) ; les autres
        ' conservent leur valeur existante. Le client peut ainsi n'envoyer que le jalon franchi
        ' sans effacer les précédents (aligné sur la projection ERP cumulative).
        Dim goTime = New ClTimeFormatAdapter(_command.JobTime.GoTime).ToDateTime
        If goTime.HasValue Then jobTime.GoTime = goTime

        Dim onSiteTime = New ClTimeFormatAdapter(_command.JobTime.OnSiteTime).ToDateTime
        If onSiteTime.HasValue Then jobTime.OnSiteTime = onSiteTime

        Dim terminateTime = New ClTimeFormatAdapter(_command.JobTime.TerminatedTime).ToDateTime
        If terminateTime.HasValue Then jobTime.TerminateTime = terminateTime

        Await _repository.SaveJobTimeAsync(jobTime, ct)
        Return ClResult(Of Boolean).Ok(True)

    End Function

End Class
