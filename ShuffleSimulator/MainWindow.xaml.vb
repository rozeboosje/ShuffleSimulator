Imports System.ComponentModel

Class MainWindow
    Private goRandom As Random
    Private mbStopping As Boolean = True
    Private mbStopped As Boolean = True

    Private Sub btnGoForIt_Click(sender As Object, e As RoutedEventArgs) Handles btnGoForIt.Click

        mbStopping = False
        mbStopped = False

        If cbKeepRestarting.IsChecked Then
            tbNumberUntilRepeat.Visibility = Visibility.Collapsed
            tbAverageNumberUntilRepeat.Visibility = Visibility.Collapsed
            tbMaxNumberOfRepeats.Visibility = Visibility.Visible
            tbAverageNumberOfRepeats.Visibility = Visibility.Visible
        Else
            tbNumberUntilRepeat.Visibility = Visibility.Visible
            tbAverageNumberUntilRepeat.Visibility = Visibility.Visible
            tbMaxNumberOfRepeats.Visibility = Visibility.Collapsed
            tbAverageNumberOfRepeats.Visibility = Visibility.Collapsed
        End If

        'Validate the input. I just check that the user enters valid integers and that the size of the queue doesn't exceed the size of the playlist minus 2
        Dim nLikedSongsSize As Integer = -1
        Try
            nLikedSongsSize = CInt(txtListLength.Text)
        Catch

        End Try
        If nLikedSongsSize < 0 Then
            mbStopping = True
            mbStopped = True
            MsgBox("enter a valid number for the size of the play list", vbOKOnly Or vbExclamation, "error")
            txtListLength.Focus()
            Exit Sub
        End If

        Dim nQueueLength As Integer = -1
        Try
            nQueueLength = CInt(txtQueueLength.Text)
        Catch

        End Try
        If nQueueLength < 0 Then
            mbStopping = True
            mbStopped = True
            MsgBox("enter a valid number for the size of the queue", vbOKOnly Or vbExclamation, "error")
            txtQueueLength.Focus()
            Exit Sub
        End If

        Dim nMaxUntilStop As Integer = -1
        If cbKeepRestarting.IsChecked Then
            Try
                nMaxUntilStop = CInt(txtMaxUntilStop.Text)
            Catch

            End Try
            If nMaxUntilStop < 0 Then
                mbStopping = True
                mbStopped = True
                MsgBox("enter a valid maximum number of songs before stopping", vbOKOnly Or vbExclamation, "error")
                txtMaxUntilStop.Focus()
                Exit Sub
            End If
        End If

        If nLikedSongsSize <= nQueueLength + 1 Then
            mbStopping = True
            mbStopped = True
            MsgBox("Please enter a Queue that's at least two songs shorter than the length of the list", vbOKOnly Or vbExclamation, "error")
            txtQueueLength.Focus()
            Exit Sub
        End If

        'Create the "Play List". 
        Dim oLikedSongs As Dictionary(Of Integer, SongItem) = New Dictionary(Of Integer, SongItem)
        goRandom = New Random()

        'Populate the "Play list"
        Dim nCount As Integer = nLikedSongsSize
        While nCount > 0
            Dim oLikedSong As SongItem = New SongItem
            oLikedSong.IndexNumber = nCount
            oLikedSong.PlayedTimes = 0
            oLikedSongs.Add(nCount, oLikedSong)
            nCount = nCount - 1
        End While

        Dim oThread As System.Threading.Thread = Nothing
        If cbKeepRestarting.IsChecked Then
            oThread = New System.Threading.Thread(Sub()
                                                      ShuffleRepeats(oLikedSongs:=oLikedSongs, nLikedSongsSize:=nLikedSongsSize, nQueueLength:=nQueueLength, nMaxUntilStop:=nMaxUntilStop, oDispatcher:=Application.Current.Dispatcher)
                                                  End Sub)
        Else
            oThread = New System.Threading.Thread(Sub()
                                                      ShuffleThem(oLikedSongs:=oLikedSongs, nLikedSongsSize:=nLikedSongsSize, nQueueLength:=nQueueLength, oDispatcher:=Application.Current.Dispatcher)
                                                  End Sub)
        End If
        oThread.SetApartmentState(System.Threading.ApartmentState.STA)
        oThread.Start()


    End Sub

    Private Sub ShuffleThem(ByVal oLikedSongs As Dictionary(Of Integer, SongItem),
                            ByVal nLikedSongsSize As Integer,
                            ByVal nQueueLength As Integer,
                            ByVal oDispatcher As System.Windows.Threading.Dispatcher)

        Dim dTotal As Double
        Dim dCount As Double

        'Keep simulating "shuffled play lists" until the user hits Stop
        While True
            'Next Simulation
            dCount = dCount + 1

            'Copy the Play List into a "running" play list. This ensures that with every Simulation we start with all songs yet "unplayed"
            Dim oCopyList As Dictionary(Of Integer, SongItem)
            oCopyList = New Dictionary(Of Integer, SongItem)
            For Each oSongItem As SongItem In oLikedSongs.Values
                Dim oCopy As SongItem = New SongItem
                oCopy.IndexNumber = oSongItem.IndexNumber
                oCopy.PlayedTimes = 0
                oCopyList.Add(oCopy.IndexNumber, oCopy)
            Next

            'Create the first Queue of 80 items
            Dim oQueue As List(Of SongItem) = New List(Of SongItem)
            Dim oPlayedSongs As Dictionary(Of Integer, SongItem) = New Dictionary(Of Integer, SongItem)

            'Now we take the first N items out of the play list and move them to the queue.
            'For example if the Play List contains 1000 "songs" and the Queue size is set to 80,
            'it will move 80 random "songs" from the (Copied) Play List to the Queue, after which
            'the queue contains 80 "songs" and the (Copied) Play List contains the other 920 "songs"
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

            'Now run the simulation
            Dim nSongsPlayedUntilAPreviouslyPlayedSongIsPlayedAgain As Integer = 0
            While True
                'Count the number of "songs" "played" so far
                nSongsPlayedUntilAPreviouslyPlayedSongIsPlayedAgain = nSongsPlayedUntilAPreviouslyPlayedSongIsPlayedAgain + 1

                '"Play" the first "song" in the queue
                Dim oSong As SongItem = oQueue.Item(0)
                If oSong.PlayedTimes > 0 Then
                    'If this song has already been played, end the simulation. With the Count we did above we now know how many
                    '"song"s were "played" in the current Simulation before a "song" that had already been "played" was "played" again
                    Exit While
                End If

                'Otherwise, flag this "song" as "played" ...
                oSong.PlayedTimes = oSong.PlayedTimes + 1

                '... remove it from the first position in the Queue ...
                oQueue.RemoveAt(0)

                '... add it back to the end of the Copied "play list" ...
                oCopyList.Add(oSong.IndexNumber, oSong)

                '... get a random "song" out of the Copied Play List (remember, this contains the "song"s that are currently not in the queue) ...
                nMax = oCopyList.Count
                Dim nSongNumber As Integer = goRandom.Next(minValue:=0, maxValue:=nMax - 1)
                oSong = oCopyList.Item(oCopyList.Keys(nSongNumber))

                '... remove the "song" from the Copied Play List ...
                oCopyList.Remove(oSong.IndexNumber)

                '... and add it to the end of the Queue
                oQueue.Add(oSong)
            End While

            'Add the result of this simulation to the total, and dTotal/dCount will give you the average number of "song"s that were "played"
            'before the simulation picked up a song that had already been "played" before
            dTotal = dTotal + nSongsPlayedUntilAPreviouslyPlayedSongIsPlayedAgain
            oDispatcher.Invoke(Sub()
                                   'And update the display with the results
                                   rSimulations.Text = CInt(Math.Round(CDec(dCount), 0)).ToString()
                                   rSongsPlayed.Text = nSongsPlayedUntilAPreviouslyPlayedSongIsPlayedAgain.ToString
                                   rAverage.Text = (dTotal / dCount).ToString()
                               End Sub)

            'If the user hit Stop, end this procedure
            If mbStopping Then
                mbStopped = True
                Exit Sub
            End If

            'Otherwise, do the next Simulation with the same parameters.
        End While

    End Sub

    Private Sub ShuffleRepeats(ByVal oLikedSongs As Dictionary(Of Integer, SongItem),
                               ByVal nLikedSongsSize As Integer,
                               ByVal nQueueLength As Integer,
                               ByVal nMaxUntilStop As Integer,
                               ByVal oDispatcher As System.Windows.Threading.Dispatcher)

        Dim nMaxRepeats As Integer
        Dim nMaxMaxRepeats As Integer = 0
        Dim dCount As Double = 0
        Dim dTotal As Double = 0
        Dim nRunsPerSim As Integer

        While True
            'Next Simulation
            dCount = dCount + 1
            nMaxRepeats = 0

            For Each oSongItem As SongItem In oLikedSongs.Values
                oSongItem.PlayedTimes = 0
            Next

            nRunsPerSim = 10

            'Keep simulating "shuffled play lists" until the user hits Stop
            While nRunsPerSim > 0
                nRunsPerSim = nRunsPerSim - 1

                'Copy the Play List into a "running" play list. This ensures that with every Simulation we start with all songs yet "unplayed"
                Dim oCopyList As Dictionary(Of Integer, SongItem)
                oCopyList = New Dictionary(Of Integer, SongItem)
                For Each oSongItem As SongItem In oLikedSongs.Values
                    Dim oCopy As SongItem = New SongItem
                    oCopy.IndexNumber = oSongItem.IndexNumber
                    oCopy.PlayedTimes = oSongItem.PlayedTimes
                    oCopyList.Add(oCopy.IndexNumber, oCopy)
                Next

                'Create the first Queue of 80 items
                Dim oQueue As List(Of SongItem) = New List(Of SongItem)
                Dim oPlayedSongs As Dictionary(Of Integer, SongItem) = New Dictionary(Of Integer, SongItem)

                'Now we take the first N items out of the play list and move them to the queue.
                'For example if the Play List contains 1000 "songs" and the Queue size is set to 80,
                'it will move 80 random "songs" from the (Copied) Play List to the Queue, after which
                'the queue contains 80 "songs" and the (Copied) Play List contains the other 920 "songs"
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

                'Now run the simulation
                Dim nNumberOfSongsToPlay = goRandom.Next(minValue:=1, maxValue:=nMaxUntilStop)
                While nNumberOfSongsToPlay > 0
                    'Count the number of "songs" "played" so far
                    nNumberOfSongsToPlay = nNumberOfSongsToPlay - 1

                    '"Play" the first "song" in the queue
                    Dim oSong As SongItem = oQueue.Item(0)

                    'Flag this "song" as "played" ...
                    oSong.PlayedTimes = oSong.PlayedTimes + 1

                    'Reflect this in the original list of liked songs
                    oLikedSongs.Item(oSong.IndexNumber).PlayedTimes = oSong.PlayedTimes

                    If oSong.PlayedTimes > nMaxRepeats Then
                        nMaxRepeats = oSong.PlayedTimes
                    End If

                    '... remove it from the first position in the Queue ...
                    oQueue.RemoveAt(0)

                    '... add it back to the end of the Copied "play list" ...
                    oCopyList.Add(oSong.IndexNumber, oSong)

                    '... get a random "song" out of the Copied Play List (remember, this contains the "song"s that are currently not in the queue) ...
                    nMax = oCopyList.Count
                    Dim nSongNumber As Integer = goRandom.Next(minValue:=0, maxValue:=nMax - 1)
                    oSong = oCopyList.Item(oCopyList.Keys(nSongNumber))

                    '... remove the "song" from the Copied Play List ...
                    oCopyList.Remove(oSong.IndexNumber)

                    '... and add it to the end of the Queue
                    oQueue.Add(oSong)
                End While
            End While

            dTotal = dTotal + nMaxRepeats
            If nMaxRepeats > nMaxMaxRepeats Then
                nMaxMaxRepeats = nMaxRepeats
            End If

            'Add the result of this simulation to the total, and dTotal/dCount will give you the average number of "song"s that were "played"
            'before the simulation picked up a song that had already been "played" before
            oDispatcher.Invoke(Sub()
                                   'And update the display with the results
                                   rSimulations.Text = CInt(Math.Round(CDec(dCount), 0)).ToString()
                                   rMaxRepeat.Text = nMaxMaxRepeats.ToString()
                                   rAverageRepeat.Text = (dTotal / dCount).ToString()
                               End Sub)

            'If the user hit Stop, end this procedure
            If mbStopping Then
                mbStopped = True
                Exit Sub
            End If

            'Otherwise, do the next Simulation with the same parameters.
        End While

    End Sub

    Private Sub btnStop_Click(sender As Object, e As RoutedEventArgs) Handles btnStop.Click
        mbStopping = True
    End Sub

    Private Sub MainWindow_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        If mbStopping = False Then
            mbStopping = True
            e.Cancel = True
        ElseIf mbStopped = False Then
            e.Cancel = True
        End If
    End Sub

    Private Sub cbKeepRestarting_Click(sender As Object, e As RoutedEventArgs) Handles cbKeepRestarting.Click
        If cbKeepRestarting.IsChecked Then
            tbMaxUntilStop.Visibility = Visibility.Visible
            txtMaxUntilStop.Visibility = Visibility.Visible
        Else
            tbMaxUntilStop.Visibility = Visibility.Collapsed
            txtMaxUntilStop.Visibility = Visibility.Collapsed
        End If
    End Sub
End Class

Public Class SongItem
    Public Property IndexNumber As Integer
    Public Property PlayedTimes As Integer
End Class