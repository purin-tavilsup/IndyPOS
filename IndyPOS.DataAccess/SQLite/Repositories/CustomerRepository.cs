using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using Dapper;
using IndyPOS.DataAccess.Repositories;

namespace IndyPOS.DataAccess.SQLite.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly IDbConnectionProvider _dbConnectionProvider;

        public CustomerRepository(IDbConnectionProvider dbConnectionProvider)
        {
            _dbConnectionProvider = dbConnectionProvider;
        }
    }
}
