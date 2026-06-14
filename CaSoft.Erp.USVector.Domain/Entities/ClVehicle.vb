Public Class ClVehicle

    Private _id As Guid
    Private _immatriculation As String

    Public Sub New(gId As Guid, strImmatriculation As String, objLastKilometers As ClKilometers)

        _id = gId
        _immatriculation = strImmatriculation
        _LastKilometers = objLastKilometers

    End Sub
    Public ReadOnly Property Id As Guid
        Get
            Return _id
        End Get
    End Property
    Public ReadOnly Property Immatriculation As String
        Get
            Return _immatriculation
        End Get
    End Property
    Public Property IMEI As String
    Public Property [TransmitDate] As DateTime?
    Public ReadOnly Property HasLastKilometers As Boolean
        Get
            Return (_LastKilometers IsNot Nothing) AndAlso Not (_LastKilometers.IsEmpty)
        End Get
    End Property
    Public ReadOnly Property LastKilometers As ClKilometers
    Public Function SetKilometers(dteDate As Date, intKilometers As Integer, Optional strInputBy As String = "N/A") As Boolean
        Dim result As Boolean = True
        _LastKilometers = New ClKilometers(dteDate, intKilometers, strInputBy)
        Return True
    End Function
    Public ReadOnly Property Location As ClLocation

End Class
