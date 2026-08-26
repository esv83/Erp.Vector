''' <summary>
''' Présence d'une carte mutuelle pour un bénéficiaire, sans rien de ce qu'elle contient.
''' </summary>
''' <remarks>
''' <see cref="CapturedAt"/> accompagne la présence parce qu'il est gratuit — il sert déjà à
''' désigner la carte courante — et qu'il évite un second appel pour afficher « photo du 26/08 ».
''' Rien d'autre ne remonte : ni nom de mutuelle, ni code AMC. Cette réponse est servie sans jeton,
''' et ce qu'elle divulgue doit se limiter à « ce bénéficiaire a une photo ».
''' </remarks>
Public Class ClMutuelleCardPresence

    Public Property BeneficiaryId As Guid
    Public Property CapturedAt As DateTime

End Class
