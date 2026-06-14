Imports System.Text

Public Class ClJobDetailModel

    Public Sub New()
        Beneficiary = New ClPatientDto
    End Sub
    Public Property TransportMode As String = String.Empty
    Public Property IsSerial As Boolean
    Public Property TransportSens As String = String.Empty
    Public Property Schedule As String = String.Empty
    Public Property Appointment As String = String.Empty
    Public Property Departure As String = String.Empty
    Public Property Arrival As String = String.Empty
    Public Property Comments As String = String.Empty
    Public Property IsLastDay As Boolean
    ''' <summary>MOB-8 — Présence d'une signature patient (reflète MI_SIGNATURE_EXISTS).</summary>
    Public Property IsSign As Boolean
    Public Property Beneficiary As ClPatientDto

    Public Class ClPatientDto
        Public Property CompleteName As String
        Public Property DDN As String
        Public Property Age As String
        Public Property Phones As List(Of String)

    End Class

End Class

