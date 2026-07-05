' Enregistrement d'une signature — Result pattern. (Sans consommateur actif : SignatureController
' écrit via ISignatureRepository directement.)
Public Class ClUpdateSignUseCase
    Implements IResultUseCase(Of Boolean)

    Private ReadOnly _repository As ISignatureRepository
    Private ReadOnly _command As ClUpdateSignCommand

    Public Sub New(command As ClUpdateSignCommand, Repository As ISignatureRepository)
        _command = command
        _repository = Repository
    End Sub

    Public Function Handle() As ClResult(Of Boolean) Implements IResultUseCase(Of Boolean).Handle

        Try
            _repository.Insert(_command.JobId, _command.Data)
            Return ClResult(Of Boolean).Ok(True)
        Catch ex As Exception
            Return ClResult(Of Boolean).Fail(ClError.Application(ex.Message, ex))
        End Try

    End Function

End Class
