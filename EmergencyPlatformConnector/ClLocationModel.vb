Public Class ClLocationModel
    Public Property Immatriculation As String
    Public Property IMEI As String
    Public Property Latitude As Double
    Public Property Longitude As Double
    Public Property State As Integer

    ''' <summary>
    ''' Date de la position en UTC
    ''' </summary>
    ''' <returns></returns>
    Public Property [LocationUtcDate] As DateTime?

    Public Property Speed As Integer
    Public Property Heading As Integer
    Public Property Odometer As Integer

End Class
