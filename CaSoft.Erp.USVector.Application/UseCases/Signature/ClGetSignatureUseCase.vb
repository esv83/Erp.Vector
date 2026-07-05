
' Lecture de la signature d'une mission — Result pattern.
Public Class ClGetSignatureUseCase
    Implements IResultUseCase(Of ClSignatureDto)

    Private ReadOnly _query As Guid
    Private ReadOnly _repository As ISignatureRepository

    Public Sub New(query As Guid, Repository As ISignatureRepository)
        _query = query
        _repository = Repository
    End Sub

    Public Function Handle() As ClResult(Of ClSignatureDto) Implements IResultUseCase(Of ClSignatureDto).Handle

        Try
            Dim SignGuid As New ClValidGuid(_query)
            Dim signature As ClSignatureDto = _repository.Fetch(SignGuid.Value)
            Return ClResult(Of ClSignatureDto).Ok(signature)
        Catch ex As Exception
            Return ClResult(Of ClSignatureDto).Fail(ClError.Application(ex.Message, ex))
        End Try

    End Function

End Class
