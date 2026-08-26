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

    ''' <summary>Présence → DTO, avec l'URL de l'image par bénéficiaire.</summary>
    <Extension>
    Public Function ToDtoOut(presence As ClMutuelleCardPresence) As ClMutuelleCardPresenceDtoOut
        Return New ClMutuelleCardPresenceDtoOut With {
            .BeneficiaryId = presence.BeneficiaryId,
            .CapturedAt = presence.CapturedAt,
            .ImageUrl = $"api/beneficiaries/{presence.BeneficiaryId}/mutuelle-card/image"
        }
    End Function

End Module
