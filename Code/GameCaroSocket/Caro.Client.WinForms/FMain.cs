using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Text;
using System.Windows.Forms;

namespace Caro.Client.WinForms
{
    public partial class FMain : Form
    {
        // Mỗi màn hình chỉ tạo một instance.
        private readonly HomeView _homeView = new();
        private readonly OnlinePlayersView _onlinePlayersView = new();
        private readonly InvitationsView _invitationsView = new();
        private readonly MatchesView _matchesView = new();
        private readonly GameView _gameView = new();

        private readonly Dictionary<string, UserControl> _pages = new();

        private readonly int _uiThreadId =
            Environment.CurrentManagedThreadId;

        private bool _connected;
        private bool _hasRoom;
        private bool _canMove;
        private bool _sessionBusy;

        private int _onlinePlayersCount;
        private int _activeMatchesCount;
        private int _pendingInvitationsCount;

        // Các sự kiện để module bên ngoài đăng ký xử lý.

        public event Action? OnlinePlayersRequested;
        public event Action? InvitationsRequested;
        public event Action? MatchesRequested;
        public event Action? ConnectRequested;
        public event Action? DisconnectRequested;

        public event Action<Guid>? ChallengeRequested;
        public event Action<Guid>? WatchMatchRequested;

        public event Action? ReadyRequested;
        public event Action? RematchRequested;
        public event Action? SurrenderRequested;
        public event Action<int, int>? MoveRequested;
        public event Action? LeaveRoomRequested;

        public InvitationsView InvitationsPage => _invitationsView;

        public FMain()
        {
            InitializeComponent();

            try
            {
                string logoPath = System.IO.Path.Combine(Application.StartupPath, "Resources", "logo.jpg");
                if (System.IO.File.Exists(logoPath))
                {
                    pictureBox1.Image = Image.FromFile(logoPath);
                }
            }
            catch { }

            RegisterPages();
            RegisterViewEvents();

            InitializeConnectionControls();
            ConfigureInteractionStyles();
            SetRoomAvailability(false);
            UpdateHomeStatistics();

            NavigateTo("Home", reloadData: false);
        }

        // --------------------------------------------------
        // Đăng ký các màn hình
        // --------------------------------------------------

        private void RegisterPages()
        {
            AddPage("Home", _homeView);
            AddPage("OnlinePlayers", _onlinePlayersView);
            AddPage("Invitations", _invitationsView);
            AddPage("Matches", _matchesView);
            AddPage("Game", _gameView);
        }

        private void AddPage(string name, UserControl page)
        {
            page.Dock = DockStyle.Fill;
            page.Visible = false;

            _pages.Add(name, page);
            pnlContent.Controls.Add(page);
        }

        // --------------------------------------------------
        // Nối sự kiện từ các UserControl
        // --------------------------------------------------

