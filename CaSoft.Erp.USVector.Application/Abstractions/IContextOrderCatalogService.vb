Imports System.Threading

Namespace Port

    ''' <summary>
    ''' OC-3b — Sert la liste des types sélectionnables d'une mission <b>depuis le catalogue
    ''' Order</b>, à la place de <c>MOB_CONTRACT_TYPE</c>.
    ''' <para>
    ''' La <b>forme</b> rendue au mobile ne change pas (D14) : c'est toujours un tableau de
    ''' <see cref="ClContractChoiceDto"/> — seule la <b>source</b> change. Le passage du tableau à un
    ''' objet et le renommage de la route attendent que le front ait basculé.
    ''' </para>
    ''' <para>
    ''' ⚠️ Les <c>Id</c> servis deviennent alors ceux du catalogue <b>Order</b>. C'est le cœur de la
    ''' bascule, et sa principale chausse-trappe : un client qui aurait mis en cache l'ancienne liste
    ''' posterait un id Vector là où l'API attend désormais un id Order — l'id <c>4</c> vaut
    ''' <c>ART80</c> ici et <c>CENTRE15</c> là-bas. C'est pourquoi le drapeau ne s'arme qu'une fois
    ''' les ids en dur levés côté front.
    ''' </para>
    ''' </summary>
    Public Interface IContextOrderCatalogService

        ''' <summary>
        ''' Types sélectionnables pour la mission, dans l'ordre voulu par Order, avec le type
        ''' effectif marqué <c>IsSelected</c> et le verrou reporté sur chaque item.
        ''' <para>
        ''' <b>Aucun défaut n'est appliqué</b> : quand la mission n'a pas de type posé, aucun item
        ''' n'est sélectionné — « non renseigné » est un état valide, l'ancienne règle « défaut =
        ''' premier actif » disparaît avec OC-3b.
        ''' </para>
        ''' <para>
        ''' Renvoie une liste <b>vide</b> si l'ERP ne répond pas ou ignore la mission : la liste
        ''' <i>est</i> désormais celle d'Order, il n'y a pas de repli local possible qui ne
        ''' ressusciterait pas la collision d'identifiants. Rien à choisir vaut mieux qu'un choix qui
        ''' partirait de travers en facturation.
        ''' </para>
        ''' </summary>
        Function GetChoicesAsync(missionId As Guid, ct As CancellationToken) As Task(Of List(Of ClContractChoiceDto))

    End Interface

End Namespace
