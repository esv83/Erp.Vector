Imports System.Threading

Namespace Port

    ''' <summary>
    ''' Carte mutuelle — patient d'une mission, résolu côté ERP (mission → commande → bénéficiaire).
    ''' <para>
    ''' Le terrain connaît la mission, pas l'identifiant du patient : aucun DTO mobile ne le porte.
    ''' Sans cette résolution serveur, la capture par bénéficiaire lui était inatteignable.
    ''' </para>
    ''' </summary>
    Public Interface IMissionBeneficiaryQueryService

        ''' <summary>
        ''' Bénéficiaire de la mission, ou Nothing si la mission est introuvable ou sans patient rattaché.
        ''' </summary>
        Function GetBeneficiaryIdAsync(missionId As Guid, ct As CancellationToken) As Task(Of Guid?)

        ''' <summary>
        ''' Bénéficiaires de plusieurs missions, en <b>un</b> appel (au plus 200 missions). Une mission
        ''' inconnue d'Orders est <b>absente</b> du résultat ; une mission connue sans patient y figure
        ''' avec <c>Nothing</c>.
        ''' </summary>
        Function GetBeneficiaryIdsAsync(missionIds As IReadOnlyCollection(Of Guid), ct As CancellationToken) As Task(Of IReadOnlyDictionary(Of Guid, Guid?))

    End Interface

End Namespace
