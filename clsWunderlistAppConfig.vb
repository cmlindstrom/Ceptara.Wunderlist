

Public Class AppConfig

#Region "Fields"

    Public Const _rootClass As String = "WL.AppConfig"

#End Region

#Region "Properties"

    ' - you can store your keys in this class and start the client
    '   with just a stored token

    Public Property ClientID As String = "Your_Client_ID"
    Public Property ClientSecret As String = "Your_Client_Secret"
    Public Property CallbackURL As String = "Your_Callback_URL"

    ' OR you can store the keys in your application and start the
    '   client using an AppConfig to get a token, and/or start
    '   start the client with a AppConfig AND a stored token.

#End Region

#Region "Events"

#End Region

#Region "Event Handlers"

#End Region

#Region "Constructor"

    Public Sub New()

    End Sub

    Public Sub New(ByVal clientID As String, ByVal clientSecret As String, callBackURL As String)
        _ClientID = clientID
        _ClientSecret = clientSecret
        _CallbackURL = callBackURL
    End Sub

#End Region

#Region "Methods"

#End Region

#Region "Supporting Methods"

#End Region

#Region "Supporting Classes"

#End Region


End Class



