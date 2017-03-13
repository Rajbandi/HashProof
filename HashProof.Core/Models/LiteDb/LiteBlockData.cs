using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;

namespace HashProof.Core.Models.LiteDb
{
    public class LiteBlockData : BlockData
    {
        [BsonId]
        public ObjectId Id { get; set; }
    }

}
