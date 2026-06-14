Public Interface IUseCaseResponse

    Sub SetResult(data As Object)
    Sub AddError(strErrorText As String)
    Sub AddError(ex As Exception)
    ReadOnly Property HasError() As Boolean
    ReadOnly Property IsSuccess As Boolean
    ReadOnly Property HasResult As Boolean
    ReadOnly Property Data As Object
    ReadOnly Property ErrorText As String


End Interface
