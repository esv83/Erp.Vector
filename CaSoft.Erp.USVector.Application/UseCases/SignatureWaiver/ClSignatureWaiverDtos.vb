''' <summary>Corps de <c>PUT api/missions/{id}/signature-waiver</c> : le motif choisi par l'équipage.</summary>
Public Class ClDeclareSignatureWaiverDtoIn
    ''' <summary>Code du motif (<c>GET api/signature-waiver-reasons</c>).</summary>
    Public Property ReasonCode As String
    ''' <summary>Obligatoire si le motif l'exige (« Autre »).</summary>
    Public Property Comment As String
    ''' <summary>Équipage déclarant (traçabilité). Optionnel.</summary>
    Public Property CrewId As Guid?
End Class

''' <summary>Un motif de la liste, pour l'écran de l'équipage.</summary>
Public Class ClSignatureWaiverReasonDtoOut
    Public Property Code As String
    Public Property Label As String
    Public Property IsCommentRequired As Boolean
End Class

''' <summary>Le motif déclaré pour une mission.</summary>
Public Class ClSignatureWaiverDtoOut
    Public Property MissionId As Guid
    Public Property ReasonCode As String
    Public Property ReasonLabel As String
    Public Property Comment As String
    ''' <summary>UTC.</summary>
    Public Property DeclaredAt As DateTime
    Public Property DeclaredCrewId As Guid?
End Class

Public Module ModSignatureWaiverMapping

    <System.Runtime.CompilerServices.Extension>
    Public Function ToDtoOut(reason As ClSignatureWaiverReason) As ClSignatureWaiverReasonDtoOut
        Return New ClSignatureWaiverReasonDtoOut With {
            .Code = reason.Code,
            .Label = reason.Label,
            .IsCommentRequired = reason.IsCommentRequired
        }
    End Function

    <System.Runtime.CompilerServices.Extension>
    Public Function ToDtoOut(waiver As ClSignatureWaiver, reasonLabel As String) As ClSignatureWaiverDtoOut
        Return New ClSignatureWaiverDtoOut With {
            .MissionId = waiver.MissionId,
            .ReasonCode = waiver.ReasonCode,
            .ReasonLabel = reasonLabel,
            .Comment = waiver.Comment,
            .DeclaredAt = waiver.DeclaredAt,
            .DeclaredCrewId = waiver.DeclaredCrewId
        }
    End Function

End Module
