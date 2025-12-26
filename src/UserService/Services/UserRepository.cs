using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using UserService.Models;

namespace UserService.Services;

public class UserRepository
{
    private readonly ConcurrentDictionary<Guid, User> _users = new();
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(ILogger<UserRepository> logger)
    {
        _logger = logger;
    }

    public IEnumerable<User> GetAll()
    {
        var sw = Stopwatch.StartNew();
        _logger.LogDebug("GetAll start module=users");

        var users = _users.Values.ToList();

        sw.Stop();
        _logger.LogDebug("GetAll end module=users count={Count} durationMs={DurationMs}", users.Count, sw.Elapsed.TotalMilliseconds);
        return users;
    }

    public User? GetById(Guid id)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogDebug("GetById start module=users id={Id}", id);

        var found = _users.TryGetValue(id, out var user);

        sw.Stop();
        _logger.LogDebug("GetById end module=users id={Id} found={Found} durationMs={DurationMs}", id, found, sw.Elapsed.TotalMilliseconds);
        return found ? user : null;
    }

    public User Add(string name, string email)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogDebug("Add start module=users name={Name} email={Email}", name, MaskEmail(email));

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email
        };

        _users[user.Id] = user;

        sw.Stop();
        _logger.LogDebug("Add end module=users id={Id} durationMs={DurationMs}", user.Id, sw.Elapsed.TotalMilliseconds);
        return user;
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return string.Empty;
        }

        var at = email.IndexOf('@');
        if (at <= 1)
        {
            return "***";
        }

        return $"{email[0]}***{email.Substring(at)}";
    }
}
