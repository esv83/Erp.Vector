Imports System.Text.Json

Public Class ClLocation

    Public Sub New(ByRef latitude As Double, ByRef longitude As Double)
        Me.Latitude = New ClLatitude(latitude)
        Me.Longitude = New ClLongitude(longitude)
    End Sub
    Public Sub New(ByRef latitude As ClLatitude, ByRef longitude As ClLongitude)
        Me.Latitude = latitude
        Me.Longitude = longitude
    End Sub

    Public ReadOnly Property Latitude As ClLatitude
    Public ReadOnly Property Longitude As ClLongitude
    Public ReadOnly Property IsNullLocation As Boolean
        Get
            If Latitude Is Nothing OrElse Longitude Is Nothing Then
                Return True
            End If

            Return Latitude.IsEmpty OrElse Longitude.IsEmpty

        End Get
    End Property



    'Public ReadOnly Property IsValid As Boolean
    '    Get
    '        Return Latitude.IsValid And Longitude.IsValid
    '    End Get
    'End Property


    Public Function DistanceFromInKm(other As ClLocation) As Double
        Const EarthRadiusKm As Double = 6371.0

        Dim result As Double
        'If Not (Me.IsValid AndAlso other.IsValid) Then
        '    Return Double.NaN
        'End If

        Dim lat1Rad = DegreesToRadians(Me.Latitude.Value)
        Dim lat2Rad = DegreesToRadians(other.Latitude.Value)
        Dim deltaLat = DegreesToRadians(other.Latitude.Value - Me.Latitude.Value)
        Dim deltaLon = DegreesToRadians(other.Longitude.Value - Me.Longitude.Value)

        Dim a = Math.Sin(deltaLat / 2) ^ 2 +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(deltaLon / 2) ^ 2

        Dim c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a))

        result = EarthRadiusKm * c

        Return result

    End Function

    Public Function GetDistanceInMeters(other As ClLocation) As Double
        Dim result As Double
        result = DistanceFromInKm(other) * 1000
        Return result
    End Function
    Private Function DegreesToRadians(degrees As Double) As Double
        Return degrees * Math.PI / 180


    End Function

    Public Function ToGeoJson() As String
        Dim result = String.Empty
        If Longitude.Value.HasValue AndAlso Latitude.Value.HasValue Then
            result = $"{{""type"":""Point"",""coordinates"":[{Longitude.Value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)},{Latitude.Value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}]}}"

        End If

        Return result

    End Function

    Public Shared Function FromGeoJson(strJson As String) As ClLocation

        ' Dim json = "{""type"":""Point"",""coordinates"":[2.3522,48.8566]}"
        Dim point = JsonSerializer.Deserialize(Of GeoJsonPoint)(strJson)

            Dim longitude As New ClLongitude(point.coordinates(0))
            Dim latitude As New ClLatitude(point.coordinates(1))

            Return New ClLocation(latitude, longitude)


    End Function

    Public Shared Function NullLocation() As ClLocation
        Return New ClLocation(0, 0)
    End Function

    Private Class GeoJsonPoint
        Public Property type As String
        Public Property coordinates As Double()
    End Class

End Class
