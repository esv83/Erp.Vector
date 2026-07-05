
Imports System.Reflection
Imports CaSoft.Framework

Public Class ClJob
    Inherits ClBusinessBase(Of ClJob)

#Region "Constructeurs"

    Private Sub New()

    End Sub

    Private Sub SetMission(Mission As ClMission)
        _IsSign = Mission.IsSign
        _TransportMode = Mission.TransportMode
        _TransportType = Mission.TransportType
        _TransportModeLabel = Mission.TransportModeLabel
        _TransportSubCategoryLabel = Mission.TransportSubCategoryLabel
        _Appointment = Mission.Appointment
        _IsLastDay = Mission.IsLastDay
        _Arrival = Mission.Arrival
        _Departure = Mission.Departure
        _PickupLocation = Mission.PickupLocation
        _DropoffLocation = Mission.DropoffLocation
        _IsIterativ = Mission.IsIterativ
        _Schedule = Mission.Schedule
        _IsAsap = Mission.IsAsap
        _MaxDelay = Mission.MaxDelay
        _CallTime = Mission.CallTime
        Me.Comments = Mission.Comments

    End Sub

#End Region

#Region "proprietes"

#Region "peristent"

    Private idProperty = RegisterProperty(Of Guid)(Function(f) f.Id)
    Public Shadows Property Id As Guid
        Get
            Return GetProperty(idProperty)
        End Get
        Private Set(value As Guid)
            SetProperty(idProperty, value)
        End Set
    End Property

    Private beneficiaryProperty = RegisterProperty(Of ClJobBeneficiary)(Function(f) f.Beneficiary, RelationshipTypes.Child)
    Public ReadOnly Property Beneficiary As ClJobBeneficiary
        Get
            Return GetProperty(beneficiaryProperty)
        End Get
    End Property
    Private contractTypeProperty = RegisterProperty(Of ClContractType)(Function(f) f.contractTypeProperty, RelationshipTypes.Child)
    Public Property ContractType As ClContractType
        Get
            Return GetProperty(contractTypeProperty)
        End Get
        Private Set(value As ClContractType)
            SetProperty(contractTypeProperty, value)
        End Set
    End Property

    Private timeDataProperty = RegisterProperty(Of ClJobTimeData)(Function(f) f.TimeData, RelationshipTypes.Child)
    Public Property TimeData As ClJobTimeData
        Get
            Return GetProperty(timeDataProperty)
        End Get
        Set(value As ClJobTimeData)
            SetProperty(timeDataProperty, value)
        End Set
    End Property

    Private commentsProperty = RegisterProperty(Of String)(Function(f) f.commentsProperty)
    Public Property Comments As String
        Get
            Return GetProperty(commentsProperty)
        End Get
        Set(value As String)
            SetProperty(commentsProperty, value)
        End Set
    End Property

    'Private _phonesProperty = RegisterProperty(Of List(Of String))(Function(f) f.Phones)
    'Public ReadOnly Property Phones As List(Of String)
    '    Get
    '        Return GetProperty(_phonesProperty)
    '    End Get
    'End Property

    Private _mailsProperty = RegisterProperty(Of List(Of String))(Function(f) f.Mails)
    Public ReadOnly Property Mails As List(Of String)
        Get
            Return GetProperty(_mailsProperty)
        End Get
    End Property

#End Region

    Public ReadOnly Property IsSign As Boolean
    Public ReadOnly Property TransportMode As ClTransportMode
    Public ReadOnly Property TransportType As ClTransportType
    Public ReadOnly Property TransportModeLabel As String
    Public ReadOnly Property TransportSubCategoryLabel As String
    Public ReadOnly Property Appointment As Date?
    Public ReadOnly Property IsLastDay As Boolean
    Public ReadOnly Property Arrival As List(Of String)
    Public ReadOnly Property Departure As List(Of String)
    Public ReadOnly Property PickupLocation As ClJobLocation
    Public ReadOnly Property DropoffLocation As ClJobLocation
    Public ReadOnly Property IsIterativ As Boolean
    Public ReadOnly Property Schedule As Date
    Public ReadOnly Property IsAsap As Boolean
    Public ReadOnly Property MaxDelay As Integer?
    Public ReadOnly Property CallTime As Date?

    Public Shared Function GetBuilder() As ClJobBuilder
        Return ClJobBuilder.GetBuilder
    End Function
#End Region

#Region "Methodes"
    Public Sub SetRead()
        TimeData.ReadTime = DateTime.Now
    End Sub
    Public Sub SetBeneficiary(beneficiary As ClJobBeneficiary)
        SetProperty(beneficiaryProperty, beneficiary)
    End Sub
    Public Sub UpdateAttribute(strName As String, newValue As Object) 'As String

        If ContractType.Attributs.ContainsKey(strName) Then
            If newValue IsNot Nothing Then
                ContractType.Attributs(strName).Value = newValue.ToString
            End If

        Else
            'Throw New ArgumentNullException(strName + " n'est pas présent dans la liste d'attributs")
        End If

    End Sub

    Public Sub SetGoTime(utcDateTime As DateTime?)

        TimeData.GoTime = utcDateTime.Value

    End Sub

    Public Sub SetOnSiteTime(utcDateTime As DateTime?)
        TimeData.OnSiteTime = utcDateTime
    End Sub

    Public Sub SetOnTerminatedTime(utcDateTime As DateTime?)
        TimeData.TerminateTime = utcDateTime
    End Sub

#End Region

    Public Class ClJobBuilder
        Private _job As ClJob
        Private Sub New()
            _job = New ClJob
        End Sub

        Public Shared Function GetBuilder() As ClJobBuilder
            Return New ClJobBuilder
        End Function
        Public Function WithPersistentSource() As ClJobBuilder
            _job.MarkFromPersistentSource()
            Return Me

        End Function
        Public Function WithId(gJobId As Guid) As ClJobBuilder
            _job.Id = gJobId
            Return Me

        End Function
        Public Function WithBeneficiary(beneficiary As ClJobBeneficiary) As ClJobBuilder
            _job.SetBeneficiary(beneficiary)
            Return Me

        End Function
        Public Function WithMission(mission As ClMission) As ClJobBuilder
            _job.SetMission(mission)
            Return Me

        End Function
        Public Function WithTimeData(timeData As ClJobTimeData) As ClJobBuilder
            _job.TimeData = timeData
            Return Me

        End Function
        Public Function WithContractType(contractType As ClContractType) As ClJobBuilder
            _job.ContractType = contractType
            Return Me

        End Function
        Public Function Build() As ClJob
            Return _job
        End Function

    End Class

End Class
