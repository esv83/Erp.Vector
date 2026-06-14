Imports System.Text

Public Class ClCrew


    Public Sub New(gCrewId As Guid, lstEmployee As List(Of ClEmployee), dteServiceStartDate As DateTime, objVehicle As ClVehicle)

        _serviceEndDateR = New ClReliableEndOfService

        _crewId = gCrewId
        _employeeList = lstEmployee
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
