''' <summary>
''' Octets d'une carte mutuelle et leur type MIME — le strict nécessaire pour les servir.
''' </summary>
''' <remarks>
''' Type dédié plutôt que <c>ClMutuelleCard</c> : il rend <b>impossible</b> de charger le binaire en
''' croyant lire des métadonnées, et l'inverse. La séparation est la seule garantie qui survive à une
''' relecture distraite — un commentaire « ne pas utiliser ce champ ici » n'en est pas une.
''' </remarks>
Public Class ClMutuelleCardImage

    Public Property Bytes As Byte()
    Public Property ContentType As String

End Class
