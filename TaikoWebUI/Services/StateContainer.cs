namespace TaikoWebUI.Services
{
    public class StateContainer
    {
        private bool? _isConnected;
        private User? _currentUser;

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

        public event Action? OnChange;

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
