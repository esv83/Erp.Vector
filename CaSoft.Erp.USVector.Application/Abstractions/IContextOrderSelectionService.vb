Imports System.Threading

Namespace Port

    ''' <summary>
    ''' OC-4 — Enregistre le context choisi par l'ambulancier <b>dans l'ERP</b>, là où l'ancien
    ''' chemin écrivait dans <c>MOB_JOB_CONTRACT</c> (magasin Vector, doublon du référentiel Order).
    ''' <para>
    ''' Le paramètre est l'identifiant <b>tel que le mobile l'a reçu de
    ''' <c>GET api/Contract/{jobId}</c></b> : le contrat mobile ne change pas (D14). Quel catalogue
    ''' il désigne dépend de l'état de la bascule OC-3b — celui de <b>Vector</b> tant qu'elle n'est
    ''' pas armée, celui d'<b>Order</b> ensuite. L'implémentation traduit dans le premier cas et
    ''' vérifie l'appartenance dans le second ; l'appelant relaie ce qu'il a servi, sans avoir à
    ''' savoir lequel des deux c'est.
    ''' </para>
    ''' </summary>
    Public Interface IContextOrderSelectionService

        ''' <param name="contractTypeId">
        ''' Identifiant servi par la liste : catalogue <b>Vector</b> (<c>MOB_CONTRACT_TYPE</c>) avant
        ''' la bascule, catalogue <b>Order</b> (<c>ORD_ORDER_CONTEXT</c>) après.
        ''' </param>
        ''' <param name="setBy">Ambulancier/équipage, tracé côté Order. Optionnel.</param>
        Function SelectAsync(missionId As Guid,
                             contractTypeId As Integer,
                             setBy As String,
                             ct As CancellationToken) As Task(Of EnContextOrderSelectionOutcome)

    End Interface

End Namespace
