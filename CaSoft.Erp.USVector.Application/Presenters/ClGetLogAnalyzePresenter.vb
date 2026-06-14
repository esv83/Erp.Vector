Public Class ClGetLogAnalyzePresenter
    Implements IResponseHandler

    Public Sub Present(response As ClUseCaseResponseBase) Implements IResponseHandler.Handle
        Dim data = CType(response.Data, List(Of ClLogEntry))

        For Each log In data

        Next


    End Sub


End Class
