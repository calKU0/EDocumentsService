namespace EDocuments.Contracts.Repositories
{
    public interface IAttributeRepository
    {
        public Task UpdateAttribute(string className, int objectId, int objectType, int objectLp, string value);
    }
}
