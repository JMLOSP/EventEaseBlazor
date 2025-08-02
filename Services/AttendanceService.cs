using EventEase.Models;
using Microsoft.JSInterop;
using System.Text.Json;

namespace EventEase.Services
{
    public interface IAttendanceService
    {
        Task<bool> RegisterForEventAsync(string eventId, string userId, string userName, string userEmail);
        Task<bool> CheckInAsync(AttendanceCheckInModel checkInModel);
        Task<bool> CheckOutAsync(string eventId, string userEmail);
        Task<bool> MarkNoShowAsync(string eventId, string userEmail);
        Task<bool> CancelRegistrationAsync(string eventId, string userEmail);
        Task<AttendanceRecord?> GetAttendanceRecordAsync(string eventId, string userEmail);
        Task<List<AttendanceRecord>> GetEventAttendanceAsync(string eventId);
        Task<List<AttendanceRecord>> GetUserAttendanceHistoryAsync(string userEmail);
        Task<EventAttendanceInfo?> GetEventAttendanceInfoAsync(string eventId);
        Task<List<AttendanceRecord>> SearchAttendanceAsync(AttendanceSearchFilter filter);
        Task<AttendanceStatistics> GetAttendanceStatisticsAsync();
        Task<bool> UpdateAttendanceNotesAsync(string eventId, string userEmail, string notes);
        Task<List<EventAttendanceInfo>> GetAllEventsAttendanceAsync();
        event Action<AttendanceRecord>? AttendanceChanged;
    }

    public class AttendanceService : IAttendanceService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly Dictionary<string, AttendanceRecord> _attendanceRecords = new();
        private readonly Dictionary<string, EventAttendanceInfo> _eventInfos = new();

        public event Action<AttendanceRecord>? AttendanceChanged;

