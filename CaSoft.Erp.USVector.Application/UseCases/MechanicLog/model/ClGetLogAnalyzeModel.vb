
Public Class ClGetLogAnalyzeModel

    Public Sub New()
        Actions = New List(Of ClGetActionModel)
    End Sub
    Public Property Immatriculation As String
    Public Property Crew As String
    Public Property Report As String
    Public Property AnalyzeBy As String
    Public Property ConcerningId As ClIdValue
    Public Property LogId As Integer?  'est null lors de la creation d'une analyze
    Public Property NatureId As ClIdValue
    Public Property ImmobilizeVehicle As Boolean
    Public Property Actions As List(Of ClGetActionModel)
    Public Property [Date] As Date
End Class

Public Class ClEditLogAnalyzeModel

    Public Sub New()
        Actions = New List(Of ClEditActionModel)
    End Sub
    Public Property Immatriculation As String
    Public Property Crew As String
    Public Property Report As String
    Public Property AnalyzeBy As String
    Public Property ConcerningId As Integer
    Public Property LogId As Integer
    Public Property NatureId As Integer
    Public Property ImmobilizeVehicle As Boolean
    Public Property Actions As List(Of ClEditActionModel)

End Class

