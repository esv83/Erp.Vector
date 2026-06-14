''' <summary>
''' MOB-13.8 — Enregistre le type de contrat choisi pour la mission. Bascule le jeu
''' d'attributs : le prochain GetFormStructure renverra core + attributs du contrat choisi.
''' </summary>
Public Class ClSelectContractUseCase
    Inherits ClUseCaseBase
    Implements IUseCase

    Private _command As ClSelectContractCommand
    Private _overlay As IJobAttributeOverlay

    Public Sub New(command As ClSelectContractCommand, overlay As IJobAttributeOverlay)
        _command = command
        _overlay = overlay
    End Sub

    Public Overrides Sub execute(presenter As IResponseHandler) Implements IUseCase.Execute
        Try
            _overlay.SelectContract(_command.MissionId, _command.ContractId)
            Response.SetResult(True)
        Catch ex As Exception
            Response.AddError(ex.Message)
        Finally
            presenter.Handle(Response)
        End Try
    End Sub

    Public Overrides Sub Before()
    End Sub
End Class
