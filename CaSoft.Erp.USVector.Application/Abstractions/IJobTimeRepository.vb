Imports System.Threading

Namespace Port


    Public Interface IJobTimeRepository
        Function SaveAsync(gJobId As Guid, timeData As ClJobTimeData, ct As CancellationToken) As Task
        ''' <summary>Les jalons de la mission, ou Nothing si aucun n'a encore été posé.</summary>
        Function GetJobTimeDataAsync(gJobId As Guid, ct As CancellationToken) As Task(Of ClJobTimeData)

    End Interface

End Namespace
