Public Interface IJobService

    ''' <summary>« Mission vue » : marque la mission reçue/vue (pose MST_READ_AT + projette MissionSeen à la régulation).</summary>
    Function MarkMissionSeen(gJobId As Guid) As ClResult(Of Boolean)
    Function GetJobTime(gJobId As Guid) As ClResult(Of ClJobTimeModel)
    Function SetJobTime(gJobId As Guid, jobTime As ClJobTimeModel) As ClResult(Of Boolean)
    ''' <summary>Retour arrière : efface un jalon (seen | go | onsite | terminate).</summary>
    Function ClearJobTime(gJobId As Guid, jalon As String) As ClResult(Of Boolean)

End Interface
