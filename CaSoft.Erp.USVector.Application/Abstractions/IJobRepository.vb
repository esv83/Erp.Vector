Namespace Port
    Public Interface IJobRepository

        Function GetJob(gJobId As Guid) As ClJob
        Sub Save(Job As ClJob)
        Function IsExist(jobId As Guid) As Boolean
        Function GetJobTime(jobId As Guid) As ClJobTimeData
        Sub SaveJobTime(jobTime As ClJobTimeData)

    End Interface

End Namespace
