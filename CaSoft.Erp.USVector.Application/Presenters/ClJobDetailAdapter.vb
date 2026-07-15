Imports System.Text

Public Class ClJobDetailAdapter
    Inherits ClJobDetailModel

    Public Sub New(job As ClJob)
        Adapt(job)
    End Sub
    Private Sub Adapt(job As ClJob)

        If job.Beneficiary.DDN.HasValue Then
            Me.Beneficiary.DDN = job.Beneficiary.DDN
            Me.Beneficiary.Age = ClHelper.GetAgeString(job.Beneficiary.DDN)
        End If

        With Me
            .Beneficiary.CompleteName = job.Beneficiary.LastName + " " + job.Beneficiary.Name
            .Beneficiary.Phones = job.Beneficiary.Phones
            .TransportMode = GetTransportModeDisplay(job.TransportMode.Value)
            .IsSerial = job.IsIterativ
            .TransportSens = GetTransportSensDisplay(job.TransportType.Value)
            .Schedule = job.Schedule.ToString
            .Appointment = job.Appointment.ToString
            .Comments = job.Comments
            .Departure = Getparagraphe(job.Departure)
            .Arrival = Getparagraphe(job.Arrival)
            .IsLastDay = job.IsLastDay
            .IsSign = job.IsSign

            ' Nouveaux champs (règles serveur) :
            ' Prise en charge formatée sur le Schedule.
            .ScheduleLabel = FormatSchedule(job.Schedule)
            ' Mode : secondaire (sous-catégorie) si présent, sinon principal.
            .TransportModeLabel = If(String.IsNullOrWhiteSpace(job.TransportSubCategoryLabel),
                                     job.TransportModeLabel, job.TransportSubCategoryLabel)
            ' Lieux détaillés structurés.
            .PickupLocation = ToLocationDto(job.PickupLocation)
            .DropoffLocation = ToLocationDto(job.DropoffLocation)

        End With

    End Sub

    ''' <summary>« à HH:mm » le jour même, sinon « dddd dd/MM/yyyy à HH:mm » (culture fr-FR).</summary>
    Private Shared Function FormatSchedule(dte As Date) As String
        Dim fr = New System.Globalization.CultureInfo("fr-FR")
        If dte.Date = Date.Today Then
            Return dte.ToString("'à' HH:mm", fr)
        End If
        Return dte.ToString("dddd dd/MM/yyyy 'à' HH:mm", fr)
    End Function

    Private Shared Function ToLocationDto(loc As ClJobLocation) As ClJobLocationDto
        Dim dto As New ClJobLocationDto
        If loc IsNot Nothing Then
            dto.Nom = loc.Nom
            dto.Service = loc.Service
            dto.Adresse = loc.Adresse
            dto.Residence = loc.Residence
            dto.BatEtage = loc.BatEtage
            dto.Commune = loc.Commune
            dto.Complement = loc.Complement
            ' Sous-objet présent seulement si le lieu est réellement géocodé : l'UI teste
            ' sa présence plutôt que deux 0.0 qui pointeraient au large du golfe de Guinée.
            If loc.Latitude.HasValue AndAlso loc.Longitude.HasValue Then
                dto.Coordinates = New ClJobCoordinatesDto With {
                    .Latitude = loc.Latitude.Value,
                    .Longitude = loc.Longitude.Value
                }
            End If
        End If
        Return dto
    End Function

    Private Function GetTransportSensDisplay(value As TripType) As String
        Dim result As String
        Select Case value
            Case TripType.OneWay
                result = "Aller simple"
            Case TripType.OneWayAndReturn
                result = "Aller / Retour"
            Case Else
                result = value.ToString
        End Select

        Return result

    End Function

    Private Function GetTransportModeDisplay(value As TransportModeEnumeration) As String
        Dim result As String
        Select Case value
            Case TransportModeEnumeration.ByTap
                result = "Transport assis (TAP)"
            Case TransportModeEnumeration.ByAmbulance
                result = "Ambulance"
            Case TransportModeEnumeration.Psl
                result = "Produits Sanguins"
            Case Else
                result = "Autre"
        End Select

        Return result
    End Function

    Private Function Getparagraphe(input As List(Of String)) As String
        Dim sbDep As New StringBuilder
        For Each line In input
            sbDep.AppendLine(line)
        Next

        Return sbDep.ToString

    End Function

End Class