        public AttendanceService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
            InitializeDemoData();
        }

        private void InitializeDemoData()
        {
            // Create demo events
            var events = new[]
            {
                new EventAttendanceInfo
                {
                    EventId = "event-001",
                    EventName = "Conferencia de Tecnología 2025",
                    EventDate = DateTime.Today.AddDays(7),
                    EventLocation = "Madrid, España",
                    MaxCapacity = 100
                },
                new EventAttendanceInfo
                {
                    EventId = "event-002",
                    EventName = "Workshop de Blazor",
                    EventDate = DateTime.Today.AddDays(14),
                    EventLocation = "Barcelona, España",
                    MaxCapacity = 50
                },
                new EventAttendanceInfo
                {
                    EventId = "event-003",
                    EventName = "Meetup de Desarrolladores",
                    EventDate = DateTime.Today.AddDays(-2),
                    EventLocation = "Valencia, España",
                    MaxCapacity = 75
                }
            };

            foreach (var eventInfo in events)
            {
                _eventInfos[eventInfo.EventId] = eventInfo;
            }

            // Create demo attendance records
            var demoAttendances = new[]
            {
                new AttendanceRecord
                {
                    EventId = "event-001",
                    UserId = "user-001",
                    UserName = "Juan Pérez",
                    UserEmail = "juan@example.com",
                    Status = AttendanceStatus.Registered,
                    RegistrationDate = DateTime.Now.AddDays(-3)
                },
                new AttendanceRecord
                {
                    EventId = "event-001",
                    UserId = "user-002",
                    UserName = "María García",
                    UserEmail = "maria@example.com",
                    Status = AttendanceStatus.Present,
                    RegistrationDate = DateTime.Now.AddDays(-5),
                    CheckInTime = DateTime.Now.AddHours(-2),
                    IsVip = true
                },
                new AttendanceRecord
                {
                    EventId = "event-003",
                    UserId = "user-001",
                    UserName = "Juan Pérez",
                    UserEmail = "juan@example.com",
                    Status = AttendanceStatus.CheckedOut,
                    RegistrationDate = DateTime.Now.AddDays(-10),
                    CheckInTime = DateTime.Now.AddDays(-2).AddHours(1),
                    CheckOutTime = DateTime.Now.AddDays(-2).AddHours(4)
                },
                new AttendanceRecord
                {
                    EventId = "event-003",
                    UserId = "user-003",
                    UserName = "Carlos López",
                    UserEmail = "carlos@example.com",
                    Status = AttendanceStatus.NoShow,
                    RegistrationDate = DateTime.Now.AddDays(-8)
                }
            };

            foreach (var attendance in demoAttendances)
            {
                var key = $"{attendance.EventId}_{attendance.UserEmail}";
                _attendanceRecords[key] = attendance;
            }

            UpdateEventStatistics();
        }

        public async Task<bool> RegisterForEventAsync(string eventId, string userId, string userName, string userEmail)
        {
            try
            {
                var key = $"{eventId}_{userEmail}";
                
                // Check if already registered
                if (_attendanceRecords.ContainsKey(key))
                {
                    return false; // Already registered
                }

                // Check event capacity
                var eventInfo = _eventInfos.GetValueOrDefault(eventId);
                if (eventInfo != null && eventInfo.TotalRegistered >= eventInfo.MaxCapacity)
                {
                    return false; // Event is full
                }

                var record = new AttendanceRecord
                {
                    EventId = eventId,
                    UserId = userId,
                    UserName = userName,
                    UserEmail = userEmail,
                    Status = AttendanceStatus.Registered,
                    RegistrationDate = DateTime.Now
                };

                _attendanceRecords[key] = record;
                UpdateEventStatistics();
                AttendanceChanged?.Invoke(record);
                
                await SaveToLocalStorageAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CheckInAsync(AttendanceCheckInModel checkInModel)
        {
            try
            {
                var key = $"{checkInModel.EventId}_{checkInModel.UserEmail}";
                
                if (!_attendanceRecords.TryGetValue(key, out var record))
                {
                    return false; // Not registered
                }

                if (record.Status == AttendanceStatus.Present)
                {
                    return false; // Already checked in
                }

                record.Status = AttendanceStatus.Present;
                record.CheckInTime = DateTime.Now;
                record.Notes = checkInModel.Notes;
                record.IsVip = checkInModel.IsVip;
                record.SeatNumber = checkInModel.SeatNumber;

                UpdateEventStatistics();
                AttendanceChanged?.Invoke(record);
                
                await SaveToLocalStorageAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CheckOutAsync(string eventId, string userEmail)
        {
            try
            {
                var key = $"{eventId}_{userEmail}";
                
                if (!_attendanceRecords.TryGetValue(key, out var record))
                {
                    return false; // Not registered
                }

                if (record.Status != AttendanceStatus.Present)
                {
                    return false; // Not checked in
                }

                record.Status = AttendanceStatus.CheckedOut;
                record.CheckOutTime = DateTime.Now;

                UpdateEventStatistics();
                AttendanceChanged?.Invoke(record);
                
                await SaveToLocalStorageAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> MarkNoShowAsync(string eventId, string userEmail)
        {
            try
            {
                var key = $"{eventId}_{userEmail}";
                
                if (!_attendanceRecords.TryGetValue(key, out var record))
                {
                    return false; // Not registered
                }

                record.Status = AttendanceStatus.NoShow;
                
                UpdateEventStatistics();
                AttendanceChanged?.Invoke(record);
                
                await SaveToLocalStorageAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CancelRegistrationAsync(string eventId, string userEmail)
        {
            try
            {
                var key = $"{eventId}_{userEmail}";
                
                if (!_attendanceRecords.TryGetValue(key, out var record))
                {
                    return false; // Not registered
                }

                record.Status = AttendanceStatus.Cancelled;
                
                UpdateEventStatistics();
                AttendanceChanged?.Invoke(record);
                
                await SaveToLocalStorageAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public Task<AttendanceRecord?> GetAttendanceRecordAsync(string eventId, string userEmail)
        {
            var key = $"{eventId}_{userEmail}";
            _attendanceRecords.TryGetValue(key, out var record);
            return Task.FromResult(record);
        }

        public Task<List<AttendanceRecord>> GetEventAttendanceAsync(string eventId)
        {
            var records = _attendanceRecords.Values
                .Where(r => r.EventId == eventId)
                .OrderBy(r => r.RegistrationDate)
                .ToList();
            return Task.FromResult(records);
        }

        public Task<List<AttendanceRecord>> GetUserAttendanceHistoryAsync(string userEmail)
        {
            var records = _attendanceRecords.Values
                .Where(r => r.UserEmail == userEmail)
                .OrderByDescending(r => r.RegistrationDate)
                .ToList();
            return Task.FromResult(records);
        }

        public Task<EventAttendanceInfo?> GetEventAttendanceInfoAsync(string eventId)
        {
            _eventInfos.TryGetValue(eventId, out var eventInfo);
            return Task.FromResult(eventInfo);
        }

        public Task<List<AttendanceRecord>> SearchAttendanceAsync(AttendanceSearchFilter filter)
        {
            var query = _attendanceRecords.Values.AsQueryable();

            if (!string.IsNullOrEmpty(filter.EventId))
                query = query.Where(r => r.EventId == filter.EventId);

            if (!string.IsNullOrEmpty(filter.UserEmail))
                query = query.Where(r => r.UserEmail.Contains(filter.UserEmail, StringComparison.OrdinalIgnoreCase));

            if (filter.Status.HasValue)
                query = query.Where(r => r.Status == filter.Status.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(r => r.RegistrationDate >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(r => r.RegistrationDate <= filter.ToDate.Value);

            if (filter.IsVip.HasValue)
                query = query.Where(r => r.IsVip == filter.IsVip.Value);

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(r => 
                    r.UserName.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    r.UserEmail.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase));
            }

            return Task.FromResult(query.ToList());
        }

        public Task<AttendanceStatistics> GetAttendanceStatisticsAsync()
        {
            var stats = new AttendanceStatistics
            {
                TotalEvents = _eventInfos.Count,
                TotalRegistrations = _attendanceRecords.Values.Count(r => r.Status != AttendanceStatus.Cancelled),
                TotalAttendees = _attendanceRecords.Values.Count(r => r.IsPresent)
            };

            stats.OverallAttendanceRate = stats.TotalRegistrations > 0 
                ? (double)stats.TotalAttendees / stats.TotalRegistrations * 100 
                : 0;

            // Group by status
            stats.AttendanceByStatus = _attendanceRecords.Values
                .GroupBy(r => r.Status.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            // Group by event
            stats.AttendanceByEvent = _attendanceRecords.Values
                .Where(r => r.IsPresent)
                .GroupBy(r => r.EventId)
                .ToDictionary(
                    g => _eventInfos.GetValueOrDefault(g.Key)?.EventName ?? g.Key,
                    g => g.Count() / (double)_attendanceRecords.Values.Count(r => r.EventId == g.Key) * 100
                );

            // Top attendees
            stats.TopAttendees = _attendanceRecords.Values
                .Where(r => r.IsPresent)
                .GroupBy(r => new { r.UserId, r.UserName, r.UserEmail })
                .Select(g => new TopAttendee
                {
                    UserId = g.Key.UserId,
                    UserName = g.Key.UserName,
                    UserEmail = g.Key.UserEmail,
                    EventsAttended = g.Count(),
                    AttendanceRate = 100.0 // Simplified for demo
                })
                .OrderByDescending(a => a.EventsAttended)
                .Take(10)
                .ToList();

            return Task.FromResult(stats);
        }

        public async Task<bool> UpdateAttendanceNotesAsync(string eventId, string userEmail, string notes)
        {
            try
            {
                var key = $"{eventId}_{userEmail}";
                
                if (!_attendanceRecords.TryGetValue(key, out var record))
                {
                    return false;
                }

                record.Notes = notes;
                AttendanceChanged?.Invoke(record);
                
                await SaveToLocalStorageAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public Task<List<EventAttendanceInfo>> GetAllEventsAttendanceAsync()
        {
            return Task.FromResult(_eventInfos.Values.ToList());
        }

        private void UpdateEventStatistics()
        {
            foreach (var eventInfo in _eventInfos.Values)
            {
                var eventRecords = _attendanceRecords.Values.Where(r => r.EventId == eventInfo.EventId).ToList();
                
                eventInfo.TotalRegistered = eventRecords.Count(r => r.Status != AttendanceStatus.Cancelled);
                eventInfo.TotalPresent = eventRecords.Count(r => r.Status == AttendanceStatus.Present);
                eventInfo.TotalNoShow = eventRecords.Count(r => r.Status == AttendanceStatus.NoShow);
                eventInfo.TotalCheckedOut = eventRecords.Count(r => r.Status == AttendanceStatus.CheckedOut);
            }
        }

        private async Task SaveToLocalStorageAsync()
        {
            try
            {
                var data = new
                {
                    AttendanceRecords = _attendanceRecords,
                    EventInfos = _eventInfos
                };
                var json = JsonSerializer.Serialize(data);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "attendanceData", json);
            }
            catch
            {
                // Handle localStorage errors silently
            }
        }

        public async Task LoadFromLocalStorageAsync()
        {
            try
            {
                var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "attendanceData");
                if (!string.IsNullOrEmpty(json))
                {
                    var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                    // Implementation would restore data from localStorage
                    // For demo purposes, we'll keep the initialized data
                }
            }
            catch
            {
                // Handle localStorage errors silently
            }
        }
    }
}
