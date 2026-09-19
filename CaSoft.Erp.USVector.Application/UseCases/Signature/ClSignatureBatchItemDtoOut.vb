''' <summary>
''' B5 — Une mission d'un lot de signatures (19/09, demande de la facturation : 144 à 217 images par
''' journée, ~42 Ko chacune, jusqu'ici une requête par mission).
''' </summary>
''' <remarks>
''' <para>
''' <b>D8 ne bouge pas</b> : les octets restent chez Vector, la facturation les tire. Seule change la
''' granularité. <see cref="Data"/> est la valeur exacte que sert <c>GET api/Signature/{id}</c> — la
''' facturation la stocke telle quelle, en base64.
''' </para>
''' <para>
''' Une entrée par mission demandée : <c>Found</c>, ou <c>NotFound</c> (aucune signature). Une panne
''' fait échouer l'appel entier — une seule requête en base, il n'y a pas d'échec partiel à rendre.
''' </para>
''' </remarks>
Public Class ClSignatureBatchItemDtoOut

    Public Const StatusFound As String = "Found"
    Public Const StatusNotFound As String = "NotFound"

    Public Property MissionId As Guid

    ''' <summary><c>Found</c> : signature présente · <c>NotFound</c> : la mission n'est pas signée.</summary>
    Public Property Status As String

    ''' <summary>Horodatage de la signature. Nothing sauf si Found.</summary>
    Public Property SignedAt As DateTime?

    ''' <summary>L'image, telle que la sert la route unitaire. Nothing sauf si Found.</summary>
    Public Property Data As String

End Class
