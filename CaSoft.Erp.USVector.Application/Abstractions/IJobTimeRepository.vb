Namespace Port


    Public Interface IJobTimeRepository
        Sub Save(gJobId As Guid, timeData As ClJobTimeData)
        Function GetJobTimeData(gJobId As Guid) As ClJobTimeData

    End Interface

End Namespace
