Imports System.Threading

Public Interface IJobService

    ''' <summary>« Mission vue » : marque la mission reçue/vue (pose MST_READ_AT + projette MissionSeen à la régulation).</summary>
    Function MarkMissionSeenAsync(gJobId As Guid, ct As CancellationToken) As Task(Of ClResult(Of Boolean))
    Function GetJobTimeAsync(gJobId As Guid, ct As CancellationToken) As Task(Of ClResult(Of ClJobTimeModel))
    ''' <summary>Timeline ordonnée + labellisée des jalons (Option A — contrat riche pour l'UI).</summary>
    Function GetJobTimelineAsync(gJobId As Guid, ct As CancellationToken) As Task(Of ClResult(Of ClJobTimelineDtoOut))
    Function SetJobTimeAsync(gJobId As Guid, jobTime As ClJobTimeModel, ct As CancellationToken) As Task(Of ClResult(Of Boolean))
    ''' <summary>Retour arrière : efface un jalon (seen | go | onsite | terminate).</summary>
    Function ClearJobTimeAsync(gJobId As Guid, jalon As String, ct As CancellationToken) As Task(Of ClResult(Of Boolean))

End Interface
