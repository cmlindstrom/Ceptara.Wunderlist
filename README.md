# Ceptara.Wunderlist
The repository contains methods to interact with the Wunderlist v1 RESTful API - the libary is written in vb.net.  It contains:

1) Client: used to interface with the Wunderlist service
2) AppConfig: class used to store your app keys, i.e. ClientId, ClientSecret and callBack URL.

The library supports interactions with the List and Task services.

Example

Setup your keys

    Private Shared Function GetAppConfig() As Wunderlist.AppConfig
        Dim myConfig As New Wunderlist.AppConfig(ClientID, ClientSecret,"http://my.app.com/oauth2redirect")
        Return myConfig
    End Function
    
Make a call

    public shared function GetMyLists() as List(Of Wunderlist.List)
    
        strTrace = "Set up the appConfig"
        Dim myConfig As Wunderlist.AppConfig = GetAppConfig()

        strTrace = "Get the saved token"
        Dim myToken As New Wunderlist.Client.oAuthAccessToken()
        myToken.RestoreFromStorageString(My.Settings.WunderlistStoredToken) 

        myW = New Wunderlist.Client(myConfig, myToken)
        
        Dim myLists As List(Of Wunderlist.List) = myW.GetLists() ' Can also use Async calls, i.e. myW.GetListsAsync()
        If IsNothing(myLists) Then
            strTrace = "A null collection returned."
            Throw New Exception(strDefault)
        End If
    
    end function
    
Supports presenting a login to get a user's token

    Public Shared Function GetNewConnection_UnitTest(ByVal R As TestSuite.Reporter) As Boolean

        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":GetNewConnection_UnitTest"
        Dim strDefault As String = "Method failed."
        Try
            ' Create a Client instance - configured with your keys
            Dim myW As New Wunderlist.Client(GetAppConfig)
            AddHandler myW.Connected, AddressOf ServiceConnected

            ' Presents a .net WebBrowser control to get the AuthCode and
            '   then exchange the code for an AccessToken
            myW.Connect()

        Catch ex As Exception
            TraceLogger(strTrace, ex, strRoutine)
            ErrorLogger(strTrace, ex, Err.Number, strRoutine)
        End Try

        Return True

    End Function
    Private Shared Sub ServiceConnected(ByVal sender As Object, e As EventArgs)
        Dim myW As Wunderlist.Client = TryCast(sender, Wunderlist.Client)
        If Not IsNothing(myW) Then
        
            Dim strToken As String = myW.AccessToken.Token

            MsgBox("Connected to Wunderlist using token: " & strToken)

            My.Settings.WunderlistStoredToken = myW.AccessTokenStored
            My.Settings.Save()
            
        End If

    End Sub

     
