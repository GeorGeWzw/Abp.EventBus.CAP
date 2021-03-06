// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Dapper;
using DotNetCore.CAP.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories.Dapper;
using Volo.Abp.EntityFrameworkCore;

namespace DotNetCore.CAP.MySql
{
    public class MySqlStorageInitializer<TDbContext> : DapperRepository<TDbContext>, IStorageInitializer
            where TDbContext : IEfCoreDbContext
    {
        private readonly IOptions<EFOptions> _options;
        private readonly ILogger _logger;

        public MySqlStorageInitializer(
            IDbContextProvider<TDbContext> dbContextProvider,
            ILogger<MySqlStorageInitializer<TDbContext>> logger,
            IOptions<EFOptions> options)
            :base(dbContextProvider)
        {
            _options = options;
            _logger = logger;
        }

        public virtual string GetPublishedTableName()
        {
            return $"{_options.Value.TableNamePrefix}.published";
        }

        public virtual string GetReceivedTableName()
        {
            return $"{_options.Value.TableNamePrefix}.received";
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var sql = CreateDbTablesScript();
            var connection = this.DbConnection;
 
                await connection.ExecuteAsync(sql, transaction: DbTransaction);
      

            _logger.LogDebug("Ensuring all create database tables script are applied.");
        }


        protected virtual string CreateDbTablesScript()
        {
            var batchSql =
                $@"
CREATE TABLE IF NOT EXISTS `{GetReceivedTableName()}` (
  `Id` bigint NOT NULL,
  `Version` varchar(20) DEFAULT NULL,
  `Name` varchar(400) NOT NULL,
  `Group` varchar(200) DEFAULT NULL,
  `Content` longtext,
  `Retries` int(11) DEFAULT NULL,
  `Added` datetime NOT NULL,
  `ExpiresAt` datetime DEFAULT NULL,
  `StatusName` varchar(50) NOT NULL,
  PRIMARY KEY (`Id`),
  INDEX `IX_ExpiresAt`(`ExpiresAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `{GetPublishedTableName()}` (
  `Id` bigint NOT NULL,
  `Version` varchar(20) DEFAULT NULL,
  `Name` varchar(200) NOT NULL,
  `Content` longtext,
  `Retries` int(11) DEFAULT NULL,
  `Added` datetime NOT NULL,
  `ExpiresAt` datetime DEFAULT NULL,
  `StatusName` varchar(40) NOT NULL,
  PRIMARY KEY (`Id`),
  INDEX `IX_ExpiresAt`(`ExpiresAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
";
            return batchSql;
        }
    }
}