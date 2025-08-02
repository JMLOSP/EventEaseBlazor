using EventEase.Models;
using Microsoft.JSInterop;
using System.Text.Json;

namespace EventEase.Services
{
    public interface IUserSessionService
    {
        UserSession? CurrentSession { get; }
        event Action<UserSession?>? SessionChanged;
        
        Task<bool> LoginAsync(string email, string password);
        Task LogoutAsync();
        Task<bool> RegisterUserAsync(UserRegistrationModel registration);
        Task UpdateSessionActivityAsync();
        Task<UserProfile?> GetUserProfileAsync(string userId);
        Task<bool> UpdateUserProfileAsync(UserProfile profile);
        bool IsSessionValid();
        Task<T?> GetSessionDataAsync<T>(string key);
        Task SetSessionDataAsync<T>(string key, T value);
        Task ClearSessionDataAsync(string key);
        Task<UserSession?> GetCurrentSessionAsync();
    }

    public class UserSessionService : IUserSessionService
    {
        private readonly IJSRuntime _jsRuntime;
        private UserSession? _currentSession;
        private readonly Dictionary<string, UserProfile> _userProfiles = new();
        private readonly TimeSpan _sessionTimeout = TimeSpan.FromMinutes(30);

        public UserSession? CurrentSession => _currentSession;
        public event Action<UserSession?>? SessionChanged;

        public UserSessionService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
            InitializeDemoUsers();
        }

        private void InitializeDemoUsers()
        {
            // Add some demo users for testing
            _userProfiles["demo@example.com"] = new UserProfile
            {
                UserId = "demo-user-1",
                FirstName = "Juan",
                LastName = "Pérez",
                Email = "demo@example.com",
                Phone = "+34 123 456 789",
                DateOfBirth = new DateTime(1990, 5, 15),
                Address = "Calle Mayor 123, Madrid"
            };

            _userProfiles["admin@eventease.com"] = new UserProfile
            {
                UserId = "admin-user-1",
                FirstName = "María",
                LastName = "García",
                Email = "admin@eventease.com",
                Phone = "+34 987 654 321",
                DateOfBirth = new DateTime(1985, 8, 22),
                Address = "Avenida Libertad 456, Barcelona"
            };
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            try
            {
                // Simulate authentication (in a real app, you'd validate against a database)
                await Task.Delay(500); // Simulate network delay

                // Demo authentication logic
                if (_userProfiles.ContainsKey(email) && 
                    (password == "Demo123!" || password == "Admin123!"))
                {
                    var profile = _userProfiles[email];
                    _currentSession = new UserSession
                    {
                        UserId = profile.UserId,
                        Email = email,
                        FullName = $"{profile.FirstName} {profile.LastName}",
                        IsAuthenticated = true
                    };

                    await SaveSessionToLocalStorageAsync();
                    SessionChanged?.Invoke(_currentSession);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            _currentSession = null;
            await ClearSessionFromLocalStorageAsync();
            SessionChanged?.Invoke(null);
        }

        public async Task<bool> RegisterUserAsync(UserRegistrationModel registration)
        {
            try
            {
                // Simulate registration process
                await Task.Delay(1000);

                // Check if user already exists
                if (_userProfiles.ContainsKey(registration.Email))
                {
                    return false; // User already exists
                }

                // Create new user profile
                var profile = new UserProfile
                {
                    UserId = Guid.NewGuid().ToString(),
                    FirstName = registration.FirstName,
                    LastName = registration.LastName,
                    Email = registration.Email,
                    Phone = registration.Phone,
                    DateOfBirth = registration.DateOfBirth,
                    Address = registration.Address
                };

                _userProfiles[registration.Email] = profile;

                // Auto-login after registration
                _currentSession = new UserSession
                {
                    UserId = profile.UserId,
                    Email = registration.Email,
                    FullName = registration.FullName,
                    IsAuthenticated = true
                };

                await SaveSessionToLocalStorageAsync();
                SessionChanged?.Invoke(_currentSession);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task UpdateSessionActivityAsync()
        {
            if (_currentSession != null)
            {
                _currentSession.UpdateActivity();
                await SaveSessionToLocalStorageAsync();
            }
        }

        public async Task<UserProfile?> GetUserProfileAsync(string userId)
        {
            await Task.Delay(100); // Simulate async operation
            return _userProfiles.Values.FirstOrDefault(p => p.UserId == userId);
        }

        public async Task<bool> UpdateUserProfileAsync(UserProfile profile)
        {
            try
            {
                await Task.Delay(200); // Simulate async operation
                var existingProfile = _userProfiles.Values.FirstOrDefault(p => p.UserId == profile.UserId);
                if (existingProfile != null)
                {
                    _userProfiles[profile.Email] = profile;
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public bool IsSessionValid()
        {
            return _currentSession?.IsAuthenticated == true && 
                   !_currentSession.IsSessionExpired(_sessionTimeout);
        }

        public Task<T?> GetSessionDataAsync<T>(string key)
        {
            if (_currentSession?.SessionData.TryGetValue(key, out var value) == true)
            {
                if (value is JsonElement jsonElement)
                {
                    return Task.FromResult(JsonSerializer.Deserialize<T>(jsonElement.GetRawText()));
                }
                return Task.FromResult((T?)value);
            }
            return Task.FromResult(default(T?));
        }

        public async Task SetSessionDataAsync<T>(string key, T value)
        {
            if (_currentSession != null)
            {
                _currentSession.SessionData[key] = value!;
                await SaveSessionToLocalStorageAsync();
            }
        }

        public async Task ClearSessionDataAsync(string key)
        {
            if (_currentSession != null)
            {
                _currentSession.SessionData.Remove(key);
                await SaveSessionToLocalStorageAsync();
            }
        }

        private async Task SaveSessionToLocalStorageAsync()
        {
            if (_currentSession != null)
            {
                var sessionJson = JsonSerializer.Serialize(_currentSession);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "userSession", sessionJson);
            }
        }

        private async Task ClearSessionFromLocalStorageAsync()
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "userSession");
        }

        public async Task<bool> RestoreSessionAsync()
        {
            try
            {
                var sessionJson = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "userSession");
                if (!string.IsNullOrEmpty(sessionJson))
                {
                    var session = JsonSerializer.Deserialize<UserSession>(sessionJson);
                    if (session != null && !session.IsSessionExpired(_sessionTimeout))
                    {
                        _currentSession = session;
                        _currentSession.UpdateActivity();
                        SessionChanged?.Invoke(_currentSession);
                        return true;
                    }
                }
            }
            catch
            {
                // Session restoration failed
            }

            return false;
        }

        public Task<UserSession?> GetCurrentSessionAsync()
        {
            return Task.FromResult(_currentSession);
        }
    }
}
