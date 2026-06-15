''' <summary>P1 — Dépôt d'une photo de carte mutuelle pour un bénéficiaire.</summary>
Public Class ClUploadMutuelleCardCommand

    Public Sub New(beneficiaryId As Guid, image As Byte(), contentType As String,
                   crewId As Guid?, missionId As Guid?)
        _BeneficiaryId = beneficiaryId
        _Image = image
        _ContentType = contentType
        _CrewId = crewId
        _MissionId = missionId
    End Sub

    Public ReadOnly Property BeneficiaryId As Guid
    Public ReadOnly Property Image As Byte()
    Public ReadOnly Property ContentType As String
    Public ReadOnly Property CrewId As Guid?
    Public ReadOnly Property MissionId As Guid?

End Class
