
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

        Try
            Dim SignGuid As New ClValidGuid(_query)
            Dim signature As ClSignatureDtoOut = _repository.Fetch(SignGuid.Value)
            Return ClResult(Of ClSignatureDtoOut).Ok(signature)
        Catch ex As Exception
            Return ClResult(Of ClSignatureDtoOut).Fail(ClError.Application(ex.Message, ex))
        End Try

    End Function

End Class
