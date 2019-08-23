//Copyright 2019 Volodymyr Podshyvalov
//
//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using AuthorityController.Data.Application;
using System.Data.Common;
using UniformDataOperator.SQL.Tables;

namespace AuthorityController.Data.Personal
{
    /// <summary>
    /// Part ob class that  implements UniformDataOprator's interfaces.
    /// </summary>
    public partial class User : ISQLDataReadCompatible, ISQLTable
    {
        /// <summary>
        /// Name of the shema that would created in data base to store users data.
        /// </summary>
        public virtual string SchemaName
        {
            get
            {
                return "DNFAuthControl";
            }
        }

        /// <summary>
        /// Name of the table in data base that would be used to storage user's data.
        /// </summary>
        public virtual string TableName
        {
            get
            {
                return "user";
            }
        }

        /// <summary>
        /// Name of data base engine that would power the table.
        /// </summary>
        public virtual string TableEngine
        {
            get
            {
                return "InnoDB";
            }
        }

        /// <summary>
        /// Table's fields that would be created in database.
        /// </summary>
        public virtual TableColumnMeta[] TableFields
        {
            get
            {
                // Init field if not init.
                if (_TableFields == null)
                {
                    _TableFields = new TableColumnMeta[]
                    {
                        new TableColumnMeta()
                        {
                            name = "userid",
                            type = "INT",
                            isPrimaryKey = true,
                            isNotNull = true,
                            isAutoIncrement = true
                        },
                        new TableColumnMeta()
                        {
                            name = "login",
                            type = "VARCHAR(45)",
                            isNotNull = true
                        },
                        new TableColumnMeta()
                        {
                            name = "password",
                            type = "BLOB(512)",
                            isNotNull = true
                        },
                        new TableColumnMeta()
                        {
                            name = "firstname",
                            type = "VARCHAR(45)",
                            isNotNull = true
                        },
                        new TableColumnMeta()
                        {
                            name = "lastname",
                            type = "VARCHAR(45)",
                            isNotNull = true
                        },
                        new TableColumnMeta()
                        {
                            name = "rights",
                            type = "VARCHAR(1000)"
                        },
                        new TableColumnMeta()
                        {
                            name = "bans",
                            type = "TYNYBLOB"
                        }
                    };
                }
                return _TableFields;
            }
        }
        protected TableColumnMeta[] _TableFields;

        /// <summary>
        /// Read object data from DB data reader.
        /// </summary>
        /// <param name="reader"></param>
        public void ReadSQLObject(DbDataReader reader)
        {
            try { id = (uint)reader.GetInt32(reader.GetOrdinal("userid")); } catch { };

            try { login = reader.GetString(reader.GetOrdinal("login")); } catch { };
            try { password = reader["password"] as byte[]; } catch { };

            try { firstName = reader.GetString(reader.GetOrdinal("firstname")); } catch { };
            try { secondName = reader.GetString(reader.GetOrdinal("lastname")); } catch { };

            try { rights = reader.GetString(reader.GetOrdinal("rights")).Split('+'); } catch { };

            try
            {
                byte[] bansBinary = reader["bans"] as byte[];
                if (bansBinary != null)
                {
                    bans = UniformDataOperator.Binary.BinaryHandler.FromByteArray<List<BanInformation>>(bansBinary);
                }
            }
            catch { };
        }
    }
}
