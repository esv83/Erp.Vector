Public Class ClDriveLocation
    Inherits ClLocation

    Public Sub New(ByRef latitude As ClLatitude, ByRef longitude As ClLongitude)
        MyBase.New(latitude, longitude)
    End Sub

    Public Property LocalTime As DateTime
    Public Property Speed As Integer
    Public Property Heading As Integer
    Public Property Odometer As Double
    Public Property Ignition As Boolean

    Public Overrides Function ToString() As String
        Return $"Time: {LocalTime.ToShortTimeString}, Speed: {Speed}, Lat: {Latitude.ToString}, Long: {Longitude.ToString},  Heading: {Heading}, Odometer: {Odometer}, Ignition: {Ignition}"
    End Function
End Class