        private void RegisterViewEvents()
        {
            btnConnect.Click += (_, _) => ConnectRequested?.Invoke();
            btnDisconnect.Click += (_, _) => DisconnectRequested?.Invoke();

            // HomeView
            _homeView.ViewPlayersRequested += () =>
                NavigateTo("OnlinePlayers");

            _homeView.ViewMatchesRequested += () =>
                NavigateTo("Matches");

            _homeView.ViewInvitationsRequested += () =>
                NavigateTo("Invitations");

            // OnlinePlayersView
            _onlinePlayersView.RefreshRequested +=
                RequestOnlinePlayers;

            _onlinePlayersView.ChallengeRequested += playerId =>
            {
                if (!EnsureConnected())
                    return;

                if (_hasRoom)
                {
                    DisplayStatus("Hãy rời phòng hiện tại trước khi thách đấu.");
                    return;
                }

                if (ChallengeRequested is null)
                {
                    DisplayStatus("Chức năng thách đấu chưa sẵn sàng.");
                    return;
                }

                ChallengeRequested.Invoke(playerId);
            };

            // InvitationsView
            _invitationsView.RefreshRequested +=
                RequestInvitations;

            // MatchesView
            _matchesView.RefreshRequested += RequestMatches;

            _matchesView.WatchMatchRequested += roomId =>
            {
                if (!EnsureConnected())
                    return;

                if (_hasRoom)
                {
                    DisplayStatus("Hãy rời phòng hiện tại trước khi xem trận khác.");
                    return;
                }

                if (WatchMatchRequested is null)
                {
                    DisplayStatus("Chức năng xem trận chưa sẵn sàng.");
                    return;
                }

                // Chờ server xác nhận rồi mới gọi OpenRoom().
                WatchMatchRequested.Invoke(roomId);
            };

            // GameView
            _gameView.ReadyRequested += () =>
            {
                if (!EnsureConnected() || !_hasRoom)
                    return;

                if (ReadyRequested is null)
                {
                    DisplayStatus("Chức năng sẵn sàng chưa khả dụng.");
                    return;
                }

                ReadyRequested.Invoke();
            };

            _gameView.RematchRequested += () =>
            {
                if (!EnsureConnected() || !_hasRoom) return;
                RematchRequested?.Invoke();
            };

            _gameView.SurrenderRequested += () =>
            {
                if (!EnsureConnected() || !_hasRoom) return;

                if (SurrenderRequested is null)
                {
                    DisplayStatus("Chức năng đầu hàng chưa khả dụng.");
                    return;
                }

                SurrenderRequested.Invoke();
            };

            _gameView.MoveRequested += (row, column) =>
            {
                if (!EnsureConnected() || !_hasRoom)
                    return;

                if (MoveRequested is null)
                {
                    // GameView đã khóa ô sau khi người dùng nhấn.
                    // Mở lại nếu chưa có module tiếp nhận thao tác.
                    _gameView.SetBoardInteraction(_canMove);
                    DisplayStatus("Chưa thể gửi nước đi.");
                    return;
                }

                MoveRequested.Invoke(row, column);
            };

            _gameView.LeaveRequested += () =>
            {
                if (!EnsureConnected() || !_hasRoom)
                    return;

                if (LeaveRoomRequested is null)
                {
                    DisplayStatus("Chưa thể gửi yêu cầu rời phòng.");
                    return;
                }

                // Chờ server xác nhận rồi mới gọi CloseRoom().
                LeaveRoomRequested.Invoke();
            };
        }

        // --------------------------------------------------
        // Các handler đã được gắn trong MainForm.Designer.cs
        // --------------------------------------------------

        private void NavigationButton_Click(object? sender, EventArgs e)
        {
            if (sender is Button button &&
                button.Tag is string pageName)
            {
                NavigateTo(pageName);
            }
        }

        // --------------------------------------------------
        // Điều hướng
        // --------------------------------------------------

        private void NavigateTo(string pageName, bool reloadData = true)
        {
            if (!_pages.TryGetValue(pageName, out var page))
                return;

            if (pageName == "Game" && !_hasRoom)
            {
                DisplayStatus("Bạn chưa tham gia phòng chơi.");
                return;
            }

            foreach (var item in _pages.Values)
            {
                item.Visible = false;
            }

            page.Visible = true;
            page.BringToFront();

            HighlightNavigation(pageName);

            if (!reloadData)
                return;

            switch (pageName)
            {
                case "OnlinePlayers":
                    RequestOnlinePlayers();
                    break;

                case "Invitations":
                    RequestInvitations();
                    break;

                case "Matches":
                    RequestMatches();
                    break;
            }
        }

        private void HighlightNavigation(string pageName)
        {
            foreach (Control control in flpNavigation.Controls)
            {
                if (control is not Button button ||
                    button.Tag is not string buttonPage)
                {
                    continue;
                }

                bool selected = buttonPage == pageName;

                button.BackColor = selected
                    ? Color.FromArgb(235, 242, 255)
                    : Color.White;

                button.ForeColor = selected
                    ? Color.FromArgb(37, 99, 235)
                    : Color.FromArgb(23, 43, 77);
            }
        }

