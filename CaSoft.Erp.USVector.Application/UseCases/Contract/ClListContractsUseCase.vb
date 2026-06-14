''' <summary>
''' MOB-13.8 — Liste des types de contrat sélectionnables pour une mission, en marquant
''' le contrat effectif (choisi explicitement, sinon premier actif = défaut de BuildContractType).
''' </summary>
Public Class ClListContractsUseCase
    Inherits ClUseCaseBase
    Implements IUseCase

    Private _missionId As Guid
    Private _overlay As IJobAttributeOverlay

    Public Sub New(missionId As Guid, overlay As IJobAttributeOverlay)
        _missionId = missionId
        _overlay = overlay
    End Sub

    Public Overrides Sub execute(presenter As IResponseHandler) Implements IUseCase.Execute
        Try
            Dim contracts = _overlay.GetContracts()

            Dim selectedId As Integer? = _overlay.GetSelectedContractId(_missionId)
            If Not selectedId.HasValue AndAlso contracts.Count > 0 Then
                selectedId = contracts(0).Id
            End If

            Dim list = contracts.Select(Function(c) New ClContractChoiceDto With {
                .Id = c.Id,
                .Display = c.Display,
                .IsSelected = selectedId.HasValue AndAlso c.Id = selectedId.Value
            }).ToList()

            Response.SetResult(list)
        Catch ex As Exception
            Response.AddError(ex.Message)
        Finally
            presenter.Handle(Response)
        End Try
    End Sub

    Public Overrides Sub Before()
    End Sub
End Class
