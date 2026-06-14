
Public Class ClGetSignatureUseCase
    Inherits ClUseCaseBase
    Implements IUseCase

    Private _query As Guid
    Private _repository As ISignatureRepository

    Public Sub New(query As Guid, Repository As ISignatureRepository)
        _query = query

        _repository = Repository
    End Sub

    Public Overrides sub execute(presenter As IResponseHandler) Implements IUseCase.Execute

        Try
            Dim SignGuid As New ClValidGuid(_query)
            Dim signature As ClSignatureDto = _repository.Fetch(SignGuid.Value)
            Response.SetResult(signature)

        Catch ex As Exception
            Response.AddError(ex.Message)

        Finally
            presenter.Handle(Response)

        End Try




    End Sub

    Public Overrides Sub Before()
    End Sub

End Class
