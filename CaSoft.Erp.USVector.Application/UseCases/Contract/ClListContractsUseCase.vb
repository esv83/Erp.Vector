''' <summary>
''' MOB-13.8 — Liste des types de contrat sélectionnables pour une mission, en marquant
''' le contrat effectif (choisi explicitement, sinon premier actif = défaut de BuildContractType). Result pattern.
''' </summary>
Public Class ClListContractsUseCase
    Implements IResultUseCase(Of List(Of ClContractChoiceDto))

    Private ReadOnly _missionId As Guid
    Private ReadOnly _overlay As IJobAttributeOverlay
    Private ReadOnly _locked As Boolean

    ''' <param name="locked">
    ''' OC-3a — Verrou du context côté ERP, lu par l'appelant via
    ''' <c>IContextOrderStateQueryService</c>. Reporté tel quel sur chaque item : le use case ne
    ''' fait pas d'appel réseau, il compose la vue mobile à partir du catalogue overlay et de cet
    ''' état. Faux tant que l'état n'est pas connu — le contrat mobile reste alors identique à
    ''' l'existant.
    ''' </param>
    Public Sub New(missionId As Guid, overlay As IJobAttributeOverlay, Optional locked As Boolean = False)
        _missionId = missionId
        _overlay = overlay
        _locked = locked
    End Sub

    Public Function Handle() As ClResult(Of List(Of ClContractChoiceDto)) Implements IResultUseCase(Of List(Of ClContractChoiceDto)).Handle
        Try
            Dim contracts = _overlay.GetContracts()

            Dim selectedId As Integer? = _overlay.GetSelectedContractId(_missionId)
            If Not selectedId.HasValue AndAlso contracts.Count > 0 Then
                selectedId = contracts(0).Id
            End If

            Dim list = contracts.Select(Function(c) New ClContractChoiceDto With {
                .Id = c.Id,
                .Display = c.Display,
                .IsSelected = selectedId.HasValue AndAlso c.Id = selectedId.Value,
                .Locked = _locked
            }).ToList()

            Return ClResult(Of List(Of ClContractChoiceDto)).Ok(list)
        Catch ex As Exception
            Return ClResult(Of List(Of ClContractChoiceDto)).Fail(ClError.Application(ex.Message, ex))
        End Try
    End Function

End Class
