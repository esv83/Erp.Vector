Public MustInherit Class ClUseCaseBase
    Implements IUseCase


    Protected _response As ClUseCaseResponseBase

    Public Sub New()
        _response = New ClUseCaseResponseBase

    End Sub

    Public MustOverride Sub Execute(Handler As IResponseHandler) Implements IUseCase.Execute
    ' Public MustOverride Function CanExecute() As Boolean

    Public MustOverride Sub Before()


    Public ReadOnly Property Response As ClUseCaseResponseBase
        Get
            Return _response
        End Get
    End Property

    Public Function CanExecute() As Boolean
        Before()
        Return Not Response.HasError
    End Function

    Public Function ValidGuid(gGuid As Guid) As Boolean
        Dim result As Boolean = True

        If gGuid = Nothing Then
            result = False
        Else gGuid = Guid.Empty
            result = False
        End If

        Return result

    End Function

    Protected Sub SetResult(data As Object)
        Response.SetResult(data)
    End Sub
    Protected Sub AddError(ex As Exception)
        Response.AddError(ex.Message)
    End Sub

    Protected Sub AddError(strErrorMessage As String)
        Response.AddError(strErrorMessage)
    End Sub
    Public Function IsNull(obj As Object) As Boolean
        Return obj Is Nothing
    End Function

End Class
