Public Class JobValuePresenterold
    ' Implements IResponsePresenter(Of IUseCaseResponse(Of ClJob), List(Of ClAttributValueDto))

    Private Response As IUseCaseResponse(Of ClJob)

    'Public Sub SetResponse(result As IUseCaseResponse(Of ClJob)) Implements IResponsePresenter(Of IUseCaseResponse(Of ClJob), List(Of ClAttributValueDto)).SetResponse
    '    Response = result
    'End Sub

    'Public Function Present() As List(Of ClAttributValueDto) Implements IResponsePresenter(Of IUseCaseResponse(Of ClJob), List(Of ClAttributValueDto)).Present
    '    Dim result As New List(Of ClAttributValueDto)
    '    For Each attribut In Response.Result.ContractType.Attributs
    '        result.Add(New ClAttributValueDto(attribut.Key, attribut.Value.Type, attribut.Value.Value))
    '    Next

    '    Return result

    'End Function
End Class