        // --------------------------------------------------
        // Yêu cầu tải dữ liệu
        // --------------------------------------------------

        private void RequestOnlinePlayers()
        {
            if (!_connected)
            {
                _onlinePlayersView.DisplayError(
                    "Hãy kết nối máy chủ để tải danh sách người chơi.");
                return;
            }

            if (OnlinePlayersRequested is null)
            {
                _onlinePlayersView.DisplayError(
                    "Chưa thể tải danh sách người chơi.");
                return;
            }

            _onlinePlayersView.DisplayLoading();

            OnlinePlayersRequested.Invoke();
        }

        private void RequestInvitations()
        {
            if (!EnsureConnected())
                return;

            if (InvitationsRequested is null)
            {
                DisplayStatus("Chưa thể tải danh sách lời mời.");
                return;
            }

            DisplayStatus("Đang tải danh sách lời mời...");

            InvitationsRequested.Invoke();
        }

        private void RequestMatches()
        {
            if (!_connected)
            {
                _matchesView.DisplayError(
                    "Hãy kết nối máy chủ để tải danh sách trận đấu.");
                return;
            }

            if (MatchesRequested is null)
            {
                _matchesView.DisplayError(
                    "Chưa thể tải danh sách trận đấu.");
                return;
            }

            _matchesView.DisplayLoading();

            MatchesRequested.Invoke();
        }

        private bool EnsureConnected()
        {
            if (_connected)
                return true;

            DisplayStatus("Bạn chưa kết nối máy chủ.");
            return false;
        }

        // --------------------------------------------------
        // Các hàm cho module xử lý cập nhật giao diện
        // --------------------------------------------------

        public void DisplayConnection(
            bool connected,
            string nickname = "",
            string serverAddress = "")
        {
            RunOnUi(() =>
            {
                _connected = connected;
                btnConnect.Enabled = !connected && !_sessionBusy;
                btnDisconnect.Enabled = connected && !_sessionBusy;
                UpdateConnectionBadge(connected,
                    connected ? "Đã kết nối" : "Mất kết nối");
                lblNickname.Text = connected ? nickname : "—";
                lblServer.Text = connected ? serverAddress : "—";

                _homeView.DisplayNickname(
                    connected ? nickname : "bạn");

                if (!connected)
                {
                    SetRoomAvailability(false);
                    _canMove = false;

                    _gameView.SetBoardInteraction(false);

                    _onlinePlayersView.DisplayPlayers(
                        Array.Empty<OnlinePlayerRow>());

                    _matchesView.DisplayMatches(
                        Array.Empty<MatchRow>());

                    _invitationsView.DisplayInvitations(
                        Array.Empty<InvitationRow>());

                    _onlinePlayersCount = 0;
                    _activeMatchesCount = 0;
                    _pendingInvitationsCount = 0;

                    UpdateHomeStatistics();

                    NavigateTo("Home", reloadData: false);
                }

            });
        }

        private void InitializeConnectionControls()
        {
            lblConnection.Visible = true;
            lblNickname.Visible = true;
            lblServer.Visible = true;
            btnConnect.Visible = true;
            btnDisconnect.Visible = true;
            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
            UpdateConnectionBadge(false, "Chưa kết nối");
        }

        private void UpdateConnectionBadge(bool connected, string text)
        {
            lblConnection.Text = connected ? $"●  {text}" : $"○  {text}";
            lblConnection.ForeColor = connected
                ? Color.FromArgb(21, 128, 61)
                : Color.FromArgb(100, 116, 139);
            lblConnection.BackColor = connected
                ? Color.FromArgb(240, 253, 244)
                : Color.FromArgb(241, 245, 249);
        }

