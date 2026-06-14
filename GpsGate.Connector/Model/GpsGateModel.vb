Public Class TokenResponse
    Public Property token As String

End Class

Public Class View
    Public Property applicationID As Integer
    Public Property description As String
    Public Property id As Integer
    Public Property matchAllTags As Boolean
    Public Property name As String
    Public Property statusFilter As String
    Public Property tagIDs As Integer()

End Class

Public Class Position
    Public Property altitude As Double
    Public Property longitude As Double
    Public Property latitude As Double
End Class

Public Class Velocity
    Public Property groundSpeed As Double
    Public Property heading As Double
End Class

Public Class TrackPoint
    Public Property position As Position
    Public Property velocity As Velocity
    Public Property utc As DateTime
    Public Property valid As Boolean
End Class

Public Class Msisdn
    Public Property raw As String
End Class

Public Class Device
    Public Property id As Integer
    Public Property created As DateTime
    Public Property name As String
    Public Property hidePosition As Boolean
    Public Property proximity As Double
    Public Property msisdn As Msisdn
    Public Property email As String
    Public Property apn As String
    Public Property gprsUsername As String
    Public Property gprsPassword As String
    Public Property lastIP As String
    Public Property lastPort As Integer
    Public Property staticIP As String
    Public Property staticPort As Integer
    Public Property protocolID As String
    Public Property profileId As Integer
    Public Property protocolVersionID As Integer
    Public Property msgFieldDictionaryID As Integer
    Public Property deviceDefinitionID As Integer
    Public Property mobileNetworkID As Integer
    Public Property longitude As Double
    Public Property latitude As Double
    Public Property timeStamp As DateTime
    Public Property ownerID As Integer
    Public Property ownerUsername As String
    Public Property ownerName As String
    Public Property ownerEmail As String
    Public Property devicePassword As String
    Public Property oneWireVariables As Object()
    Public Property imei As String
End Class

Public Class Icon
    Public Property iconOffsetX As Integer
    Public Property iconOffsetY As Integer
    Public Property iconGUID As String
    Public Property rotatable As Boolean
End Class

Public Class ClGgsUserModel
    Public Property trackPoint As TrackPoint
    Public Property calculatedSpeed As Double
    Public Property deviceActivity As DateTime
    Public Property username As String
    Public Property name As String
    Public Property surname As String
    Public Property email As String
    Public Property phoneNumber As String
    Public Property devices As Device()
    Public Property userTemplateID As Integer
    Public Property id As Integer
    Public Property icon As Icon
    Public Property lastTransport As String
    Public Property description As String
End Class

Public Class Tag
    Public Property applicationId As Integer
    Public Property description As String
    Public Property id As Integer
    Public Property name As String
    Public Property usersIds As Integer()
End Class

Public Class Variable
    Public Property name As String
    Public Property time As String
    Public Property type As String
    Public Property value As String
End Class

Public Class ClGgsUserStatutModel
    Public Property deviceActivity As DateTime
    Public Property id As Integer
    Public Property name As String
    Public Property position As Position
    Public Property username As String
    Public Property uTC As DateTime
    Public Property variables As Variable()
    Public Property velocity As Velocity
End Class

