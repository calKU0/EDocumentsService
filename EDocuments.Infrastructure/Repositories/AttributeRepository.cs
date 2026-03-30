using Dapper;
using EDocuments.Contracts.Repositories;
using EDocuments.Infrastructure.Data;
using System.Data;

namespace EDocuments.Infrastructure.Repositories
{
    public class AttributeRepository : IAttributeRepository
    {
        private readonly DapperContext _context;
        public AttributeRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task UpdateAttribute(string className, int objectId, int objectType, int objectLp, string value)
        {
            const string procedure = "kkur.ZaktualizujAtrybut";
            using (var connection = _context.CreateConnection())
            {
                var parameters = new
                {
                    Class = className,
                    ObjectId = objectId,
                    ObjectType = objectType,
                    ObjectLp = objectLp,
                    Value = value,
                };

                connection.Open();

                await connection.ExecuteAsync(procedure, parameters, commandType: CommandType.StoredProcedure);
            }
        }
    }
}
