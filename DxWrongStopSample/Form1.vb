Imports System.Threading
Imports DxLibDLL

Public Class Form1
    Private isEnd As Boolean
    Private targetForm As Form
    Private drawThread As Thread
    Private pictureHandle As Integer

    'DxLibのオプション設定と初期化
    Private Sub InitDxLib(ByRef handle As Integer)
        DX.SetUserWindow(handle) 'ハンドルセット
        'ウィンドウがアクティブじゃない時も処理を続ける
        DX.SetAlwaysRunFlag(DX.TRUE)
        'DxLib_Init()を呼んだwindow以外からも操作を可能にする
        DX.SetMultiThreadFlag(DX.TRUE)
        'pngなどの透過チャンネルを有効に設定
        DX.SetMovieColorA8R8G8B8Flag(DX.TRUE)
        '一度裏画面に描画を行い、Flipすると描画が行われるように設定（ダブルバッファリング）
        DX.SetDrawScreen(DX.DX_SCREEN_BACK)
        '2枚以上の画面にScreen.flipする場合デフォルトではVSync待ちで遅くなるのでVSyncは切る
        DX.SetWaitVSyncFlag(DX.FALSE)
        'これが無いとプログラム本体のIME関連動作がおかしくなる
        DX.SetUseIMEFlag(DX.TRUE)
        '透過色を指定しない、これが無いとデフォルトでは黒が透過されてしまう
        DX.SetUseTransColor(DX.FALSE)

        If DX.DxLib_Init() = -1 Then MsgBox("初期化に失敗しました。", MsgBoxStyle.Critical)
    End Sub

    '描画ループ
    Private Sub DrawLoop()
        targetForm = New Form
        With targetForm
            .FormBorderStyle = FormBorderStyle.None '枠無
            .Size = New Size(192, 108)
            .Show() '表示
            .DesktopLocation = New Point(0, 0)
        End With
        Dim formHandle = targetForm.Handle

        InitDxLib(formHandle)
        Dim i As Integer = 0
        Dim sw = Stopwatch.StartNew()
        Dim color = DX.GetColor(100, 0, 0)

        While Not isEnd
            sw.Restart()

            DX.SetDrawScreen(formHandle)
            DX.ClearDrawScreen() '描画先初期化

            i += 1
            If i = 100 Then i = 0
            DX.DrawBox(i, 0, 100, 100, color, DX.TRUE)

            DX.SetDrawScreen(DX.DX_SCREEN_BACK) '描画先を裏画面へ
            DX.DrawGraph(0, 0, formHandle, DX.TRUE) '裏画面へ描画
            DX.ScreenFlip() 'メインウィンドウへ表示
            'サブウィンドウへ表示し、その後メインウィンドウへ処理を戻す
            DX.DrawGraph(0, 0, pictureHandle, DX.TRUE)
            DX.SetScreenFlipTargetWindow(pictureHandle) '描画先をサブ画面へ
            DX.ScreenFlip() 'サブ画面へ描画
            '後処理
            DX.ClearDrawScreen() '裏画面クリア
            DX.SetScreenFlipTargetWindow(0) '描画先をメイン画面(0番のハンドルの画面がそれ)へ戻す

            sw.Stop()
            Thread.Sleep(Math.Max(0, 16 - sw.ElapsedMilliseconds)) '簡単な垂直同期
        End While
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        isEnd = False
        targetForm = New Form
        With targetForm
            .FormBorderStyle = FormBorderStyle.None '枠無
            .Size = New Size(192, 108)
            .Show() '表示
            .DesktopLocation = New Point(0, 0)
        End With

        pictureHandle = PictureBox1.Handle

        drawThread = New Thread(New ParameterizedThreadStart(AddressOf DrawLoop)) With {
            .IsBackground = True
        }
        drawThread.Start(targetForm.Handle)
    End Sub
End Class
