Class MainWindow
    Private goRandom As Random
    Private mbStopping As Boolean
    Private mbStopped As Boolean

    Private Sub btnGoForIt_Click(sender As Object, e As RoutedEventArgs) Handles btnGoForIt.Click

        mbStopping = False
        mbStopped = False
        Dim nLikedSongsSize As Integer = -1
        Try
            nLikedSongsSize = CInt(tbListLength.Text)
        Catch

        End Try
        If nLikedSongsSize < 0 Then
            MsgBox("enter a valid list length", vbOKOnly Or vbExclamation, "error")
            tbListLength.Focus()
            Exit Sub
        End If

        Dim nQueueLength As Integer = -1
        Try
            nQueueLength = CInt(tbQueueLength.Text)
        Catch

        End Try
        If nLikedSongsSize < 0 Then
            MsgBox("enter a valid queue length", vbOKOnly Or vbExclamation, "error")
            tbQueueLength.Focus()
            Exit Sub
        End If

        If nLikedSongsSize <= nQueueLength + 1 Then
            MsgBox("Please enter a Queue that's at least two songs shorter than the length of the list", vbOKOnly Or vbExclamation, "error")
            tbQueueLength.Focus()
            Exit Sub
        End If

        Dim oLikedSongs As Dictionary(Of Integer, SongItem) = New Dictionary(Of Integer, SongItem)
        goRandom = New Random()

        'Populate the "liked songs" list 
        Dim nCount As Integer = nLikedSongsSize
        While nCount > 0
            Dim oLikedSong As SongItem = New SongItem
            oLikedSong.IndexNumber = nCount
            oLikedSong.Played = False
            oLikedSongs.Add(nCount, oLikedSong)
            nCount = nCount - 1
        End While

        Dim oThread As System.Threading.Thread = New System.Threading.Thread(Sub()
                                                                                 ShuffleThem(oLikedSongs:=oLikedSongs, nLikedSongsSize:=nLikedSongsSize, nQueueLength:=nQueueLength, oDispatcher:=Application.Current.Dispatcher)
                                                                             End Sub)
        oThread.SetApartmentState(System.Threading.ApartmentState.STA)
        oThread.Start()


    End Sub

    Private Sub ShuffleThem(ByVal oLikedSongs As Dictionary(Of Integer, SongItem),
                            ByVal nLikedSongsSize As Integer,
                            ByVal nQueueLength As Integer,
                            ByVal oDispatcher As System.Windows.Threading.Dispatcher)

        Dim dTotal As Double
        Dim dCount As Double

        While True
            dCount = dCount + 1
            Dim oCopyList As Dictionary(Of Integer, SongItem)
            oCopyList = New Dictionary(Of Integer, SongItem)
            For Each oSongItem As SongItem In oLikedSongs.Values
                Dim oCopy As SongItem = New SongItem
                oCopy.IndexNumber = oSongItem.IndexNumber
                oCopy.Played = False
                oCopyList.Add(oCopy.IndexNumber, oCopy)
            Next

            'Create the first Queue of 80 items
            Dim oQueue As List(Of SongItem) = New List(Of SongItem)
            Dim oPlayedSongs As Dictionary(Of Integer, SongItem) = New Dictionary(Of Integer, SongItem)

            Dim nCount As Integer = nQueueLength
            Dim nMax As Integer = nLikedSongsSize
            While nCount > 0
                nCount = nCount - 1
                nMax = nMax - 1
                Dim nSongNumber As Integer = goRandom.Next(minValue:=0, maxValue:=nMax - 1)
                Dim oSong As SongItem = oCopyList.Item(oCopyList.Keys(nSongNumber))
                oCopyList.Remove(oCopyList.Keys(nSongNumber))
                oQueue.Add(oSong)
            End While

            Dim nSongsPlayedUntilAPreviouslyPlayedSongIsPlayedAgain As Integer = 0
            While True
                nSongsPlayedUntilAPreviouslyPlayedSongIsPlayedAgain = nSongsPlayedUntilAPreviouslyPlayedSongIsPlayedAgain + 1
                Dim oSong As SongItem = oQueue.Item(0)
                If oSong.Played Then
                    Exit While
                End If
                oSong.Played = True
                oQueue.RemoveAt(0)
                oCopyList.Add(oSong.IndexNumber, oSong)
                nMax = oCopyList.Count
                Dim nSongNumber As Integer = goRandom.Next(minValue:=0, maxValue:=nMax - 1)
                oSong = oCopyList.Item(oCopyList.Keys(nSongNumber))
                oCopyList.Remove(oSong.IndexNumber)
                oQueue.Add(oSong)
            End While

            dTotal = dTotal + nSongsPlayedUntilAPreviouslyPlayedSongIsPlayedAgain
            oDispatcher.Invoke(Sub()
                                   rSimulations.Text = CInt(Math.Round(CDec(dCount), 0)).ToString()
                                   rSongsPlayed.Text = nSongsPlayedUntilAPreviouslyPlayedSongIsPlayedAgain.ToString
                                   rAverage.Text = (dTotal / dCount).ToString()
                               End Sub)
            If mbStopping Then
                Exit Sub
            End If
        End While

    End Sub

    Private Sub btnStop_Click(sender As Object, e As RoutedEventArgs) Handles btnStop.Click
        mbStopping = True
    End Sub
End Class

Public Class SongItem
    Public Property IndexNumber As Integer
    Public Property Played As Boolean
End Class