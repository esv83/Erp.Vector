Imports System.Threading

' Lecture de la signature d'une mission — Result pattern.
Public Class ClGetSignatureUseCase
    Implements IResultUseCaseAsync(Of ClSignatureDtoOut)

    Private ReadOnly _query As Guid
    Private ReadOnly _repository As ISignatureRepository

    Public Sub New(query As Guid, Repository As ISignatureRepository)
        _query = query
        _repository = Repository
    End Sub

    Public Async Function HandleAsync(ct As CancellationToken) As Task(Of ClResult(Of ClSignatureDtoOut)) Implements IResultUseCaseAsync(Of ClSignatureDtoOut).HandleAsync

        If _query = Guid.Empty Then
            Return ClResult(Of ClSignatureDtoOut).Fail(ClError.Application("Identifiant de mission vide."))
        End If

        Dim signature As ClSignatureDtoOut = Await _repository.FetchAsync(_query, ct)
        Return ClResult(Of ClSignatureDtoOut).Ok(signature)

    End Function

End Class
