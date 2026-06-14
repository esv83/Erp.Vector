Public Class ClLocation

    Public Property Latitude As Double
    Public Property Longitude As Double
    Public Property Etat As Integer

    ''' <summary>
    ''' Date de la position en UTC
    ''' </summary>
    ''' <returns></returns>
    Public Property [LocationUtcDate] As DateTime?

    Public Property Vitesse As Integer
    Public Property Cap As Integer
    Public Property Odometer As Integer


End Class
