using Xunit;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace RaceDay.Tests
{
    public class RaceDayTests
    {
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private DefaultHttpContext GetMockHttpContext(int userId, string role)
        {
            var httpContext = new DefaultHttpContext();
            var session = new TestSession();
            session.SetInt32("UserId", userId);
            session.SetString("UserRole", role);
            httpContext.Session = session;
            return httpContext;
        }

        [Fact]
        public async Task Register_CreatesUser_WhenValid()
        {
            var db = GetInMemoryDbContext();
            var controller = new AuthController(db);

            var result = await controller.Register(new RegisterDto
            {
                Name = "Aphiwe Ndlovu",
                Email = "aphiwe@test.com",
                Password = "Password123!",
                Role = UserRole.Participant
            });

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(1, await db.Users.CountAsync());
        }

        [Fact]
        public async Task Login_ReturnsSuccess_WhenCredentialsAreValid()
        {
            var db = GetInMemoryDbContext();
            var controller = new AuthController(db);

            // Register first
            await controller.Register(new RegisterDto
            {
                Name = "Organiser User",
                Email = "org@test.com",
                Password = "Password123!",
                Role = UserRole.Organiser
            });

            // Now login
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var result = await controller.Login(new LoginDto
            {
                Email = "org@test.com",
                Password = "Password123!"
            });

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult);
        }

        [Fact]
        public async Task CreateEvent_AsOrganiser_Succeeds()
        {
            var db = GetInMemoryDbContext();
            var controller = new EventsController(db)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = GetMockHttpContext(1, "Organiser")
                }
            };

            var evtDto = new EventCreateDto
            {
                Name = "Spring Marathon",
                DistanceKm = 10.0,
                Type = EventType.Run,
                Location = "Durban",
                Date = DateTime.UtcNow.AddDays(10)
            };

            var result = await controller.Create(evtDto);

            Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(1, await db.Events.CountAsync());
        }

        [Fact]
        public async Task CreateEvent_AsParticipant_ReturnsForbidden()
        {
            var db = GetInMemoryDbContext();
            var controller = new EventsController(db)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = GetMockHttpContext(2, "Participant") // Participant role attempting organiser action
                }
            };

            var evtDto = new EventCreateDto
            {
                Name = "Unauthorized Event",
                DistanceKm = 5.0
            };

            var result = await controller.Create(evtDto);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, objectResult.StatusCode); // Verify role enforcement rejection
        }

        [Fact]
        public async Task Enrol_AsParticipant_Succeeds()
        {
            var db = GetInMemoryDbContext();

            // Seed a category
            var eventObj = new Event { Name = "Test Run", Location = "JHB", DistanceKm = 5 };
            db.Events.Add(eventObj);
            db.Categories.Add(new Category { Name = "Open", MinAge = 18, MaxAge = 99, Event = eventObj });
            await db.SaveChangesAsync();

            var controller = new EnrolmentsController(db)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = GetMockHttpContext(5, "Participant")
                }
            };

            var enrolmentDto = new EnrolmentDto { EventId = eventObj.Id, CategoryId = 1 };
            var result = await controller.Enrol(enrolmentDto);

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(1, await db.Enrolments.CountAsync());
        }
    }

    // Helper class to mock ISession in unit tests
    public class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _sessionStorage = new(StringComparer.OrdinalIgnoreCase);
        public bool IsAvailable => true;
        public string Id => Guid.NewGuid().ToString();
        public IEnumerable<string> Keys => _sessionStorage.Keys;
        public void Clear() => _sessionStorage.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _sessionStorage.Remove(key);
        public void Set(string key, byte[] value) => _sessionStorage[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _sessionStorage.TryGetValue(key, out value!);
    }
}