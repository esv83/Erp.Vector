''' <summary>
''' MOB — Réponse <b>décision-complète</b> de <c>GET /api/crew/mine</c>. Le backend tranche toute
''' la logique ; l'UI se contente d'afficher et de renvoyer un CrewId au clic.
'''
''' <para>Flux : au login (et via un bouton « changer d'équipage » en cours de journée) l'app
''' appelle cet endpoint. Si <see cref="RequiresSelection"/> = True, elle <b>impose</b> l'écran de
''' choix (pré-cochant <see cref="RecommendedCrewId"/>) ; sinon elle utilise directement l'unique
''' équipage. Aucun calcul côté web.</para>
''' </summary>
Public Class ClActiveCrewSelectionDtoOut
    ''' <summary>True dès qu'il y a &gt; 1 équipage actif : l'UI DOIT forcer le choix.</summary>
    Public Property RequiresSelection As Boolean
    ''' <summary>Équipage couvrant « maintenant » à pré-sélectionner (ou l'unique équipage). Nothing si aucun.</summary>
    Public Property RecommendedCrewId As Guid?
    ''' <summary>Équipages actifs du personnel aujourd'hui, prêts à afficher.</summary>
    Public Property Crews As IReadOnlyList(Of ClActiveCrewDtoOut)
End Class