        private void ConfigureInteractionStyles()
        {
            foreach (Control control in flpNavigation.Controls)
            {
                if (control is not Button button) continue;
                button.Cursor = Cursors.Hand;
                button.FlatAppearance.MouseOverBackColor = Color.FromArgb(239, 246, 255);
                button.FlatAppearance.MouseDownBackColor = Color.FromArgb(219, 234, 254);
            }

            btnDisconnect.Cursor = Cursors.Hand;
            btnDisconnect.FlatAppearance.MouseOverBackColor = Color.FromArgb(254, 242, 242);
            btnDisconnect.FlatAppearance.MouseDownBackColor = Color.FromArgb(254, 226, 226);
        }

        private void SetRoomAvailability(bool hasRoom)
        {
            _hasRoom = hasRoom;
            btnGame.Enabled = hasRoom;
            btnGame.Cursor = hasRoom ? Cursors.Hand : Cursors.Default;
        }

        public void DisplaySessionBusy(bool busy)
        {
            RunOnUi(() =>
            {
                _sessionBusy = busy;
                UseWaitCursor = busy;
                btnConnect.Enabled = !busy && !_connected;
                btnDisconnect.Enabled = !busy && _connected;
            });
        }

        public void DisplayOnlinePlayers(
            IEnumerable<OnlinePlayerRow> players)
        {
            // Sao chép danh sách trước khi đưa sang UI thread.
            var snapshot = players.ToArray();

            RunOnUi(() =>
            {
                if (!_connected)
                    return;

                _onlinePlayersView.DisplayPlayers(snapshot);

                _onlinePlayersCount = snapshot.Length;
                UpdateHomeStatistics();

            });
        }

        public void DisplayMatches(IEnumerable<MatchRow> matches)
        {
            var snapshot = matches.ToArray();

            RunOnUi(() =>
            {
                if (!_connected)
                    return;

                _matchesView.DisplayMatches(snapshot);

                _activeMatchesCount = snapshot.Length;
                UpdateHomeStatistics();

            });
        }

        public void DisplayInvitationCount(int count)
        {
            RunOnUi(() =>
            {
                if (!_connected)
                    return;

                _pendingInvitationsCount = Math.Max(0, count);
                UpdateHomeStatistics();
            });
        }

        public void DisplayOnlinePlayersError(string message)
        {
            RunOnUi(() =>
                _onlinePlayersView.DisplayError(message));
        }

        public void DisplayOnlinePlayersLoading()
        {
            RunOnUi(() => { if (_connected) _onlinePlayersView.DisplayLoading(); });
        }

        public void DisplayMatchesError(string message)
        {
            RunOnUi(() =>
                _matchesView.DisplayError(message));
        }

        private void UpdateHomeStatistics()
        {
            _homeView.DisplayStatistics(
                _onlinePlayersCount,
                _activeMatchesCount,
                _pendingInvitationsCount);
        }

        // Gọi khi server xác nhận đã vào phòng.
        // Sau đó cập nhật board, lượt, trạng thái ready qua các hàm bên dưới.
        public void OpenRoom(
            string playerX,
            string playerO,
            string status)
        {
            RunOnUi(() =>
            {
                if (!_connected)
                    return;

                SetRoomAvailability(true);
                _canMove = false;

                _gameView.DisplayRoom(
                    playerX,
                    playerO,
                    status);

                _gameView.DisplayBoard(
                    new string?[15, 15],
                    canMove: false);

                _gameView.DisplayReadyState(
                    isReady: false,
                    canReady: false);

                _gameView.DisplayTurn("Chờ bắt đầu", 0);
                _gameView.DisplaySpectators(0);
                _gameView.DisplayHint(string.Empty);

                NavigateTo("Game", reloadData: false);

            });
        }

        public void DisplayGameBoard(string?[,] board, bool canMove)
        {
            var snapshot = (string?[,])board.Clone();

            RunOnUi(() =>
            {
                if (!_connected || !_hasRoom)
                    return;

                _canMove = canMove;
                _gameView.DisplayBoard(snapshot, canMove);

            });
        }

