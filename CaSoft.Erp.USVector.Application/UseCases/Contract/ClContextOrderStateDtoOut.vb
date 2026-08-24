''' <summary>
''' OC-3a — État du <b>context de la mission</b> tel que l'ERP le connaît, servi par
''' <c>GET api/Contract/{jobId}/state</c>. Ajout <b>additif</b> au contrat mobile (D14) : aucune
''' route existante n'est touchée, le front l'adopte à son rythme.
''' <para>
''' Deux informations distinctes, à ne pas confondre : <see cref="Locked"/> dit si l'ambulancier a
''' encore la main, <see cref="Origin"/> dit d'où vient la valeur affichée. C'est la combinaison
''' « posé par la régulation mais modifiable » (<c>Origin = "Regulator"</c>, <c>Locked = False</c>)
''' qui permet à l'UI d'écrire « proposé par la régulation » sans griser le sélecteur.
''' </para>
''' </summary>
Public Class ClContextOrderStateDtoOut

    ''' <summary>Mission concernée (identique au <c>jobId</c> de la route).</summary>
    Public Property MissionId As Guid

    ''' <summary>
    ''' Vrai si le context est gelé côté ERP : toute tentative d'écriture terrain sera refusée (409).
    ''' L'UI grise le sélecteur.
    ''' </summary>
    Public Property Locked As Boolean

    ''' <summary>
    ''' ⚠️ Identifiant du catalogue <b>Order</b> (<c>ORD_ORDER_CONTEXT</c>), <b>pas</b> celui des items
    ''' de <c>GET api/Contract/{jobId}</c>, qui viennent encore de <c>MOB_CONTRACT_TYPE</c> tant
    ''' qu'OC-3b n'est pas livré. Les deux espaces d'identifiants ne coïncident pas — l'id <c>4</c>
    ''' vaut <c>ART80</c> côté Vector et <c>CENTRE15</c> côté Order. <b>Ne pas s'en servir pour
    ''' pré-sélectionner un item de la liste</b> : utiliser <see cref="ContextOrderDisplay"/> pour
    ''' l'affichage. Nothing si aucun context n'est posé.
    ''' </summary>
    Public Property ContextOrderId As Integer?

    ''' <summary>Code technique du context côté Order (ex. <c>CENTRE15</c>). Nothing si aucun.</summary>
    Public Property ContextOrderCode As String

    ''' <summary>Libellé affichable du context côté Order (ex. « Centre 15 »). Nothing si aucun.</summary>
    Public Property ContextOrderDisplay As String

    ''' <summary>
    ''' Provenance de la valeur : <c>"Regulator"</c> (posée par la régulation), <c>"Field"</c>
    ''' (choisie par le terrain), ou Nothing si rien n'est posé.
    ''' </summary>
    Public Property Origin As String

End Class
