
Imports CaSoft.Framework

Public Class ClMission
    Inherits ClBusinessBase(Of ClMission)

    Public Property MissionId As Guid
    Public Property IsLastDay As Boolean
    Public Property IsIterativ As Boolean
    Public Property IsAsap As Boolean
    Public Property IsSign As Boolean
    Public Property Appointment As DateTime?
    Public Property Schedule As DateTime
    Public Property CallTime As DateTime?
    Public Property MaxDelay As Integer?
    Public Property TransportMode As ClTransportMode
    Public Property TransportType As ClTransportType
    ' Libellé du mode principal (ex. « AMBULANCE », « VSL ») et de la sous-catégorie/secondaire
    ' (ex. « Bariatrique », « TPMR ») — libellés ERP. Vide si non renseigné.
    Public Property TransportModeLabel As String = String.Empty
    Public Property TransportSubCategoryLabel As String = String.Empty
    Public Property Comments As String
    Public Property Departure As List(Of String)
    Public Property Arrival As List(Of String)
    ' Lieux détaillés (structurés) pour affichage multi-lignes. Departure/Arrival restent la version
    ' « paragraphe » (compat) tirée des mêmes données.
    Public Property PickupLocation As ClJobLocation
    Public Property DropoffLocation As ClJobLocation

    Private _contractTypeIdProperty = RegisterProperty(Of Integer)(Function(f) f.ContractTypeId, RelationshipTypes.None, 2)
    Public Property ContractTypeId As Integer?
        Get
            Return GetProperty(_contractTypeIdProperty)
        End Get
        Set(value As Integer?)
            SetProperty(_contractTypeIdProperty, value)
        End Set
    End Property
    Public Property ContactId As Guid
End Class
