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

End Class
