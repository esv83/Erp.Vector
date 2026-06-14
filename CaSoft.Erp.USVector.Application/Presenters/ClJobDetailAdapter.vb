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

        End With

    End Sub

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
