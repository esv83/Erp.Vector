Public Interface IJobService

    Sub ReadJob(gJobId As Guid, handler As IResponseHandler)
    Sub GetJobEditFormStructure(gJobId As Guid, Handler As IResponseHandler)
    Sub GetJobTime(gJobId As Guid, Handler As IResponseHandler)
    Sub SetJobTime(gJobId As Guid, jobTime As ClJobTimeModel, handler As IResponseHandler)
    Function GetJobDetail(gJobId As Guid) As Object
    Function GetJobValue(gJobId As Guid) As Object
    Function UpdateAttributValues(gJobId As Guid, values As List(Of ClAttributValueModel)) As Boolean

End Interface
