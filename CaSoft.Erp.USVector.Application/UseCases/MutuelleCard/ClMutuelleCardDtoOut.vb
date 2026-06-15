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
End Class
