''' <summary>
''' Motif « le patient ne peut pas signer » de la liste tenue en base (MOB_012). Un motif retiré
''' (<see cref="IsActive"/> faux) ne se choisit plus, mais ceux déjà déclarés restent lisibles.
''' </summary>
Public Class ClSignatureWaiverReason
    Public Property Code As String
    Public Property Label As String
    ''' <summary>Le motif exige un commentaire (« Autre »).</summary>
    Public Property IsCommentRequired As Boolean
    Public Property IsActive As Boolean
End Class

''' <summary>
''' Déclaration, par l'équipage, que le patient d'une mission ne pouvait pas signer — au plus une par
''' mission. Projetée vers Order (PRJ-2) pour l'alerte signature, qui ne compte pas la mission en manque.
''' </summary>
Public Class ClSignatureWaiver
    Public Property MissionId As Guid
    Public Property ReasonCode As String
    Public Property Comment As String
    ''' <summary>UTC — une donnée neuve naît en UTC (la signature, elle, reste en heure locale : cf. ClHorloge).</summary>
    Public Property DeclaredAt As DateTime
    Public Property DeclaredCrewId As Guid?
End Class
