using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LowSums;

namespace TSqlStrong.TypeSystem
{
    /// <summary>
    /// 
    /// </summary>
    public class UnknownDataType : DataType
    {
        public static readonly UnknownDataType Instance = new UnknownDataType();

        protected override ITry<Unit> OnIsAssignableTo(DataType otherType) => Try.SuccessUnit; 
        
        public override int SizeOfDomain => int.MaxValue;

        public override string ToString() => "Unknown";        
    }
}
