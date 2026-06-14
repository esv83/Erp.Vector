Public Interface IEmergencyConnector
    Property TimerEnabled As Boolean
    Sub AddVehicleToTrackList(strImmatriculation As String)
    Sub UpdateVehicleStatut(strImmat As String, intStatut As Integer)  'TODO creer un enum pour le statut
    Function UpdatePositionFromGeolocToPlatform() As Integer
    Sub RemoveVehicleFromTrackList(strImmatriculation As String)

End Interface
