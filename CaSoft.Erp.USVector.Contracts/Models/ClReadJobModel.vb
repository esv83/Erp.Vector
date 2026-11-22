Public Class ClReadJobModel
    Public Property IsJob As Boolean
    Public Property JobId As Guid

    ''' <summary>
    ''' Heure du clic « Vu » sur le téléphone, avec son décalage (ISO 8601, ex.
    ''' <c>2026-11-22T10:15:00+01:00</c>). <b>Facultative</b> : absente, le serveur horodate à la
    ''' réception. Sert au « Vu » donné hors réseau et envoyé au retour du réseau (J-172, 22/11).
    ''' Refusée (400) si plus de 5 minutes dans le futur ou plus de 24 heures dans le passé.
    ''' </summary>
    Public Property SeenAt As DateTimeOffset?
End Class
