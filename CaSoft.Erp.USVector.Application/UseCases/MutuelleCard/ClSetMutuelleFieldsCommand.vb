''' <summary>P2 — Renseigne les champs mutuelle d'une carte (saisie manuelle, avant OCR).</summary>
Public Class ClSetMutuelleFieldsCommand
    Public Sub New(cardId As Guid, fields As ClMutuelleFieldsDtoIn)
        _CardId = cardId
        _Fields = fields
    End Sub

    Public ReadOnly Property CardId As Guid
    Public ReadOnly Property Fields As ClMutuelleFieldsDtoIn
End Class
