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
            .OcrStatus = card.OcrStatus,
            .OcrProposal = ToProposalDtoOut(card)
        }
    End Function

    ''' <summary>
    ''' La proposition, ou Nothing quand il n'y en a pas — un bloc vide se lirait comme « le modèle a
    ''' lu, et n'a rien trouvé », ce qui n'est pas la même chose que « personne n'a encore lu ».
    ''' </summary>
    Private Function ToProposalDtoOut(card As ClMutuelleCard) As ClMutuelleCardOcrProposalDtoOut
        If String.IsNullOrWhiteSpace(card.OcrMutuelleName) AndAlso
           String.IsNullOrWhiteSpace(card.OcrAmcCode) AndAlso
           String.IsNullOrWhiteSpace(card.OcrConcentrateur) AndAlso
           String.IsNullOrWhiteSpace(card.OcrTeletransmission) Then
            Return Nothing
        End If

        Return New ClMutuelleCardOcrProposalDtoOut With {
            .MutuelleName = card.OcrMutuelleName,
            .AmcCode = card.OcrAmcCode,
            .Concentrateur = card.OcrConcentrateur,
            .Teletransmission = card.OcrTeletransmission,
            .Confidence = card.OcrConfidence,
            .ExtractedAt = card.OcrExtractedAt
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
