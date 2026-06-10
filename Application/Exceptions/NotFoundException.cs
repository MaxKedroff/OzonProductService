namespace Application.Exceptions
{
    public class NotFoundException : ApplicationException
    {
        public NotFoundException(string entityName, object id)
        : base($"{entityName} with id '{id}' was not found")
        {
            EntityName = entityName;
            Id = id;
        }

        public string EntityName { get; }
        public object Id { get; }
    }
}
