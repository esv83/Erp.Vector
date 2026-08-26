''' <summary>
''' Réponse du sondage par lot : un bénéficiaire qui porte une carte, la date de la plus récente, et
''' l'URL à laquelle son image se sert.
''' </summary>
''' <remarks>
''' <b>Chemin relatif</b>, comme partout ailleurs dans ce contrat (<c>ClMutuelleCardDtoOut.ImageUrl</c>,
''' <c>ClFieldSignatureDto.ImageUrl</c>) : c'est à l'appelant de le composer avec la base de l'API
''' Vector, qu'il connaît par sa propre configuration. Servir une URL absolue obligerait Vector à
''' savoir sous quel nom d'hôte on l'atteint — ce qu'il ignore derrière un répertoire virtuel IIS.
''' <para>
''' L'URL vise la route <b>par bénéficiaire</b>, pas par carte : elle reste valable après une nouvelle
''' capture, là où un identifiant de carte deviendrait périmé sans prévenir.
''' </para>
''' </remarks>
Public Class ClMutuelleCardPresenceDtoOut

    Public Property BeneficiaryId As Guid
    Public Property CapturedAt As DateTime
    Public Property ImageUrl As String

End Class
