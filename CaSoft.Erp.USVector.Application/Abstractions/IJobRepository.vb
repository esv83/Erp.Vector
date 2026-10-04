Namespace Port
    Public Interface IJobRepository

        ''' <summary>La mission assemblée, ou <c>Nothing</c> si elle est inconnue d'Orders.</summary>
        Function GetJobAsync(gJobId As Guid, ct As Threading.CancellationToken) As Task(Of ClJob)
        ''' <summary>Les jalons de la mission — jamais Nothing : vides si aucun n'a été posé.</summary>
        Function GetJobTimeAsync(jobId As Guid, ct As Threading.CancellationToken) As Task(Of ClJobTimeData)
        Function SaveJobTimeAsync(jobTime As ClJobTimeData, ct As Threading.CancellationToken) As Task

    End Interface

End Namespace
