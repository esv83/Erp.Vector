Imports System.Text

Public Class ClCrew


    Public Sub New(gCrewId As Guid, lstEmployee As List(Of ClEmployee), dteServiceStartDate As DateTime, objVehicle As ClVehicle, Optional dteServiceEndDate As DateTime? = Nothing)

        _serviceEndDateR = New ClReliableEndOfService

        _crewId = gCrewId
        _employeeList = lstEmployee
        _serviceStart = dteServiceStartDate
        _serviceEnd = dteServiceEndDate
        _vehicle = objVehicle

    End Sub

    Public ReadOnly Property LastDriver As ClLastDriver

    Private _crewId As Guid
    Public ReadOnly Property CrewId As Guid
        Get
            Return _crewId
        End Get
    End Property

    Private _serviceStart As DateTime
    Public ReadOnly Property ServiceStart As DateTime
        Get
            Return _serviceStart
        End Get
    End Property

    Private _serviceEnd As DateTime?
    Public ReadOnly Property ServiceEnd As DateTime?
        Get
            Return _serviceEnd
        End Get
    End Property

    Private _serviceEndDateR As ClReliableEndOfService
    Public ReadOnly Property ServiceEndDateR As ClReliableEndOfService
        Get
            Return _serviceEndDateR
        End Get
    End Property

    Public Property IsServiceEnded As Boolean

    ''' <summary>Durée max d'une vacation avant de la considérer obsolète (vacation vraisemblablement oubliée, non clôturée).</summary>
    Public Const MaxServiceDurationHours As Integer = 18

    ''' <summary>
    ''' L'équipage est-il sélectionnable par le terrain à l'instant <paramref name="at"/> ? Trois conditions
    ''' cumulatives : <b>commencé</b> (début &lt;= <paramref name="at"/>), <b>non clôturé</b> (ni fin de service
    ''' déclarée, ni fenêtre de vacation dépassée) et <b>non obsolète</b> (durée écoulée &lt;=
    ''' <see cref="MaxServiceDurationHours"/> h — au-delà, vacation probablement oubliée).
    ''' </summary>
    Public Function IsSelectableAt(at As DateTime) As Boolean
        Dim started = _serviceStart <= at
        Dim closed = IsServiceEnded OrElse (_serviceEnd.HasValue AndAlso _serviceEnd.Value < at)
        Dim obsolete = (at - _serviceStart) > TimeSpan.FromHours(MaxServiceDurationHours)
        Return started AndAlso Not closed AndAlso Not obsolete
    End Function



    Private _employeeList As List(Of ClEmployee)
    Public ReadOnly Property EmployeeList As List(Of ClEmployee)
        Get
            Return _employeeList
        End Get
    End Property

    Private _vehicle As ClVehicle
    Public ReadOnly Property Vehicle As ClVehicle
        Get
            Return _vehicle
        End Get
    End Property

    Public Property HasVehicle As Boolean

    Public Sub SetLastDriver(employee As ClEmployee, dteFrom As DateTime)
        Dim lastDriver = New ClLastDriver(employee, dteFrom)
        SetLastDriver(lastDriver)
    End Sub
    Public Sub SetLastDriver(lastDriver As ClLastDriver)
        If _LastDriver IsNot Nothing Then
            If _LastDriver.Employee.Id = lastDriver.Employee.Id Then
                Exit Sub
            End If
        End If
        _LastDriver = lastDriver
    End Sub

    Public Overrides Function ToString() As String
        Dim result As String = MyBase.ToString

        Dim sb As New StringBuilder

        'If Me.HasVehicle Then
        '    sb.Append($"{Me.Vehicle.Immatriculation} ")
        'End If

        For Each employee In EmployeeList

            sb.Append($"{employee.Name}, ")

        Next

        If sb.Length > 1 Then
            result = sb.ToString
        End If

        Return result


    End Function
End Class
