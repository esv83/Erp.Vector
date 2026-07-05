''' <summary>
''' MOB-13.8 — Enregistre le type de contrat choisi pour la mission. Bascule le jeu
''' d'attributs : le prochain GetFormStructure renverra core + attributs du contrat choisi. Result pattern.
''' </summary>
Public Class ClSelectContractUseCase
    Implements IResultUseCase(Of Boolean)

    Private ReadOnly _command As ClSelectContractCommand
    Private ReadOnly _overlay As IJobAttributeOverlay

    Public Sub New(command As ClSelectContractCommand, overlay As IJobAttributeOverlay)
        _command = command
        _overlay = overlay
    End Sub

    Public Function Handle() As ClResult(Of Boolean) Implements IResultUseCase(Of Boolean).Handle
        Try
            _overlay.SelectContract(_command.MissionId, _command.ContractId)
            Return ClResult(Of Boolean).Ok(True)
        Catch ex As Exception
            Return ClResult(Of Boolean).Fail(ClError.Application(ex.Message, ex))
        End Try
    End Function

End Class
