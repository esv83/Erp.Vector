Public Class ClDefaultPresenter
    Inherits ClPresenterBase

    Public Overloads ReadOnly Property Response As ClUseCaseResponseBase
        Get
            Return MyBase.response
        End Get
    End Property

End Class

Public Class ClDefaultPresenter(Of T)
    Implements IResponseHandler

    Private _response As ClUseCaseResponseBase
    Public ReadOnly Property Result As T
        Get
            Return _response.Data
        End Get
    End Property
    Public ReadOnly Property IsSucces As Boolean
        Get
            Return _response.IsSuccess
        End Get
    End Property

    Public ReadOnly Property ErrorText As String
        Get
            Return _response.ErrorText
        End Get
    End Property


    Public Sub Handle(response As ClUseCaseResponseBase) Implements IResponseHandler.Handle
        _response = response

    End Sub
End Class
