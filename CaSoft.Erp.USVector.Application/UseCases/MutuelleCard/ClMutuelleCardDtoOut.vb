''' <summary>P1 — Métadonnées de la carte mutuelle courante (sans le binaire).</summary>
Public Class ClMutuelleCardDtoOut
    Public Property Id As Guid
    Public Property BeneficiaryId As Guid
    Public Property ContentType As String
    Public Property ByteSize As Integer
    Public Property CapturedAt As DateTime
    ''' <summary>Chemin relatif de l'image (à composer avec la base de l'API mobile).</summary>
    Public Property ImageUrl As String

    ' Champs extraits (P3) — Nothing tant que non renseignés.
    Public Property MutuelleName As String
    Public Property AmcCode As String
    Public Property Concentrateur As String
    Public Property Teletransmission As String
    Public Property OcrStatus As String

    ''' <summary>
    ''' P3 — Ce que la lecture automatique PROPOSE, à côté des champs officiels ci-dessus. Nothing
    ''' tant qu'aucune lecture n'a abouti. L'écran de validation affiche ces valeurs à côté de la
    ''' saisie ; les valider les recopie dans les champs officiels (M5 : jamais d'écriture aveugle).
    ''' </summary>
    Public Property OcrProposal As ClMutuelleCardOcrProposalDtoOut
End Class

''' <summary>P3 — Proposition de la lecture automatique, telle que l'écran de validation la lit.</summary>
Public Class ClMutuelleCardOcrProposalDtoOut
    Public Property MutuelleName As String
    Public Property AmcCode As String
    Public Property Concentrateur As String
    Public Property Teletransmission As String

    ''' <summary>Confiance du modèle, de 0 à 1 — à afficher : on ne valide pas 0,4 comme 0,95.</summary>
    Public Property Confidence As Decimal?

    Public Property ExtractedAt As DateTime?
End Class
