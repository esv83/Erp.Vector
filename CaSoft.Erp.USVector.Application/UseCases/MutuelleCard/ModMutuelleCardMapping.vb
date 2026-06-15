Imports System.Runtime.CompilerServices

''' <summary>Mapping Application : métier → DTO (carte mutuelle).</summary>
Public Module ModMutuelleCardMapping

    <Extension>
    Public Function ToDtoOut(card As ClMutuelleCard) As ClMutuelleCardDtoOut
        Return New ClMutuelleCardDtoOut With {
            .Id = card.Id,
            .BeneficiaryId = card.BeneficiaryId,
            .ContentType = card.ContentType,
            .ByteSize = card.ByteSize,
            .CapturedAt = card.CapturedAt,
            .ImageUrl = $"api/mutuelle-card/{card.Id}/image",
            .MutuelleName = card.MutuelleName,
            .AmcCode = card.AmcCode,
            .Concentrateur = card.Concentrateur,
            .Teletransmission = card.Teletransmission,
            .OcrStatus = card.OcrStatus
        }
    End Function

End Module
