Imports System.IO
Imports System.Text.Json

Public Class ClConfigurationManager

    Private Const ConfigFilePath As String = "config.json"

    ' Classe générique pour stocker les paramètres
    Private Class Configuration
        Public Property Settings As New Dictionary(Of String, String)
    End Class

    Private _config As Configuration

    ' Charger la configuration depuis le fichier
    Public Sub LoadConfig()
        If File.Exists(ConfigFilePath) Then
            Dim json As String = File.ReadAllText(ConfigFilePath)
            _config = JsonSerializer.Deserialize(Of Configuration)(json)
        Else
            _config = New Configuration()
            SaveConfig() ' Crée un fichier par défaut si inexistant
        End If
    End Sub

    ' Sauvegarder la configuration dans le fichier
    Public Sub SaveConfig()
        Dim json As String = JsonSerializer.Serialize(_config, New JsonSerializerOptions With {.WriteIndented = True})
        File.WriteAllText(ConfigFilePath, json)
    End Sub

    ' Lire une variable générique
    Public Function GetSetting(key As String, Optional defaultValue As String = "") As String
        If _config Is Nothing Then LoadConfig()
        If _config.Settings.ContainsKey(key) Then
            Return _config.Settings(key)
        Else
            Return defaultValue
        End If
    End Function

    ' Modifier une variable générique
    Public Sub SetSetting(key As String, value As String)
        If _config Is Nothing Then LoadConfig()
        _config.Settings(key) = value
        SaveConfig()
    End Sub

    ' Supprimer une variable
    Public Sub RemoveSetting(key As String)
        If _config Is Nothing Then LoadConfig()
        If _config.Settings.ContainsKey(key) Then
            _config.Settings.Remove(key)
            SaveConfig()
        End If
    End Sub

    ' Retourner toutes les clés
    Public Function GetAllKeys() As List(Of String)
        If _config Is Nothing Then LoadConfig()
        Return _config.Settings.Keys.ToList()
    End Function

End Class