        public void DisplayGameTurn(string message, int remainingSeconds, string activeSymbol = "")
        {
            RunOnUi(() =>
            {
                if (!_hasRoom)
                    return;

                _gameView.DisplayTurn(message, remainingSeconds);
                _gameView.DisplayActivePlayer(activeSymbol);
            });
        }

        public void DisplayReadyState(bool isReady, bool canReady)
        {
            RunOnUi(() =>
            {
                if (!_hasRoom)
                    return;

                _gameView.DisplayReadyState(isReady, canReady);
            });
        }

        public void OpenPendingRoom()
        {
            RunOnUi(() =>
            {
                if (!_connected || _hasRoom) return;
                SetRoomAvailability(false);
            });
        }

        public void ClosePendingRoom()
        {
            RunOnUi(() =>
            {
                SetRoomAvailability(false);
                _canMove = false;
                _gameView.SetBoardInteraction(false);
                NavigateTo("Invitations", reloadData: false);
            });
        }

        public void DisplayMove(int row, int column, string symbol, bool canMove)
        {
            RunOnUi(() =>
            {
                if (!_connected || !_hasRoom) return;
                _canMove = canMove;
                _gameView.DisplayMove(row, column, symbol, canMove);
            });
        }

        public void DisplayRematchState(bool canRequest, bool accepted)
        {
            RunOnUi(() =>
            {
                if (_hasRoom) _gameView.DisplayRematchState(canRequest, accepted);
            });
        }

        public void DisplaySurrenderState(bool canSurrender)
        {
            RunOnUi(() =>
            {
                if (_hasRoom) _gameView.DisplaySurrenderState(canSurrender);
            });
        }

        public void DisplayGameActionMode(GameActionMode mode)
        {
            RunOnUi(() =>
            {
                if (_hasRoom) _gameView.DisplayActionMode(mode);
            });
        }

        public void DisplaySpectators(int count)
        {
            RunOnUi(() =>
            {
                if (!_hasRoom)
                    return;

                _gameView.DisplaySpectators(count);
            });
        }

        public void DisplayGameHint(string message)
        {
            RunOnUi(() =>
            {
                if (!_hasRoom)
                    return;

                _gameView.DisplayHint(message);
            });
        }

        // Module xử lý quyết định có cho đánh lại hay không.
        public void DisplayMoveError(string message, bool canMove)
        {
            RunOnUi(() =>
            {
                if (!_connected || !_hasRoom)
                    return;

                _canMove = canMove;
                _gameView.SetBoardInteraction(canMove);

            });
        }

        // Gọi sau khi server xác nhận đã rời phòng.
        public void CloseRoom()
        {
            RunOnUi(() =>
            {
                SetRoomAvailability(false);
                _canMove = false;

                _gameView.SetBoardInteraction(false);
                _gameView.DisplayReadyState(isReady: false, canReady: false);
                _gameView.DisplayTurn("Chờ bắt đầu", 0);

                NavigateTo("Home", reloadData: false);

            });
        }

        public void DisplayStatus(string _) { }

        public void ShowMessage(string message, string title)
        {
            RunOnUi(() =>
            {
                MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
        }

        public void ShowGameOver(string title, string reason, bool isWin)
        {
            RunOnUi(() =>
            {
                using var dialog = new Caro.Client.WinForms.Features.Game.GameOverDialog(title, reason, isWin);
                dialog.ShowDialog(this);
            });
        }

        // --------------------------------------------------
        // Đưa cập nhật từ module nhận dữ liệu về UI thread
        // --------------------------------------------------

        private void RunOnUi(Action action)
        {
            if (IsDisposed || Disposing)
                return;

            if (Environment.CurrentManagedThreadId == _uiThreadId)
            {
                action();
                return;
            }

            if (!IsHandleCreated)
                return;

            try
            {
                BeginInvoke((Action)(() =>
                {
                    if (!IsDisposed && !Disposing)
                    {
                        action();
                    }
                }));
            }
            catch (InvalidOperationException)
            {
                // Form đã đóng trong lúc cập nhật được xếp hàng.
            }
        }


    }
}
