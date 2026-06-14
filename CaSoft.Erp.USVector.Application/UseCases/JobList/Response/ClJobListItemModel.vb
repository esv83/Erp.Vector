Namespace Dto


    Public Class ClJobListItemModel
        Public Property Index As Integer?
        Public Property JobId As Guid
        Public Property Patient As String = String.Empty
        Public Property TransportMode As Integer
        Public Property TransportType As Integer
        Public Property TransportSens As Integer
        Public Property IsSerial As Boolean
        Public Property IsAck As Boolean
        Public Property IsTerminated As Boolean
        ''' <summary>MOB-8 — Présence d'une signature patient (reflète MI_SIGNATURE_EXISTS).</summary>
        Public Property SignatureExists As Boolean
        Public Property schedule As String = String.Empty
        Public Property Appointment As DateTime?
        Public Property Departure As String = String.Empty
        Public Property Arrival As String = String.Empty
    End Class

End Namespace