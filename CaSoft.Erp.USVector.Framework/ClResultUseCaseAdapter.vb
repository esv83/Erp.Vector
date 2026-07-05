''' <summary>
''' Pont de migration : expose un use case <see cref="IResultUseCase(Of T)"/> via l'ancien contrat
''' <see cref="IUseCase"/> (<c>Execute(presenter)</c>). Permet de brancher un use case déjà migré au
''' Result pattern dans la plomberie legacy (ClUseCaseHandler côté contrôleur, ou services appelant
''' <c>execute(presenter)</c>) <b>sans toucher aux consommateurs</b>.
'''
''' Traduction alignée sur ClWebApiPresenter (parité stricte) :
'''  • succès + valeur → SetResult(valeur)      (200)
'''  • succès sans valeur / erreur NotFound → SetResult(Nothing)  (404, comme Data nul)
'''  • autre erreur → AddError(message)          (400)
''' Les exceptions sont capturées et traduites en erreur (comme les use cases legacy).
''' </summary>
Public Class ClResultUseCaseAdapter(Of T)
    Implements IUseCase

    Private ReadOnly _inner As IResultUseCase(Of T)

    Public Sub New(inner As IResultUseCase(Of T))
        _inner = inner
    End Sub

    Public Sub Execute(presenter As IResponseHandler) Implements IUseCase.Execute
        Dim response As New ClUseCaseResponseBase()

        Try
            Dim result = _inner.Handle()

            If result.IsSucces Then
                response.SetResult(result.Value)
            ElseIf IsNotFound(result.InnerError) Then
                response.SetResult(Nothing)                  ' Data nul → 404 (parité legacy)
            Else
                response.AddError(If(result.InnerError?.ErrorText, "Erreur inconnue."))
            End If

        Catch ex As Exception
            response.AddError(ex.Message)
        End Try

        presenter.Handle(response)
    End Sub

    Private Shared Function IsNotFound([error] As IError) As Boolean
        Dim asClError = TryCast([error], ClError)
        Return asClError IsNot Nothing AndAlso asClError.IsNotFound
    End Function

End Class
