namespace TaikoWebUI.Services
{
    public class StateContainer
    {
        private bool? _isConnected;
        private bool? _isDarkMode;
        private User? _currentUser;
        private string? _currentTitle;

        public bool isConnected
        {
            get => _isConnected ?? false;
            set
            {
                _isConnected = value;
                NotifyStateChanged();
            }
        }

        public User currentUser
        {
            get => _currentUser ?? new User();
            set
            {
                _currentUser = value;
                isConnected = true;
            }
        }

        public bool darkMode
        {
            get => _isDarkMode ?? true;
            set
            {
                _isDarkMode = value;
                NotifyStateChanged();
            }
        }

        public string currentTitle
        {
            get => _currentTitle ?? "";
            set
            {
                _currentTitle = value;
                NotifyStateChanged();
            }
        }

        public event Action? OnChange;

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
