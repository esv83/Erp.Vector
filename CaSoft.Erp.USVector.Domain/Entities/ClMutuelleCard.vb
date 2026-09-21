''' <summary>
''' Photo de carte mutuelle d'un bénéficiaire (P1). Conteneur de données : l'image + sa
''' traçabilité de capture, et les champs extraits (OCR/IA, P3) renseignés ultérieurement.
''' </summary>
Public Class ClMutuelleCard

    Public Property Id As Guid
    Public Property BeneficiaryId As Guid

    Public Property Image As Byte()
    Public Property ContentType As String
    Public Property ByteSize As Integer

    Public Property CapturedAt As DateTime
    Public Property CapturedCrewId As Guid?
    Public Property MissionId As Guid?

    ' ── Champs extraits (OCR/IA — P3), Nothing tant que non extrait/validé ──
    Public Property MutuelleName As String
    Public Property AmcCode As String
    Public Property Concentrateur As String
    Public Property Teletransmission As String
    Public Property OcrStatus As String
    Public Property OcrValidatedAt As DateTime?

    ' ── Ce que la lecture automatique PROPOSE (P3) — jamais ce qu'elle impose (M5) ──────
    ' Ces quatre-là vivent à côté des officiels ci-dessus : c'est la validation humaine qui
    ' recopie. Les confondre reviendrait à écrire en facturation ce qu'un modèle a cru lire.
    Public Property OcrMutuelleName As String
    Public Property OcrAmcCode As String
    Public Property OcrConcentrateur As String
    Public Property OcrTeletransmission As String
    Public Property OcrConfidence As Decimal?
    Public Property OcrExtractedAt As DateTime?
    Public Property OcrAttempts As Integer
    Public Property OcrLastError As String

End Class
