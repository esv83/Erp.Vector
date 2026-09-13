Imports System.Runtime.CompilerServices

''' <summary>
''' Mapping Application : jalons opérationnels (métier) → DTO de la timeline.
''' Extrait de <c>ModToModelExtension</c> (dossier MechanicLog) lors du retrait de MOB-14 : ces deux
''' mappings n'avaient rien de mécanique.
''' </summary>
Public Module ModJobTimeMapping

    <Extension()>
    Public Function ToJobTimeModel(jobTime As ClJobTimeData) As ClJobTimeModel
        Dim jobTimeModel As New ClJobTimeModel
        With jobTimeModel
            .GoTime = New ClTimeFormatAdapter(jobTime.GoTime).ToString
            .OnSiteTime = New ClTimeFormatAdapter(jobTime.OnSiteTime).ToString
            .TerminatedTime = New ClTimeFormatAdapter(jobTime.TerminateTime).ToString
        End With
        Return jobTimeModel
    End Function

    ''' <summary>
    ''' Mapping « Option A » : projette les jalons opérationnels vers une timeline ordonnée et labellisée.
    ''' Les 3 jalons sont toujours émis dans l'ordre chronologique ; <c>At</c> = Nothing si non franchi.
    ''' </summary>
    <Extension()>
    Public Function ToJobTimelineDtoOut(jobTime As ClJobTimeData) As ClJobTimelineDtoOut
        Dim statuses As New List(Of ClJobStatusDtoOut) From {
            New ClJobStatusDtoOut With {
                .Order = 1, .Code = "EnRoute", .Label = "En route",
                .At = New ClTimeFormatAdapter(jobTime.GoTime).ToString},
            New ClJobStatusDtoOut With {
                .Order = 2, .Code = "SurPlace", .Label = "Sur place",
                .At = New ClTimeFormatAdapter(jobTime.OnSiteTime).ToString},
            New ClJobStatusDtoOut With {
                .Order = 3, .Code = "Disponible", .Label = "Disponible",
                .At = New ClTimeFormatAdapter(jobTime.TerminateTime).ToString}
        }

        Return New ClJobTimelineDtoOut With {
            .JobId = jobTime.JobId,
            .Statuses = statuses
        }
    End Function

End Module
