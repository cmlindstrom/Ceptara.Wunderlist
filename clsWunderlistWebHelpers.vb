Imports System.Runtime.InteropServices

'Class for deleting the cache.
Public Class Web

    ' Based on code from:
    ' https://support.microsoft.com/en-us/kb/311289#bookmark-2
    ' https://social.msdn.microsoft.com/Forums/office/en-US/49e39cf5-c9be-4106-bd3b-de21ef032f8e/how-to-delete-specific-cookies-with-access-vba?forum=accessdev

    Private Const _rootClass As String = "Wunderlist.Web"

    'No more items have been found.
    Private Const ERROR_NO_MORE_ITEMS = 259

    'For PInvoke: Contains information about an entry in the Internet cache
    <StructLayout(LayoutKind.Explicit, Size:=80)> _
    Public Structure INTERNET_CACHE_ENTRY_INFOA
        <FieldOffset(0)> Public dwStructSize As UInt32
        <FieldOffset(4)> Public lpszSourceUrlName As IntPtr
        <FieldOffset(8)> Public lpszLocalFileName As IntPtr
        <FieldOffset(12)> Public CacheEntryType As UInt32
        <FieldOffset(16)> Public dwUseCount As UInt32
        <FieldOffset(20)> Public dwHitRate As UInt32
        <FieldOffset(24)> Public dwSizeLow As UInt32
        <FieldOffset(28)> Public dwSizeHigh As UInt32
        <FieldOffset(32)> Public LastModifiedTime As ComTypes.FILETIME
        <FieldOffset(40)> Public ExpireTime As ComTypes.FILETIME
        <FieldOffset(48)> Public LastAccessTime As ComTypes.FILETIME
        <FieldOffset(56)> Public LastSyncTime As ComTypes.FILETIME
        <FieldOffset(64)> Public lpHeaderInfo As IntPtr
        <FieldOffset(68)> Public dwHeaderInfoSize As UInt32
        <FieldOffset(72)> Public lpszFileExtension As IntPtr
        <FieldOffset(76)> Public dwReserved As UInt32
        <FieldOffset(76)> Public dwExemptDelta As UInt32
    End Structure

    'For PInvoke: Initiates the enumeration of the cache groups in the Internet cache
    <DllImport("wininet.dll", SetLastError:=True, _
       CharSet:=CharSet.Auto, _
       EntryPoint:="FindFirstUrlCacheGroup", _
       CallingConvention:=CallingConvention.StdCall)> _
    Shared Function FindFirstUrlCacheGroup( _
        ByVal dwFlags As Int32, _
        ByVal dwFilter As Integer, _
        ByVal lpSearchCondition As IntPtr, _
        ByVal dwSearchCondition As Int32, _
        ByRef lpGroupId As Long, _
        ByVal lpReserved As IntPtr) As IntPtr
    End Function

    'For PInvoke: Retrieves the next cache group in a cache group enumeration
    <DllImport("wininet.dll", _
       SetLastError:=True, _
       CharSet:=CharSet.Auto, _
       EntryPoint:="FindNextUrlCacheGroup", _
       CallingConvention:=CallingConvention.StdCall)> _
    Shared Function FindNextUrlCacheGroup( _
        ByVal hFind As IntPtr, _
        ByRef lpGroupId As Long, _
        ByVal lpReserved As IntPtr) As Boolean
    End Function

    'For PInvoke: Releases the specified GROUPID and any associated state in the cache index file
    <DllImport("wininet.dll", _
       SetLastError:=True, _
       CharSet:=CharSet.Auto, _
       EntryPoint:="DeleteUrlCacheGroup", _
       CallingConvention:=CallingConvention.StdCall)> _
    Shared Function DeleteUrlCacheGroup( _
        ByVal GroupId As Long, _
        ByVal dwFlags As Int32, _
        ByVal lpReserved As IntPtr) As Boolean
    End Function

    'For PInvoke: Begins the enumeration of the Internet cache
    <DllImport("wininet.dll", _
        SetLastError:=True, _
        CharSet:=CharSet.Auto, _
        EntryPoint:="FindFirstUrlCacheEntryA", _
        CallingConvention:=CallingConvention.StdCall)> _
    Shared Function FindFirstUrlCacheEntry( _
    <MarshalAs(UnmanagedType.LPStr)> ByVal lpszUrlSearchPattern As String, _
         ByVal lpFirstCacheEntryInfo As IntPtr, _
         ByRef lpdwFirstCacheEntryInfoBufferSize As Int32) As IntPtr
    End Function

    'For PInvoke: Retrieves the next entry in the Internet cache
    <DllImport("wininet.dll", _
       SetLastError:=True, _
       CharSet:=CharSet.Auto, _
       EntryPoint:="FindNextUrlCacheEntryA", _
       CallingConvention:=CallingConvention.StdCall)> _
    Shared Function FindNextUrlCacheEntry( _
          ByVal hFind As IntPtr, _
          ByVal lpNextCacheEntryInfo As IntPtr, _
          ByRef lpdwNextCacheEntryInfoBufferSize As Integer) As Boolean
    End Function

    'For PInvoke: Removes the file that is associated with the source name from the cache, if the file exists
    <DllImport("wininet.dll", _
      SetLastError:=True, _
      CharSet:=CharSet.Auto, _
      EntryPoint:="DeleteUrlCacheEntryA", _
      CallingConvention:=CallingConvention.StdCall)> _
    Shared Function DeleteUrlCacheEntry( _
        ByVal lpszUrlName As IntPtr) As Boolean
    End Function

    <DllImport("kernel32.dll", _
        SetLastError:=True, _
        CharSet:=CharSet.Auto, _
        EntryPoint:="LocalAlloc", _
        CallingConvention:=CallingConvention.StdCall)> _
    Shared Function LocalAlloc(ByVal uFlags As Long, ByVal uBytes As Long) As Long

    End Function

    '   Private Declare Function LocalAlloc Lib "kernel32" (ByVal uFlags As Long, ByVal uBytes As Long) As Long
    Private Declare Function LocalFree Lib "kernel32" (ByVal hMem As Long) As Long
    Private Declare Sub CopyMemory Lib "kernel32" Alias "RtlMoveMemory" (pDest As IntPtr, pSource As IntPtr, ByVal dwLength As Long)
    Private Declare Function lstrcpyA Lib "kernel32" (ByVal RetVal As String, ByVal Ptr As IntPtr) As Long
    Private Declare Function lstrlenA Lib "kernel32" (ByVal Ptr As IntPtr) As Long

    ''' <summary>
    ''' Converts a IntPtr to a string.
    ''' </summary>
    ''' <param name="lptr">IntPtr:</param>
    ''' <returns>String:</returns>
    ''' <remarks></remarks>
    Public Shared Function StrFromPtrA(ByVal lptr As IntPtr) As String

        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":StrFromPtrA"
        Dim strDefault As String = "Method failed."
        Try
            'Create buffer
            Dim leng As Long = 512 ' lstrlenA(lptr)
            'StrFromPtrA = New String(" ", leng) '  String$(lstrlenA(lptrString), 0)
            Dim strReturn As String = New String(" ", leng)

            Dim lReturn As Long = lstrcpyA(strReturn, lptr)

            Return strReturn.Trim

        Catch ex As Exception
            Client.LogError(strTrace, ex, strRoutine)
            Return String.Empty
        End Try

    End Function

    ''' <summary>
    ''' Enumerates the Cookie cache and removes cookies with the
    ''' specified name in the URL
    ''' </summary>
    ''' <param name="strName">String:</param>
    ''' <returns>Integer: # of caches removed</returns>
    ''' <remarks></remarks>
    Public Shared Function RemoveCookies(ByVal strName As String) As Integer

        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":RemoveCookies"
        Dim strDefault As String = "Method failed."
        Try
            If String.IsNullOrEmpty(strName) Then
                strTrace = "No name specified."
                Throw New Exception(strDefault)
            End If

            Dim iReturn As Integer = 0

            'Local variables
            Dim cacheEntryInfoBufferSizeInitial As Integer = 0
            Dim cacheEntryInfoBufferSize As Integer = 0
            Dim cacheEntryInfoBuffer As IntPtr = IntPtr.Zero
            Dim internetCacheEntry As Web.INTERNET_CACHE_ENTRY_INFOA
            Dim enumHandle As IntPtr = IntPtr.Zero
            Dim returnValue As Boolean = False

            'Start to delete URLs that do not belong to any group.
            enumHandle = Web.FindFirstUrlCacheEntry(vbNull, IntPtr.Zero, cacheEntryInfoBufferSizeInitial)

            If (enumHandle.Equals(IntPtr.Zero) And ERROR_NO_MORE_ITEMS.Equals(Marshal.GetLastWin32Error())) Then
                GoTo SkipOut
            End If

            ' Enumerate the cache files
            cacheEntryInfoBufferSize = cacheEntryInfoBufferSizeInitial
            cacheEntryInfoBuffer = Marshal.AllocHGlobal(cacheEntryInfoBufferSize)
            enumHandle = Web.FindFirstUrlCacheEntry(vbNull, cacheEntryInfoBuffer, cacheEntryInfoBufferSizeInitial)

            While (True)
                internetCacheEntry = CType(Marshal.PtrToStructure(cacheEntryInfoBuffer, _
                                                                  GetType(Web.INTERNET_CACHE_ENTRY_INFOA)),  _
                                                              Web.INTERNET_CACHE_ENTRY_INFOA)

                cacheEntryInfoBufferSizeInitial = cacheEntryInfoBufferSize

                Dim sourceURLName As String = Web.StrFromPtrA(internetCacheEntry.lpszSourceUrlName)
                If sourceURLName.ToLower.Contains(strName.ToLower) Then
                    returnValue = Web.DeleteUrlCacheEntry(internetCacheEntry.lpszSourceUrlName)

                    If (Not returnValue) Then
                        'Console.WriteLine("Error Deleting: {0}", Marshal.GetLastWin32Error())
                    Else
                        iReturn += 1
                    End If
                Else
                    strTrace = "Ignoring cache file: " & sourceURLName
                End If

                returnValue = Web.FindNextUrlCacheEntry(enumHandle, cacheEntryInfoBuffer, cacheEntryInfoBufferSizeInitial)
                If (Not returnValue And ERROR_NO_MORE_ITEMS.Equals(Marshal.GetLastWin32Error())) Then
                    Exit While
                End If

                If (Not returnValue And cacheEntryInfoBufferSizeInitial > cacheEntryInfoBufferSize) Then

                    cacheEntryInfoBufferSize = cacheEntryInfoBufferSizeInitial
                    Dim tempIntPtr As New IntPtr(cacheEntryInfoBufferSize)
                    cacheEntryInfoBuffer = Marshal.ReAllocHGlobal(cacheEntryInfoBuffer, tempIntPtr)
                    returnValue = Web.FindNextUrlCacheEntry(enumHandle, cacheEntryInfoBuffer, cacheEntryInfoBufferSizeInitial)
                End If
            End While

            Marshal.FreeHGlobal(cacheEntryInfoBuffer)

SkipOut:
            Return iReturn

        Catch ex As Exception
            Client.LogError(strTrace, ex, strRoutine)
            Return -1
        End Try

    End Function

    Public Class UnitTest

        Public Shared Sub Main()

            Dim strTrace As String = String.Empty

            'Indicates that all of the cache groups in the user's system should be enumerated
            '   Const CACHEGROUP_SEARCH_ALL = &H0
            'Indicates that all of the cache entries that are associated with the cache group should be deleted,
            'unless the entry belongs to another cache group.
            '   Const CACHEGROUP_FLAG_FLUSHURL_ONDELETE = &H2
            'File not found.
            '   Const ERROR_FILE_NOT_FOUND = &H2
            'No more items have been found.
            Const ERROR_NO_MORE_ITEMS = 259
            'Pointer to a GROUPID variable
            Dim groupId As Long = 0

            'Local variables
            Dim cacheEntryInfoBufferSizeInitial As Integer = 0
            Dim cacheEntryInfoBufferSize As Integer = 0
            Dim cacheEntryInfoBuffer As IntPtr = IntPtr.Zero
            Dim internetCacheEntry As Web.INTERNET_CACHE_ENTRY_INFOA
            Dim enumHandle As IntPtr = IntPtr.Zero
            Dim returnValue As Boolean = False

            'Delete the groups first.
            'Groups may not always exist on the system.
            'For more information, visit the following Microsoft Web site: 
            'http://msdn.microsoft.com/library/?url=/workshop/networking/wininet/overview/cache.asp
            'By default, a URL does not belong to any group. Therefore, that cache may become
            'empty even when CacheGroup APIs are not used because the existing URL does not belong to any group.     

            'enumHandle = Web.FindFirstUrlCacheGroup(0, CACHEGROUP_SEARCH_ALL, IntPtr.Zero, 0, groupId, IntPtr.Zero)
            ''If there are no items in the Cache, you are finished.
            'If (Not enumHandle.Equals(IntPtr.Zero) And ERROR_NO_MORE_ITEMS.Equals(Marshal.GetLastWin32Error)) Then
            '    Exit Sub
            'End If

            ''Loop through Cache Group, and then delete entries.
            'While (True)
            '    'Delete a particular Cache Group.
            '    returnValue = Web.DeleteUrlCacheGroup(groupId, CACHEGROUP_FLAG_FLUSHURL_ONDELETE, IntPtr.Zero)

            '    If (Not returnValue And ERROR_FILE_NOT_FOUND.Equals(Marshal.GetLastWin32Error())) Then
            '        returnValue = Web.FindNextUrlCacheGroup(enumHandle, groupId, IntPtr.Zero)
            '    End If

            '    If (Not returnValue And (ERROR_NO_MORE_ITEMS.Equals(Marshal.GetLastWin32Error()) Or _
            '                             ERROR_FILE_NOT_FOUND.Equals(Marshal.GetLastWin32Error()))) Then
            '        Exit While
            '    End If
            'End While

            'Start to delete URLs that do not belong to any group.
            enumHandle = Web.FindFirstUrlCacheEntry(vbNull, IntPtr.Zero, cacheEntryInfoBufferSizeInitial)

            If (enumHandle.Equals(IntPtr.Zero) And ERROR_NO_MORE_ITEMS.Equals(Marshal.GetLastWin32Error())) Then
                Exit Sub
            End If

            cacheEntryInfoBufferSize = cacheEntryInfoBufferSizeInitial
            cacheEntryInfoBuffer = Marshal.AllocHGlobal(cacheEntryInfoBufferSize)
            enumHandle = Web.FindFirstUrlCacheEntry(vbNull, cacheEntryInfoBuffer, cacheEntryInfoBufferSizeInitial)

            While (True)
                internetCacheEntry = CType(Marshal.PtrToStructure(cacheEntryInfoBuffer, _
                                                                  GetType(Web.INTERNET_CACHE_ENTRY_INFOA)),  _
                                                              Web.INTERNET_CACHE_ENTRY_INFOA)

                cacheEntryInfoBufferSizeInitial = cacheEntryInfoBufferSize

                Dim sourceURLName As String = Web.StrFromPtrA(internetCacheEntry.lpszSourceUrlName)
                If sourceURLName.ToLower.Contains("wunderlist") Then
                    returnValue = Web.DeleteUrlCacheEntry(internetCacheEntry.lpszSourceUrlName)

                    If (Not returnValue) Then
                        'Console.WriteLine("Error Deleting: {0}", Marshal.GetLastWin32Error())
                    End If
                Else
                    strTrace = "Ignoring cache file: " & sourceURLName

                End If


                returnValue = Web.FindNextUrlCacheEntry(enumHandle, cacheEntryInfoBuffer, cacheEntryInfoBufferSizeInitial)
                If (Not returnValue And ERROR_NO_MORE_ITEMS.Equals(Marshal.GetLastWin32Error())) Then
                    Exit While
                End If

                If (Not returnValue And cacheEntryInfoBufferSizeInitial > cacheEntryInfoBufferSize) Then

                    cacheEntryInfoBufferSize = cacheEntryInfoBufferSizeInitial
                    Dim tempIntPtr As New IntPtr(cacheEntryInfoBufferSize)
                    cacheEntryInfoBuffer = Marshal.ReAllocHGlobal(cacheEntryInfoBuffer, tempIntPtr)
                    returnValue = Web.FindNextUrlCacheEntry(enumHandle, cacheEntryInfoBuffer, cacheEntryInfoBufferSizeInitial)
                End If
            End While
            Marshal.FreeHGlobal(cacheEntryInfoBuffer)

        End Sub

    End Class

End Class

Public NotInheritable Class WebHelper

    ' Adopted from here:
    ' http://stackoverflow.com/questions/912741/how-to-delete-cookies-from-windows-form

    Private Const _rootClass As String = "Wunderlist.Web"

    Private Sub New()
    End Sub

    Public Shared Function SupressCookiePersist() As Boolean
        ' 3 = INTERNET_SUPPRESS_COOKIE_PERSIST 
        ' 81 = INTERNET_OPTION_SUPPRESS_BEHAVIOR
        Return SetOption(81, 3)
    End Function

    Public Shared Function EndBrowserSession() As Boolean
        ' 42 = INTERNET_OPTION_END_BROWSER_SESSION 
        Return SetOption(42, Nothing)
    End Function

    Private Shared Function SetOption(settingCode As Integer, [option] As System.Nullable(Of Integer)) As Boolean

        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":SetOption"
        Dim strDefault As String = "Method failed."
        Try
            Dim optionPtr As IntPtr = IntPtr.Zero
            Dim size As Integer = 0
            If [option].HasValue Then
                size = 4
                optionPtr = Marshal.AllocCoTaskMem(size)
                Marshal.WriteInt32(optionPtr, [option].Value)
            End If

            Dim success As Boolean = InternetSetOption(0, settingCode, optionPtr, size)

            If optionPtr <> IntPtr.Zero Then
                Marshal.Release(optionPtr)
            End If
            Return success
        Catch ex As Exception
            Client.LogError(strTrace, ex, strRoutine)
            Return False
        End Try

    End Function

    <DllImport("wininet.dll", CharSet:=CharSet.Auto, SetLastError:=True)> _
    Private Shared Function InternetSetOption(hInternet As Integer, dwOption As Integer, lpBuffer As IntPtr, dwBufferLength As Integer) As Boolean
    End Function

End Class


