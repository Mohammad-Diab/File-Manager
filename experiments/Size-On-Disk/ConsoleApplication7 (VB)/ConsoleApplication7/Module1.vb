Imports System.IO
Imports System.Management
Imports System.Runtime.InteropServices

Module Module1

    Sub Main()
        Console.WriteLine(GetFileSizeOnDisk("D:\Games\War Chess\data1.pak"))
        Console.Read()
    End Sub

    Private Function GetFileSizeOnDisk(file As String) As Decimal
        Dim info As New FileInfo(file)
        Dim blockSize As UInt64 = 0
        Dim clusterSize As UInteger
        Dim searcher As New ManagementObjectSearcher("select BlockSize,NumberOfBlocks from Win32_Volume WHERE DriveLetter = '" + info.Directory.Root.FullName.TrimEnd("\") + "'")

        For Each vi As ManagementObject In searcher.[Get]()
            blockSize = vi("BlockSize")
            Exit For
        Next
        searcher.Dispose()
        clusterSize = blockSize
        Dim hosize As UInteger
        Dim losize As UInteger = GetCompressedFileSizeW(file, hosize)
        Dim size As Long
        size = CLng(hosize) << 32 Or losize
        Dim bytes As Decimal = ((size + clusterSize - 1) / clusterSize) * clusterSize

        Return CDec(bytes) / 1024
    End Function

    <DllImport("kernel32.dll")>
    Private Function GetCompressedFileSizeW(<[In](), MarshalAs(UnmanagedType.LPWStr)> lpFileName As String, <Out(), MarshalAs(UnmanagedType.U4)> lpFileSizeHigh As UInteger) As UInteger
    End Function

End Module
