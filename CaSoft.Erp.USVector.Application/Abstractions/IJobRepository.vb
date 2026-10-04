Namespace Port
    Public Interface IJobRepository

        ''' <summary>La mission assemblée, ou <c>Nothing</c> si elle est inconnue d'Orders.</summary>
        Function GetJobAsync(gJobId As Guid, ct As Threading.CancellationToken) As Task(Of ClJob)
        Function GetJobTime(jobId As Guid) As ClJobTimeData
        Sub SaveJobTime(jobTime As ClJobTimeData)

    End Interface

End Namespace
