
Public Interface IGeolocServer
        Function GetUsers() As List(Of ClUserModel)
        Function GetUser(strImmat As String) As ClUserModel
        Function GetUpdatedUsers(fromDate As Date) As List(Of ClUserModel)
        Function AddToView(strImmatriculation As String) As Boolean
        Function RemoveFromAmuView(strImmatriculation As String) As Boolean
    End Interface


