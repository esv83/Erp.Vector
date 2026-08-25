Imports System.Threading

Namespace Port

    ''' <summary>
    ''' OC-5 — Les attributs de mission servis et enregistrés <b>par Order</b>, à la place du
    ''' catalogue et du magasin Vector (<c>MOB_CONTRACT_ATTRIBUTE*</c>, <c>MOB_JOB_ATTRIBUTE_VALUE</c>).
    '''
    ''' <para>
    ''' <b>Le contrat mobile ne bouge pas</b> (D14) : <c>GET api/FormStructure/{jobId}</c> rend
    ''' toujours la même liste de <see cref="ClMobileAppFieldModel"/> et
    ''' <c>PATCH api/JobEdit/{jobId}</c> reçoit toujours les mêmes couples nom/valeur. Le front ne
    ''' change ni d'URL ni de parsing — seule la source interne bascule.
    ''' </para>
    '''
    ''' <para>
    ''' <b>Rien à corréler côté Vector.</b> Les deux endpoints Order sont indexés par <b>mission</b>,
    ''' pas par type : c'est Order qui résout mission → commande → context effectif → jeu de champs.
    ''' C'est ce qui rend la bascule si petite, et c'est aussi pourquoi elle suppose que le type de la
    ''' mission vient déjà d'Order : sinon l'ambulancier choisirait un type d'un côté et verrait les
    ''' champs d'un autre.
    ''' </para>
    ''' </summary>
    Public Interface IContextOrderAttributeService

        ''' <summary>
        ''' Champs à afficher pour la mission, prêts pour le contrat mobile. Nothing si la mission
        ''' est introuvable côté ERP.
        ''' <para>
        ''' Deux champs additifs remontent en plus de l'existant : <c>IsReadOnly</c> et
        ''' <c>ReadOnlyReason</c>. Ils portent le verrou <b>par champ</b> — une DDN déjà connue de la
        ''' fiche bénéficiaire s'affiche mais ne se saisit plus.
        ''' </para>
        ''' </summary>
        Function GetFormStructureAsync(missionId As Guid, ct As CancellationToken) As Task(Of List(Of ClMobileAppFieldModel))

        ''' <summary>
        ''' Enregistre les valeurs saisies, <b>tout ou rien</b> : une seule valeur invalide fait
        ''' échouer le lot et rien n'est écrit.
        ''' <para>
        ''' L'appelant peut renvoyer le formulaire entier sans trier les champs verrouillés :
        ''' reposer une valeur inchangée est sans effet côté Order, seule une <i>modification</i> de
        ''' champ verrouillé est refusée.
        ''' </para>
        ''' </summary>
        ''' <param name="setBy">Ambulancier à l'origine de la saisie, tracé côté Order. Optionnel.</param>
        Function SaveValuesAsync(missionId As Guid,
                                 values As List(Of ClAttributValueModel),
                                 setBy As String,
                                 ct As CancellationToken) As Task(Of EnContextOrderValuesOutcome)

    End Interface

End Namespace
