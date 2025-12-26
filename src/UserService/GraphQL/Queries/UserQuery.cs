using HotChocolate.Types;
using UserService.Models;
using UserService.Services;

namespace UserService.GraphQL.Queries;

[ExtendObjectType("Query")]
public class UserQuery
{
    public IEnumerable<User> GetUsers([Service] UserRepository repository) => repository.GetAll();

    public User? GetUserById(Guid id, [Service] UserRepository repository) => repository.GetById(id);
}
