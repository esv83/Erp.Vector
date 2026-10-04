''' <summary>
''' L'heure qu'il est, dite une fois pour toutes (04/10). Vector écrit <b>deux</b> sortes d'heures, et les
''' modules voisins comptent sur la différence : la changer d'un côté seulement décale l'autre de deux heures.
''' </summary>
''' <remarks>
''' <para>
''' <b>Heure locale — <see cref="MaintenantLocal"/></b>, pour trois horodatages et trois seulement :
''' </para>
''' <list type="bullet">
'''   <item><b>« mission vue »</b> (<c>MST_READ_AT</c>) — Orders la range sans conversion
'''   (<c>ModOperationalTime.AlreadyLocal</c>) dans <c>MOP_READ_AT</c>, que la régulation affiche ;</item>
'''   <item><b>la signature</b> (<c>SIG_DATETIME</c>) — la facturation la reporte telle quelle en C62
'''   (« en LOCAL, ne pas convertir », <c>ModTraductionHoraires</c>) ;</item>
'''   <item><b>la prise de volant</b> envoyée à Orders — comparée par Orders à la fin de vacation, qui est
'''   un horaire local (<c>ClCrew.AssignDriver</c>).</item>
''' </list>
''' <para>
''' <b>UTC — <see cref="MaintenantUtc"/></b>, pour tout le reste : étapes en route / sur place / terminée,
''' documents, anomalies, carte mutuelle, horodatages techniques. Orders et la facturation les convertissent.
''' </para>
''' <para>
''' ⚖️ <b>Mesuré le 04/10 en production</b> : 604 « vues » sur 616 portent exactement +120 min sur l'UTC
''' posé au même instant, et 93 % des missions paraissent « vues » après leur départ — c'est la trace de
''' cette convention, pas une panne. <b>Passer tout en UTC se fait en une publication coordonnée de
''' Vector, Orders et la facturation</b>, avec une date de bascule pour l'historique — jamais d'ici seul.
''' <c>ClHorlogeConventionTests</c> fige la convention pour qu'une « correction » isolée casse la suite
''' plutôt que les données des voisins.
''' </para>
''' </remarks>
Public NotInheritable Class ClHorloge

    Private Sub New()
    End Sub

    ''' <summary>
    ''' Heure locale du serveur — réservée aux trois horodatages de la convention partagée (voir la
    ''' classe). Ne pas l'utiliser ailleurs : tout nouvel horodatage s'écrit en UTC.
    ''' </summary>
    Public Shared Function MaintenantLocal() As DateTime
        Return DateTime.Now
    End Function

    ''' <summary>Heure UTC — le cas général.</summary>
    Public Shared Function MaintenantUtc() As DateTime
        Return DateTime.UtcNow
    End Function

End Class
