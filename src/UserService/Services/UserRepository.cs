using System.Collections.Concurrent;
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
        var users = _users.Values.ToList();
        _logger.LogDebug("GetAll module=users count={Count} users={Users}", users.Count, users);
        return users;
    }

    public User? GetById(Guid id)
    {
        var found = _users.TryGetValue(id, out var user);

        _logger.LogDebug("GetById module=users id={Id} found={Found} user={User}", id, found, user);
        return found ? user : null;
    }

    public User Add(string name, string email)
    {
        _logger.LogDebug("Add module=users name={Name} email={Email}", name, MaskEmail(email));

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email
        };

        _users[user.Id] = user;

        _logger.LogDebug("Add module=users id={Id} user={User}", user.Id, user);
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
