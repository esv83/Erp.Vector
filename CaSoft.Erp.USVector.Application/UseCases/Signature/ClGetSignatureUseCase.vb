
' Lecture de la signature d'une mission — Result pattern.
Public Class ClGetSignatureUseCase
    Implements IResultUseCase(Of ClSignatureDtoOut)

    Private ReadOnly _query As Guid
    Private ReadOnly _repository As ISignatureRepository

    Public Sub New(query As Guid, Repository As ISignatureRepository)
        _query = query
        _repository = Repository
    End Sub

    Public Function Handle() As ClResult(Of ClSignatureDtoOut) Implements IResultUseCase(Of ClSignatureDtoOut).Handle

        If _query = Guid.Empty Then
            Return ClResult(Of ClSignatureDtoOut).Fail(ClError.Application("Identifiant de mission vide."))
        End If

        Dim signature As ClSignatureDtoOut = _repository.Fetch(_query)
        Return ClResult(Of ClSignatureDtoOut).Ok(signature)

    End Function

End Class
