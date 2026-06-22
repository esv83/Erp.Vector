Imports System.Runtime.CompilerServices

''' <summary>Mapping Application : métier → DTO (document terrain).</summary>
Public Module ModDocumentMapping

    <Extension>
    Public Function ToDtoOut(document As ClDocument) As ClDocumentDtoOut
        Return New ClDocumentDtoOut With {
            .Id = document.Id,
            .MissionId = document.MissionId,
            .Category = CInt(document.Category),
            .ContentType = document.ContentType,
            .ByteSize = document.ByteSize,
            .FileName = document.FileName,
            .CapturedAt = document.CapturedAt,
            .FileUrl = $"api/documents/{document.Id}/content"
        }
    End Function

End Module
