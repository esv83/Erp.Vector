Public MustInherit Class ClPresenterBase
    Implements IResponseHandler

    Private _response As ClUseCaseResponseBase


    Public Sub Handle(response As ClUseCaseResponseBase) Implements IResponseHandler.Handle
        _response = response
    End Sub

    Protected ReadOnly Property Response
        Get
            Return _response
        End Get
    End Property



End Class
