Imports System.Threading

Namespace Port

    ''' <summary>
    ''' OC-4 — Enregistre le context choisi par l'ambulancier <b>dans l'ERP</b>, là où l'ancien
    ''' chemin écrivait dans <c>MOB_JOB_CONTRACT</c> (magasin Vector, doublon du référentiel Order).
    ''' <para>
    ''' Le paramètre reste l'identifiant du <b>catalogue Vector</b> tel que le mobile l'a reçu de
    ''' <c>GET api/Contract/{jobId}</c> : le contrat mobile ne change pas (D14). La correspondance
    ''' vers le catalogue Order est faite par l'implémentation.
    ''' </para>
    ''' </summary>
    Public Interface IContextOrderSelectionService

        ''' <param name="contractTypeId">Identifiant du catalogue <b>Vector</b> (MOB_CONTRACT_TYPE).</param>
        ''' <param name="setBy">Ambulancier/équipage, tracé côté Order. Optionnel.</param>
        Function SelectAsync(missionId As Guid,
                             contractTypeId As Integer,
                             setBy As String,
                             ct As CancellationToken) As Task(Of EnContextOrderSelectionOutcome)

    End Interface

End Namespace
