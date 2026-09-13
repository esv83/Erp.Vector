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

    End Interface

End Namespace
